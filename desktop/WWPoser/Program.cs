using System.Windows;
using WWPoser.Services;

namespace WWPoser;

public static class Program
{
    private static readonly HashSet<string> HeadlessCommands = new(StringComparer.Ordinal) { "--prepare-owned-mmd", "--validate-bundle", "--run-owned-mmd" };

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            var root = FindRoot(args);
            if (args.Contains("--run-owned-mmd")) return OwnedMmdCommandLine.RunAsync(root, args).GetAwaiter().GetResult();
            if (args.Contains("--prepare-owned-mmd")) return OwnedMmdCommandLine.PrepareAsync(root, args).GetAwaiter().GetResult();
            if (args.Contains("--validate-bundle")) { BundleValidation.Validate(root); return 0; }
            var application = new Application();
            application.Run(new MainWindow(root));
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
