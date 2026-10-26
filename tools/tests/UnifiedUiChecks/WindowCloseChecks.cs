using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WWPoser;
using WWPoser.Services;

internal static class WindowCloseChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(MainWindow window, string name) => (T)typeof(MainWindow).GetField(name, Private)!.GetValue(window)!;
    private static void Set(MainWindow window, string name, object? value) => typeof(MainWindow).GetField(name, Private)!.SetValue(window, value);
    private static MainWindow Window(string root, RecordingService? recording = null)
    {
        var window = recording is null ? new MainWindow(root, startPolling: false) : new MainWindow(root, false, recording);
        // Exercise WPF's real Closing event without showing a window or running
        // the polling timer. Calling the cleanup method alone misses reentrancy.
        window.Closing += (CancelEventHandler)Delegate.CreateDelegate(typeof(CancelEventHandler), window,
            typeof(MainWindow).GetMethod("CloseRequested", Private)!);
        return window;
    }
    private static async Task Until(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!predicate()) await Task.Delay(10, timeout.Token);
    }
    private static void Dispose(MainWindow window)
    {
        Set(window, "closeReady", true); window.Close();
    }

    internal static int Run(string root, string output, Action<Task> wait)
    {
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        {
            var window = Window(root); bool closed = false; window.Closed += (_, _) => closed = true;
            try
            {
                window.Close();
                Check(!Field<TextBlock>(window, "status").Text.StartsWith("关闭尚未完成"), "idle close reentered WPF Closing: " + Field<TextBlock>(window, "status").Text);
                wait(Until(() => closed));
                Check(closed && Field<CancellationTokenSource>(window, "lifetime").IsCancellationRequested, "one idle close request completes shutdown");
            }
            finally { if (!closed) Dispose(window); }
        }
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var recording = new RecordingService(root, async (_, _, _, stop) => {
                started.SetResult();
                try { await Task.Delay(Timeout.Infinite, stop); }
                catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
                stopped.SetResult(); await finish.Task;
            });
            var window = Window(root, recording); int closed = 0; window.Closed += (_, _) => closed++;
            try
            {
                wait(recording.StartAsync(Path.Combine(output, "close-recording"))); wait(started.Task);
                window.Close(); wait(stopped.Task);
                Check(closed == 0, "close waits for pending recording finalization");
                window.Close(); Check(closed == 0, "repeated close does not bypass pending cleanup");
                finish.SetResult(); wait(Until(() => closed != 0));
                Check(closed == 1 && !recording.IsBusy, "recording completion automatically closes the window once");
            }
            finally { finish.TrySetResult(); wait(recording.StopAsync()); if (closed == 0) Dispose(window); }
        }
        {
            var window = Window(root); bool closed = false; int cancellations = 0;
            window.Closed += (_, _) => closed = true;
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Func<Task> operation = async () => {
                var token = Field<CancellationTokenSource>(window, "operationCancellation").Token;
                using var registration = token.Register(() => { cancellations++; cancelled.TrySetResult(); });
                started.SetResult();
                try { await Task.Delay(Timeout.Infinite, token); }
                catch (OperationCanceledException) { await finish.Task; throw; }
            };
            var button = (Button)typeof(MainWindow).GetMethod("Action", Private)!.Invoke(window,
                ["synthetic cancellable operation", operation, null, null, false, false])!;
            try
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); wait(started.Task);
                window.Close(); wait(cancelled.Task);
                Check(!closed && Field<bool>(window, "busy"), "close waits for the active operation to finish cancellation");
                window.Close(); Check(cancellations == 1, "repeated close requests share one cancellation and shutdown");
                finish.SetResult(); wait(Until(() => closed));
                Check(closed && !Field<bool>(window, "busy"), "a busy window closes automatically after cancellation without another click");
            }
            finally { finish.TrySetResult(); if (!closed) { wait(Until(() => !Field<bool>(window, "busy"))); Dispose(window); } }
        }
        {
            var window = Window(root); bool closed = false; window.Closed += (_, _) => closed = true;
            typeof(MediaService).GetField("worker", Private)!.SetValue(Field<MediaService>(window, "music"),
                Task.FromException(new IOException("synthetic shutdown failure")));
            try
            {
                window.Close(); wait(Until(() => Field<TextBlock>(window, "status").Text.Contains("synthetic shutdown failure")));
                Check(!closed && window.IsEnabled && !Field<bool>(window, "busy"), "a cleanup failure keeps the window available for retry");
                Check(!Field<CancellationTokenSource>(window, "lifetime").IsCancellationRequested, "failed cleanup does not retire recovery ownership");
                window.Close(); wait(Until(() => closed));
                Check(closed, "a corrected cleanup failure can be retried without WPF close reentrancy");
            }
            finally { if (!closed) Dispose(window); }
        }
        Console.WriteLine($"PASS {checks} real WPF close lifecycle checks; no windows shown or game access.");
        return checks;
    }
}
