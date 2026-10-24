using System.Diagnostics;

namespace WWPoser.Services;
public sealed record MotionImportRequest(string Source, string? Skeleton, string Output, double? Scale = null, string? FaceSource = null, string? ModelId = null, string? CharacterProfile = null, string? MeshAssetName = null, string? CacheDirectory = null);
public sealed class MotionImportService(string repoRoot)
{
    private readonly string root = Path.GetFullPath(repoRoot);
    private readonly CharacterRegistry registry = new(repoRoot);
    private static readonly string[] CacheFiles = ["body_report.json", "motion.wwlocal", "expression_profile.json", "expression.wwexpr"];
    public async Task<JsonObject> ImportAsync(MotionImportRequest request, CancellationToken cancellation = default)
    {
        var started = Stopwatch.StartNew();
        var source = Path.GetFullPath(request.Source);
        var face = Path.GetFullPath(request.FaceSource ?? source);
        var output = Path.GetFullPath(request.Output);
        var profile = request.CharacterProfile != null ? registry.LoadProfile(request.CharacterProfile)
            : registry.GetProfile(Contract.S(Contract.A(registry.Index(), "profiles").First(x => request.ModelId != null ? Contract.S(x!, "model_id") == request.ModelId : Contract.S(x!, "skeleton_sha256") == JsonData.Digest(request.Skeleton!))!, "profile_id"));
        var skeleton = Path.GetFullPath(request.Skeleton ?? registry.SkeletonPath(profile));
        var model = Contract.S(profile, "model_id");
        var mesh = request.MeshAssetName ?? model;
        var targets = registry.MorphNames(profile, mesh);
        var bodyTool = Path.Combine(root, "tools", "mmd_outputs", "mmd_vmd_cli.exe");
        var faceTool = Path.Combine(root, "tools", "mmd_outputs", "mmd_face_compile.exe");
        var sourceSha = JsonData.Digest(source);
        var faceSha = JsonData.Digest(face);
        var context = new JsonObject
        {
            ["schema"] = 1,
            ["build"] = "csharp-motion-import-v1",
            ["profile_id"] = Contract.S(profile, "profile_id"),
            ["profile_sha256"] = JsonData.Digest(registry.ProfilePath(profile)),
            ["skeleton_sha256"] = JsonData.Digest(skeleton),
            ["mesh_asset_name"] = mesh,
            ["source_sha256"] = sourceSha,
            ["face_source_sha256"] = faceSha,
            ["scale"] = request.Scale,
            ["body_compiler_sha256"] = JsonData.Digest(bodyTool),
            ["face_compiler_sha256"] = JsonData.Digest(faceTool),
            ["implementation_sha256"] = JsonData.CanonicalDigest(new JsonObject { ["algorithm"] = "csharp-motion-import-v1", ["assembly_mvid"] = typeof(MotionImportService).Assembly.ManifestModule.ModuleVersionId.ToString("D") })
        };
        var cachePath = request.CacheDirectory == null ? null : Path.Combine(Path.GetFullPath(request.CacheDirectory), JsonData.CanonicalDigest(context));
        var cacheHit = cachePath != null && File.Exists(Path.Combine(cachePath, "manifest.json"));
        var bodyPath = Path.Combine(output, "motion.wwlocal");
        var reportPath = Path.Combine(output, "body_report.json");
        var expressionPath = Path.Combine(output, "expression.wwexpr");
        var expressionProfile = Path.Combine(output, "expression_profile.json");
        Directory.CreateDirectory(output);
        if (cacheHit)
        {
            foreach (var leaf in CacheFiles.Where(x => File.Exists(Path.Combine(cachePath!, x))))
                File.Copy(Path.Combine(cachePath!, leaf), Path.Combine(output, leaf));
        }
        else
        {
            var censusPath = Path.Combine(output, "target_morph_census.json");
            JsonData.Write(censusPath, new JsonObject { ["models"] = new JsonArray(new JsonObject { ["model_id"] = model, ["mesh_asset_name"] = mesh, ["morphs"] = targets.DeepClone() }), ["character_profile_id"] = Contract.S(profile, "profile_id") });
            var bodyArgs = new List<string> { "--source", source, "--output", reportPath, "--target-skeleton", skeleton, "--compiled", bodyPath };
            if (request.Scale != null)
                bodyArgs.AddRange(["--scale", request.Scale.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            (await ProcessRunner.RunAsync(bodyTool, bodyArgs, root, cancellation)).EnsureSuccess("身体动作编译失败");
            (await ProcessRunner.RunAsync(faceTool, [face, censusPath, expressionProfile, expressionPath, "--model-id", model, "--mesh-asset-name", mesh], root, cancellation)).EnsureSuccess("表情动作编译失败");
        }

        var report = JsonData.ReadObject(reportPath);
        var expression = JsonData.ReadObject(expressionProfile);
        expression["source"] = face;
        expression["source_sha256"] = sourceSha;
        expression["body_source_sha256"] = sourceSha;
        expression["expression_source_sha256"] = faceSha;
        expression["separate_expression_source"] = source != face;
        expression["character_profile_id"] = Contract.S(profile, "profile_id");
        expression["expression_binary"] = targets.Count > 0 ? expressionPath : null;
        expression["expression_binary_sha256"] = targets.Count > 0 ? JsonData.Digest(expressionPath) : null;
        JsonData.Write(expressionProfile, expression);
        var enabled = targets.Count > 0 && Contract.A(expression, "bindings").Any(x => Contract.A(x!, "targets").Count > 0);
        report["source"] = source;
        Contract.O(report, "compiled_target_clip")["path"] = bodyPath;
        report["character_profile_id"] = Contract.S(profile, "profile_id");
        report["model_id"] = model;
        report["mesh_asset_name"] = mesh;
        JsonData.Write(reportPath, report);
        if (cachePath != null && !cacheHit)
        {
            Directory.CreateDirectory(cachePath);
            foreach (var leaf in CacheFiles.Where(x => File.Exists(Path.Combine(output, x))))
                File.Copy(Path.Combine(output, leaf), Path.Combine(cachePath, leaf), true);
            JsonData.Write(Path.Combine(cachePath, "manifest.json"), new JsonObject { ["schema"] = 1, ["context"] = context });
        }

        return new JsonObject
        {
            ["state"] = "compiled",
            ["report"] = reportPath,
            ["clip"] = bodyPath,
            ["model_id"] = model,
            ["character_profile_id"] = Contract.S(profile, "profile_id"),
            ["expression_enabled"] = enabled,
            ["expression_profile"] = expressionProfile,
            ["expression_clip"] = targets.Count > 0 ? expressionPath : null,
            ["cache"] = new JsonObject { ["enabled"] = cachePath != null, ["hit"] = cacheHit },
            ["timings_seconds"] = new JsonObject { ["total"] = started.Elapsed.TotalSeconds }
        };
    }
}
