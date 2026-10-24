using System.ComponentModel;
using System.Diagnostics;

namespace WWPoser.Services;
public sealed class CSharpRosterService(string repoRoot)
{
    private readonly string repo = Path.GetFullPath(repoRoot);
    private readonly StageEvidence evidence = new(repoRoot);
    public string? ActiveSampler { get; private set; }

    public async Task StopSamplerAsync(CancellationToken cancellation = default)
    {
        if (ActiveSampler is not { } sample) return;
        JsonData.AtomicWrite(Path.Combine(sample, "stop.flag"), []);
        var watch = Stopwatch.StartNew();
        while (!File.Exists(Path.Combine(sample, "puerts_sampler_result.json")) && !CarrierLoader.NotLoaded(sample) && watch.Elapsed < TimeSpan.FromSeconds(15))
            await Task.Delay(100, cancellation);
        ActiveSampler = null;
    }

    private string PrepareSampler(string sourcePath, JsonObject controller, string output, GameIdentity identity)
    {
        var source = JsonData.ReadObject(sourcePath);
        var cfg = new JsonObject
        {
            ["schema"] = 1,
            ["host_sha256"] = Contract.Build,
            ["gobjects_rva"] = source["gobjects_rva"]?.DeepClone(),
            ["name_pool_rva"] = source["name_pool_rva"]?.DeepClone(),
            ["expected_game_pid"] = identity.Pid,
            ["expected_process_creation_filetime"] = identity.CreationFileTime,
            ["game_window_handle"] = identity.WindowHandle,
            ["controller_seed"] = controller.DeepClone(),
            ["camera_contract"] = JsonData.ReadObject(Path.Combine(repo, "data/puerts_sampler_camera_contract.json")),
            ["sample_count"] = 2,
            ["sample_spacing_frames"] = 8,
            ["result_limit"] = 262144,
            ["capture_csharp_roster"] = true,
            ["capture_native_join"] = true,
            ["source_route"] = StageEvidence.Route,
            ["source_contract"] = StageEvidence.SourceContract,
            ["profiles"] = source["profiles"]?.DeepClone(),
            ["scan_contract"] = source["scan_contract"]?.DeepClone(),
            ["property_diagnostic_contract"] = JsonData.ReadObject(Path.Combine(repo, "data/stage_identity_property_contract.json")),
            ["entity_diagnostic_contract"] = JsonData.ReadObject(Path.Combine(repo, "data/stage_identity_entity_contract.json"))
        };
        cfg["session_evidence_id"] = JsonData.CanonicalDigest(cfg);
        Directory.CreateDirectory(output);
        File.Copy(Path.Combine(repo, "probes/v20_recon/puerts_scene_sampler.dll"), Path.Combine(output, "recon_carrier_v20.dll"));
        File.Copy(Path.Combine(repo, "probes/v20_recon/hook_install_v20.exe"), Path.Combine(output, "hook_install_v20.exe"));
        JsonData.Write(Path.Combine(output, "puerts_sampler_config.json"), cfg);
        return output;
    }

    public async Task<string> RefreshAsync(string stageSeed, CancellationToken cancellation = default)
    {
        var seed = Path.GetFullPath(stageSeed);
        if (File.Exists(Path.Combine(seed, "evidence", "stage", "stage_config.json")))
            seed = Path.Combine(seed, "evidence", "stage");
        var cfg = JsonData.ReadObject(Path.Combine(seed, "stage_config.json"));
        var identity = GameProcess.Current();
        Contract.Require(identity.Pid == Contract.I(cfg, "expected_game_pid") && identity.CreationFileTime == Contract.I(cfg, "expected_process_creation_filetime"), "游戏实例已变；需要重新后台扫描，不得复用旧地址");
        var root = Path.Combine(repo, "playback/squad_sessions", "roster-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "source_config.json");
        File.Copy(Path.Combine(seed, "evidence/source_config.json"), source);
        var sample = PrepareSampler(source, Contract.O(Contract.O(cfg, "scene_team_preflight"), "controller"), Path.Combine(root, "sample"), identity);
        await GameProcess.WaitForForegroundAsync(identity, cancellation);
        ActiveSampler = sample;
        try
        {
            await CarrierLoader.LoadAsync(sample, cancellation);
            var watch = Stopwatch.StartNew();
            while (!File.Exists(Path.Combine(sample, "puerts_sampler_result.json")))
            {
                Contract.Require(watch.Elapsed < TimeSpan.FromSeconds(20), "采样结果未返回；保留采样目录等待恢复");
                await Task.Delay(100, cancellation);
            }
        }
        catch (Win32Exception) { ActiveSampler = null; throw; }
        catch { JsonData.AtomicWrite(Path.Combine(sample, "stop.flag"), []); throw; }
        ActiveSampler = null;
        return evidence.PrepareStage(source, sample, Path.Combine(root, "stage"), identity);
    }
}
