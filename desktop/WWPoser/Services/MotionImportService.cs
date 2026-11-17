using System.Diagnostics;

namespace WWPoser.Services;
public sealed record MotionImportRequest(string Source, string ProfileId, string MeshAssetName, string Output, double? Scale = null, string? FaceSource = null, string? CacheDirectory = null, string? GazeSource = null, double GazeStrength = 1);
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
        var gaze = request.GazeSource is null ? null : Path.GetFullPath(request.GazeSource);
        Contract.Require(double.IsFinite(request.GazeStrength) && request.GazeStrength >= 0 && request.GazeStrength <= 3, "目线幅度须在 0–3 倍之间。");
        var output = Path.GetFullPath(request.Output);
        var profile = registry.GetProfile(request.ProfileId);
        var skeleton = registry.SkeletonPath(profile);
        var model = Contract.S(profile, "model_id");
        var mesh = request.MeshAssetName;
        var targets = registry.MorphNames(profile, mesh);
        var bodyTool = Path.Combine(root, "tools", "mmd_outputs", "mmd_vmd_cli.exe");
        var faceTool = Path.Combine(root, "tools", "mmd_outputs", "mmd_face_compile.exe");
        string? cachePath = null;
        if (request.CacheDirectory is { } cacheDirectory)
        {
            var inputs = new InputFileDigests();
            var context = new JsonObject
            {
                ["assembly"] = typeof(MotionImportService).Module.ModuleVersionId.ToString("D"),
                ["profile"] = inputs.Read(registry.ProfilePath(profile)),
                ["skeleton"] = inputs.Read(skeleton),
                ["mesh"] = mesh,
                ["body"] = inputs.Read(source),
                ["face"] = inputs.Read(face),
                ["gaze"] = gaze is null ? null : inputs.Read(gaze),
                ["gaze_strength"] = request.GazeStrength,
                ["scale"] = request.Scale,
                ["body_compiler"] = inputs.Read(bodyTool),
                ["face_compiler"] = inputs.Read(faceTool)
            };
            cachePath = Path.Combine(Path.GetFullPath(cacheDirectory), JsonData.CanonicalDigest(context));
        }
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
            var faceArgs = new List<string> { face, censusPath, expressionProfile, expressionPath, "--model-id", model, "--mesh-asset-name", mesh,
                "--gaze-fallback", source, "--gaze-strength", request.GazeStrength.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            if (gaze is not null) faceArgs.AddRange(["--gaze-source", gaze]);
            (await ProcessRunner.RunAsync(faceTool, faceArgs, root, cancellation)).EnsureSuccess("表情 / 目线编译失败");
        }

        var report = JsonData.ReadObject(reportPath);
        var expression = JsonData.ReadObject(expressionProfile);
        expression["source"] = face;
        expression["separate_expression_source"] = source != face;
        // Compiler cache stores paths from its original preparation; bind to this request.
        if (Contract.True(expression, "gaze_enabled"))
        {
            var selectedGaze = gaze ?? (expression["gaze_source_kind"]?.GetValue<string>() == "body" ? source : face);
            expression["gaze_source"] = selectedGaze;
        }
        expression["character_profile_id"] = Contract.S(profile, "profile_id");
        expression["expression_binary"] = targets.Count > 0 ? expressionPath : null;
        expression["expression_binary_sha256"] = targets.Count > 0 ? JsonData.Digest(expressionPath) : null;
        JsonData.Write(expressionProfile, expression);
        var enabled = targets.Count > 0 && Contract.True(expression, "expression_enabled");
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
            JsonData.Write(Path.Combine(cachePath, "manifest.json"), new JsonObject { ["schema"] = 1 });
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
