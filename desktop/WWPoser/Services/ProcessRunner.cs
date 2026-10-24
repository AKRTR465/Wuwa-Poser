using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WWPoser.Services;

public sealed record ProcessResult(int ExitCode, string Output, string Error)
{
    public void EnsureSuccess(string context) { if (ExitCode != 0) throw new InvalidOperationException(context + "：" + Error + Output); }
}
public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string executable, IReadOnlyList<string> arguments, string workingDirectory, CancellationToken cancellation = default)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = workingDirectory, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = info };
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(cancellation); }
        catch (OperationCanceledException) { if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(); throw; }
        return new(process.ExitCode, await output, await error);
    }
}

public sealed record GameIdentity(int Pid, long CreationFileTime, string WindowHandle, uint WindowThread);
public static class GameProcess
{
    [DllImport("kernel32.dll", SetLastError = true)] private static extern nint OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetProcessTimes(nint handle, out long created, out long exited, out long kernel, out long user);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(nint process, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(nint process);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    public static long CreationFileTime(int pid)
    {
        var handle = OpenProcess(0x1000, false, checked((uint)pid));
        if (handle == 0) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法查询进程身份（PID " + pid + "）");
        try { return GetProcessTimes(handle, out var birth, out _, out _, out _) ? birth : throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取进程创建时间"); }
        finally { CloseHandle(handle); }
    }
    public static GameIdentity Current()
    {
        var games = Process.GetProcessesByName("Client-Win64-Shipping");
        try
        {
            if (games.Length != 1) throw new InvalidOperationException("需要一个鸣潮实例并进入角色场景");
            var game = games[0]; var hwnd = game.MainWindowHandle;
            if (hwnd == 0) throw new InvalidOperationException("鸣潮主窗口不可见");
            var thread = GetWindowThreadProcessId(hwnd, out _);
            return new(game.Id, CreationFileTime(game.Id), "0x" + hwnd.ToInt64().ToString("x"), thread);
        }
        finally { foreach (var game in games) game.Dispose(); }
    }
    public static async Task WaitForForegroundAsync(GameIdentity identity, CancellationToken cancellation = default)
    {
        var deadline = Environment.TickCount64 + 15000;
        while (Environment.TickCount64 < deadline)
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out var pid);
            if (pid == identity.Pid) return;
            await Task.Delay(100, cancellation);
        }
        throw new TimeoutException("等待鸣潮前台超时；请切换到游戏后重新准备本次操作");
    }
    public static bool Exited(int pid)
    {
        var handle = OpenProcess(0x101000, false, checked((uint)pid));
        if (handle == 0) return Marshal.GetLastWin32Error() == 87 ? true : throw new Win32Exception();
        try { return WaitForSingleObject(handle, 0) == 0; }
        finally { CloseHandle(handle); }
    }
}

public static class CarrierLoader
{
    public static async Task LoadAsync(string sessionDirectory, CancellationToken cancellation = default)
    {
        using var process = Process.Start(new ProcessStartInfo(Path.Combine(sessionDirectory, "hook_install_v20.exe")) { WorkingDirectory = sessionDirectory, Arguments = "--timeout-seconds 30", UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden })!;
        await process.WaitForExitAsync(cancellation);
        var result = Path.Combine(sessionDirectory, "installer_result.json");
        if (process.ExitCode != 0) throw new InvalidOperationException("加载未完成或临时安装器未撤销：" + (File.Exists(result) ? File.ReadAllText(result) : "exit " + process.ExitCode));
    }
    public static bool NotLoaded(string sessionDirectory)
    {
        var result = Path.Combine(sessionDirectory, "installer_result.json");
        return File.Exists(result) && File.ReadAllText(result).Contains("\"loaded\":false", StringComparison.Ordinal);
    }
}
