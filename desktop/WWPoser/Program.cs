using System.Windows;
using WWPoser.Services;

namespace WWPoser;

public static class Program
{
    private static readonly HashSet<string> HeadlessCommands = new(StringComparer.Ordinal)
    {
        "--prepare-owned-mmd", "--validate-bundle", "--save-bookmark", "--validate-bookmark",
        "--render-recording-audio", "--import-vmd", "--run-owned-mmd"
    };

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            var root = FindRoot(args);
            if (args.Contains("--run-owned-mmd")) return OwnedMmdCommandLine.RunAsync(root, args).GetAwaiter().GetResult();
            if (args.Contains("--prepare-owned-mmd"))
            {
                var i = Array.IndexOf(args, "--prepare-owned-mmd");
                var characterIndex = Array.IndexOf(args, "--character-id");
                var result = new OwnedMmdPlaybackService(root).PrepareAsync(new(args[i + 1], args[i + 2], DurationMs: int.Parse(args[i + 3], System.Globalization.CultureInfo.InvariantCulture), CharacterId: characterIndex >= 0 ? args[characterIndex + 1] : null)).GetAwaiter().GetResult();
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "owned_mmd_prepare_result.json"), result.ToJsonString(JsonData.Pretty)); return 0;
            }
            if (args.Contains("--validate-bundle")) { BundleValidation.Validate(root); return 0; }
            if (args.Contains("--save-bookmark"))
            {
                var i = Array.IndexOf(args, "--save-bookmark");
                BookmarkService.Snapshot(args[i + 1], double.Parse(args[i + 2], System.Globalization.CultureInfo.InvariantCulture), args[i + 3]); return 0;
            }
            if (args.Contains("--validate-bookmark"))
            {
                var i = Array.IndexOf(args, "--validate-bookmark"); var seconds = BookmarkService.SeekTime(args[i + 1], args[i + 2]);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "bookmark_validation.json"), new JsonObject { ["state"] = "validated", ["seconds"] = seconds }.ToJsonString()); return 0;
            }
            if (args.Contains("--render-recording-audio"))
            {
                var i = Array.IndexOf(args, "--render-recording-audio");
                var result = new RecordingService(root).RenderAudioAsync(args[i + 1], args[i + 2]).GetAwaiter().GetResult();
                JsonData.Write(Path.GetFullPath(args[i + 2]) + ".json", result); return 0;
            }
            if (args.Contains("--import-vmd"))
            {
                var i = Array.IndexOf(args, "--import-vmd");
                var result = new MotionImportService(root).ImportAsync(new(args[i + 1], null, args[i + 4], CharacterProfile: args[i + 2], MeshAssetName: args[i + 3])).GetAwaiter().GetResult();
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "import_result.json"), result.ToJsonString(JsonData.Pretty)); return 0;
            }
            var preparationIndex = Array.IndexOf(args, "--owned-preparation");
            var application = new Application();
            var window = new MainWindow(root);
            if (args.Contains("--owned-mmd")) window.SelectOwnedMmdPage();
            if (preparationIndex >= 0) window.LoadOwnedPreparation(args[preparationIndex + 1]);
            application.Run(window);
            return 0;
        }
        catch (Exception error)
        {
            if (args.Any(HeadlessCommands.Contains))
            {
                JsonData.Write(Path.Combine(AppContext.BaseDirectory, "command_error.json"), new JsonObject { ["state"] = "failed", ["error"] = error.ToString() });
                return 1;
            }
            MessageBox.Show(error.Message, "播放器启动失败", MessageBoxButton.OK, MessageBoxImage.Error); return 1;
        }
    }
    private static string FindRoot(string[] args)
    {
        var index = Array.IndexOf(args, "--root");
        if (index >= 0) return Path.GetFullPath(args[index + 1]);
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null)
        {
            if (File.Exists(Path.Combine(folder.FullName, "data/characters/index.json"))) return folder.FullName;
            folder = folder.Parent;
        }
        throw new DirectoryNotFoundException("找不到当前角色与运行契约目录");
    }
}
