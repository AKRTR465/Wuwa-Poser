using System.Buffers.Binary;
using System.Security.Cryptography;

namespace WWPoser.Services;

public sealed record OwnedMmdPrepareRequest(string BodySource, string OutputDirectory, double? Scale = null, int DurationMs = 0, double Speed = 1, bool Loop = false, string? CharacterId = null, string? FaceSource = null, string? CameraSource = null, int CameraMode = 0, SquadPlacement? Placement = null, bool NativePhysics = true, bool Physics = false);

// Preparation contains clip data only. The carrier obtains its own live mesh.
public sealed partial class OwnedMmdPlaybackService(string repoRoot)
{
    public const string Scope = "owned_character_mmd_body";
    private readonly string root = Path.GetFullPath(repoRoot);
    private static readonly string[] CanonicalFields = ["canonical_position_mask", "canonical_local_positions", "canonical_rotation_mask", "canonical_local_rotations"];

    private static ulong NewEpoch()
    {
        Span<byte> bytes = stackalloc byte[8];
        ulong epoch;
        do { RandomNumberGenerator.Fill(bytes); epoch = BinaryPrimitives.ReadUInt64LittleEndian(bytes); } while (epoch == 0);
        return epoch;
    }

    public OwnedCharacterCatalog CharacterCatalog() => OwnedCharacterCatalog.Load(root);
    public static JsonObject Config(string directory) => JsonData.ReadObject(Path.Combine(directory, "owned_mmd_config.json"));
    public static JsonObject ApplySettings(string directory, int durationMs, double speed, bool loop)
    {
        var config = Config(directory);
        config["duration_ms"] = durationMs; config["speed"] = speed; config["loop"] = loop;
        JsonData.Write(Path.Combine(directory, "owned_mmd_config.json"), config);
        return config;
    }

    public async Task<JsonObject> PrepareAsync(OwnedMmdPrepareRequest request, CancellationToken cancellation = default)
    {
        var character = CharacterCatalog().Select(request.CharacterId);
        var registry = new CharacterRegistry(root); var profile = registry.GetProfile(character.ProfileId);
        var output = Path.GetFullPath(request.OutputDirectory);
        Directory.CreateDirectory(output);
        var bodySource = Path.Combine(output, "source.vmd"); File.Copy(request.BodySource, bodySource);
        var faceSource = request.FaceSource is null ? null : Path.Combine(output, "face_source.vmd");
        if (faceSource is not null) File.Copy(request.FaceSource!, faceSource);
        File.Copy(registry.ProfilePath(profile), Path.Combine(output, "character_profile.json"));
        File.Copy(registry.SkeletonPath(profile), Path.Combine(output, "character_skeleton.json"));
        var reportPath = Path.Combine(output, "compiled_report.json"); var clipPath = Path.Combine(output, "motion.wwlocal");
        var imported = await new MotionImportService(root).ImportAsync(new(bodySource, registry.SkeletonPath(profile), output, request.Scale, faceSource, character.ModelId, registry.ProfilePath(profile), character.ModelAssetName, Path.Combine(root, "playback", "character_cache_csharp")), cancellation);
        File.Copy(Contract.S(imported, "report"), reportPath);
        var clip = Contract.O(JsonData.ReadObject(reportPath), "compiled_target_clip");
        var epoch = NewEpoch();
        var config = new JsonObject
        {
            ["schema"] = 2, ["scope"] = Scope, ["character_id"] = character.CharacterId, ["character_display_name"] = character.DisplayName,
            ["role_id"] = character.RoleId, ["skin_id"] = character.SkinId, ["model_id"] = character.ModelId, ["profile_id"] = character.ProfileId,
            ["form"] = character.Form?.DeepClone(), ["independent_instance"] = character.IndependentInstance,
            ["character_profile_file"] = "character_profile.json", ["character_skeleton_file"] = "character_skeleton.json",
            ["character_profile_sha256"] = JsonData.Digest(registry.ProfilePath(profile)),
            ["mesh_asset_name"] = character.ModelAssetName, ["skeleton_asset_name"] = profile["skeleton_asset_name"]!.DeepClone(),
            ["bone_count"] = profile["bone_count"]!.DeepClone(), ["bone_names"] = profile["bone_names"]!.DeepClone(), ["bone_parents"] = profile["bone_parents"]!.DeepClone(),
            ["source_file"] = "source.vmd", ["local_clip_file"] = "motion.wwlocal", ["compiled_report_file"] = "compiled_report.json",
            ["local_clip_sha256"] = clip["sha256"]!.DeepClone(),
            ["fps"] = 60, ["frame_count"] = clip["frame_count"]!.DeepClone(), ["clip_duration_seconds"] = clip["duration_seconds"]!.DeepClone(),
            ["duration_ms"] = request.DurationMs, ["speed"] = request.Speed, ["loop"] = request.Loop, ["session_epoch"] = epoch,
            ["expression_enabled"] = false, ["camera_enabled"] = false, ["native_physics"] = request.NativePhysics, ["body_backend"] = "game_native", ["ground_height_cm"] = -2,
            ["placement"] = PlaybackAssets.Placement(request.Placement), ["target_slot"] = 1,
            ["physics_model_id"] = profile["physics_model_id"]?.DeepClone(), ["bone_chains_file"] = "bone_chains.json"
        };
        var assets = new PlaybackAssets(root);
        assets.Expression(config, imported);
        assets.Physics(config, output, request.NativePhysics, request.Physics);
        await assets.CameraAsync(config, output, request.CameraSource, request.CameraMode, clip, cancellation);
        config["duration_seconds"] = Math.Max(Math.Max(Contract.D(clip, "duration_seconds"), config["expression_duration_seconds"]?.GetValue<double>() ?? 0), config["camera_clip_duration_seconds"]?.GetValue<double>() ?? 0);
        foreach (var key in CanonicalFields) config[key] = clip[key]!.DeepClone();
        JsonData.Write(Path.Combine(output, "owned_mmd_config.json"), config);
        return new JsonObject { ["state"] = "prepared", ["session_dir"] = output, ["config"] = Path.Combine(output, "owned_mmd_config.json"), ["character_id"] = character.CharacterId, ["character_display_name"] = character.DisplayName, ["profile_id"] = character.ProfileId, ["session_epoch"] = epoch };
    }
}
