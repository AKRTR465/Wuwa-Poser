using System.IO;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using WWPoser.Services;

internal static class AudioFormatChecks
{
    internal static async Task<int> RunLifecycleAsync(string fixture)
    {
        int checks = 0, opened = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        var decoding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool cancelled = false;
        var service = new MediaService(() => new MediaService.MciDevice(command => { if (command.StartsWith("open ")) opened++; return ""; }),
            async (_, stop) => {
                decoding.TrySetResult();
                try { await Task.Delay(Timeout.Infinite, stop); }
                catch (OperationCanceledException) { cancelled = true; throw; }
                return "unreachable.wav";
            });
        service.UpdateContext(fixture, new MusicPreset(Path.Combine(fixture, "pending.flac")));
        await service.StartSynchronizationAsync(); await decoding.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Check(cancelled && opened == 0 && service.LastError is null, "stopping a pending decode cancels it without opening an audio device or retaining an error");

        var timeline = Path.Combine(fixture, "decoded-selection"); Directory.CreateDirectory(timeline);
        File.WriteAllText(Path.Combine(timeline, "player_status.json"), new JsonObject {
            ["clip_epoch"] = 11UL, ["last_sequence"] = 0UL, ["session_active"] = true, ["mode"] = "paused",
            ["seconds"] = 0d, ["speed"] = 1d, ["restore_requested"] = false }.ToJsonString());
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstReady = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondOpened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string first = Path.Combine(fixture, "first.flac"), second = Path.Combine(fixture, "second.mp3"), decoded = Path.Combine(fixture, "decoded.wav");
        var commands = new List<string>();
        service = new MediaService(() => new MediaService.MciDevice(command => {
            commands.Add(command);
            if (command.StartsWith("open ")) secondOpened.TrySetResult();
            return command.EndsWith(" length") ? "10000" : command.EndsWith(" position") ? "0" : command.EndsWith(" mode") ? "paused" : "";
        }), async (path, stop) => {
            if (path == first) { firstStarted.TrySetResult(); return await firstReady.Task.WaitAsync(stop); }
            Check(path == second, "replacement selection uses the latest source"); return decoded;
        });
        try
        {
            service.UpdateContext(timeline, new MusicPreset(first)); await service.StartSynchronizationAsync();
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            service.UpdateContext(timeline, new MusicPreset(second)); firstReady.SetResult("stale.wav");
            await secondOpened.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { await service.StopAsync(); }
        var opens = commands.Where(command => command.StartsWith("open ")).ToArray();
        Check(opens.Length == 1 && opens[0].Contains('"' + decoded + '"') && opens[0].Contains(" type mpegvideo alias "),
            "stale decoding is discarded and the decoded replacement opens with the volume/speed-capable driver");
        Console.WriteLine($"PASS: {checks} asynchronous music decode lifecycle checks; no audio device.");
        return checks;
    }

    internal static async Task<int> RunFormatsAsync(string root, string fixture, bool nativeDevice)
    {
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        var output = Path.Combine(fixture, "音乐格式与空格"); Directory.CreateDirectory(output);
        var decoder = new AudioDecodeService(root, Path.Combine(output, "cache"));
        string ffmpeg = Path.Combine(root, "tools", "recording_bin", "ffmpeg.exe");
        foreach (var (extension, codec) in new[] { ("flac", "flac"), ("wav", "pcm_f32le"), ("mp3", "libmp3lame") })
        {
            var source = Path.Combine(output, "配乐 " + extension + "." + extension);
            var encode = await MediaJson.RunAsync(ffmpeg, new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-n", "-f", "lavfi", "-i",
                "sine=frequency=440:sample_rate=44100:duration=2", "-ac", "1", "-c:a", codec, source });
            Check(encode.ExitCode == 0, "real fixture encoding: " + extension);
            var original = SHA256.HashData(File.ReadAllBytes(source));
            var concurrent = await Task.WhenAll(decoder.PrepareAsync(source), decoder.PrepareAsync(source));
            var decoded = concurrent[0];
            Check(concurrent.All(path => path == decoded) && AudioDecodeService.IsPcmWave(decoded), "concurrent format decoding produces one complete PCM entry: " + extension);
            Check(original.SequenceEqual(SHA256.HashData(File.ReadAllBytes(source))), "the original music is unchanged: " + extension);
            var timestamp = File.GetLastWriteTimeUtc(decoded);
            Check(await decoder.PrepareAsync(source) == decoded && File.GetLastWriteTimeUtc(decoded) == timestamp, "reusing the music does not decode it again: " + extension);
            var probe = await MediaJson.RunAsync(ffmpeg, new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-i", decoded, "-f", "null", "-" });
            Check(probe.ExitCode == 0, "prepared PCM is independently readable: " + extension);
            if (nativeDevice)
            {
                if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Native device checks require Windows.");
                using var device = new MediaService.MciDevice();
                Check(Math.Abs(device.Open(decoded) - 2000) <= 25, "native PCM duration is accurate: " + extension);
                device.SetVolume(.35); device.SetSpeed(.5); device.Seek(500);
                Check(Math.Abs(device.Position - 500) <= 10, "native volume, half speed and seek work: " + extension);
                device.SetVolume(0); device.SetSpeed(2); device.Play();
                await Task.Delay(80); device.Pause();
                Check(!device.IsPlaying, "muted native playback can pause: " + extension);
                device.SetSpeed(.1); device.SetSpeed(4); device.Seek(250);
                Check(Math.Abs(device.Position - 250) <= 10, "the supported speed range and paused seeking work: " + extension);
            }
            File.WriteAllBytes(decoded, [1, 2, 3]);
            Check(await decoder.PrepareAsync(source) == decoded && AudioDecodeService.IsPcmWave(decoded), "a damaged cache is rebuilt: " + extension);
        }
        var sourceWav = Path.Combine(output, "配乐 wav.wav"); var previous = await decoder.PrepareAsync(sourceWav);
        var bytes = File.ReadAllBytes(sourceWav); bytes[^8] ^= 1; File.WriteAllBytes(sourceWav, bytes);
        Check(await decoder.PrepareAsync(sourceWav) != previous, "changing music bytes at the same filename invalidates the cache");
        var broken = Path.Combine(output, "broken.flac"); File.WriteAllText(broken, "invalid audio");
        try { await decoder.PrepareAsync(broken); throw new Exception("invalid audio was accepted"); }
        catch (InvalidDataException error) { Check(error.Message.Contains("配乐解码失败"), "invalid audio reports a useful error"); }
        Check(!Directory.EnumerateFiles(Path.Combine(output, "cache"), "*.partial.wav").Any(), "failed and concurrent decoding leave no partial output");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { await decoder.PrepareAsync(sourceWav, cancelled.Token); throw new Exception("cancelled preparation succeeded"); }
        catch (OperationCanceledException) { checks++; }
        Console.WriteLine($"PASS: {checks} FLAC/WAV/MP3 checks; real FFmpeg, native controls={nativeDevice}, native playback muted. Artifacts: {output}");
        return checks;
    }
}
