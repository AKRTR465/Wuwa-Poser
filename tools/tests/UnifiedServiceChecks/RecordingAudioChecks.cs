using System.Text;
using System.IO;
using System.Text.Json.Nodes;
using WWPoser.Services;

internal static class RecordingAudioChecks
{
    internal static async Task<int> RunAsync(string root, string fixture)
    {
        const int rate = 48000; int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        var source = new short[rate * 3];
        for (int i = 0; i < source.Length; i++) source[i] = (short)Math.Round(32767 *
            (.25 * Math.Sin(2 * Math.PI * 523 * i / rate) + .2 * Math.Sin(2 * Math.PI * 8179 * i / rate) + .1 * Math.Sin(2 * Math.PI * 18000 * i / rate)));
        string sourcePath = Path.Combine(fixture, "source.wav");
        using (var writer = new BinaryWriter(File.Create(sourcePath)))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + source.Length * 4); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)2); writer.Write(rate); writer.Write(rate * 4); writer.Write((short)4); writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(source.Length * 4); foreach (short sample in source) { writer.Write(sample); writer.Write(sample); }
        }
        JsonObject Event(double pts, double cursor, bool active = true, ulong sequence = 7, double speed = 1, double offset = 0, bool loop = false, double duration = 3, bool fresh = true, string mode = "playing", double volume = 1) => new()
        {
            ["kind"] = "event", ["pts"] = pts, ["cursor"] = cursor, ["active"] = active, ["source"] = "test", ["speed"] = speed,
            ["offset"] = offset, ["loop"] = loop, ["duration"] = duration, ["fresh"] = fresh, ["mode"] = mode,
            ["volume"] = volume, ["epoch"] = 123UL, ["sequence"] = sequence
        };
        async Task<(short[] Pcm, JsonObject Result)> Render(string name, IReadOnlyList<JsonObject> events, double duration)
        {
            var folder = Path.Combine(fixture, name); Directory.CreateDirectory(folder); string journal = Path.Combine(folder, "audio_journal.jsonl");
            var records = new List<JsonObject> { new() { ["kind"] = "header", ["schema"] = 1 }, new() { ["kind"] = "source", ["id"] = "test", ["path"] = Path.GetRelativePath(folder, sourcePath) } };
            records.AddRange(events); records.Add(new() { ["kind"] = "finish", ["frames"] = (long)Math.Round(duration * rate) });
            File.WriteAllLines(journal, records.Select(record => record.ToJsonString()));
            string output = Path.Combine(folder, "music.wav"); var result = await AudioRenderer.RenderAsync(journal, output, Path.Combine(root, "tools", "recording_bin", "ffmpeg.exe"), CancellationToken.None);
            File.WriteAllText(Path.Combine(folder, "result.json"), result.ToJsonString()); byte[] bytes = File.ReadAllBytes(output);
            var pcm = new short[(bytes.Length - 44) / 4]; for (int i = 0; i < pcm.Length; i++) pcm[i] = BitConverter.ToInt16(bytes, 44 + i * 4);
            return (pcm, result);
        }
        var jittered = Enumerable.Range(0, 20).Select(i => Event(i / 10d, i == 0 ? 0 : i / 10d + (i % 2 == 0 ? .08 : -.08), offset: .5)).ToList();
        foreach (var observation in jittered) observation["revision"] = (int)Math.Round(observation["pts"]!.GetValue<double>() * 60);
        jittered.Insert(8, new() { ["kind"] = "event", ["pts"] = .755, ["active"] = false, ["fresh"] = false, ["source"] = null });
        var smooth = await Render("jitter-and-offset", jittered, 2);
        Check(smooth.Result["segments"]!.GetValue<int>() == 1 && smooth.Result["bridged_uncertain_reads"]!.GetValue<int>() == 1, "ordinary observations and one transient read form one continuous music run");
        Check(smooth.Pcm.Take(rate / 2).All(sample => sample == 0), "positive 0.5-second delay emits exactly the intended leading silence");
        int error = smooth.Pcm.Skip(rate / 2).Select((sample, i) => Math.Abs(sample - source[i])).Max();
        Check(error <= 1, "jittered normal-speed music preserves every source sample within PCM quantization, including high frequencies");
        var halfSample = await Render("half-sample-phase", [Event(0, .5 / rate)], 1);
        Check(halfSample.Pcm.Select((sample, i) => Math.Abs(sample - source[i])).Max() <= 1, "a half-sample source phase never duplicates or skips alternating samples");
        var pause = await Render("pause", [Event(0, 0), Event(.5, .5, active: false, sequence: 8, mode: "paused"), Event(1, .5, sequence: 9)], 1.5);
        Check(pause.Result["segments"]!.GetValue<int>() == 3 && pause.Pcm.Skip(rate / 2).Take(rate / 2).All(sample => sample == 0), "an explicit pause remains silence and is never bridged");
        Check(Math.Abs(pause.Pcm[rate + 2345] - source[rate / 2 + 2345]) <= 1, "resume continues from the paused source position");
        var seek = await Render("seek", [Event(0, 0), Event(.5, .5 + 1d / 30, sequence: 8)], 1);
        Check(seek.Result["segments"]!.GetValue<int>() == 2 && Math.Abs(seek.Pcm[rate / 2 + 2345] - source[rate / 2 + rate / 30 + 2345]) <= 1, "a real one-frame seek is preserved despite a small position change");
        var loop = await Render("loop", [Event(0, 0, loop: true, duration: 1), Event(1.1, .1, loop: true, duration: 1)], 2.25);
        Check(loop.Result["segments"]!.GetValue<int>() == 1 && Math.Abs(loop.Pcm[rate + 2345] - source[2345]) <= 1, "loop wrap uses the action duration without creating observation splices");
        var outage = await Render("outage", [Event(0, 0), Event(.5, .5, active: false, fresh: false), Event(1.2, 1.2)], 1.5);
        Check(outage.Result["bridged_uncertain_reads"]!.GetValue<int>() == 0 && outage.Pcm.Skip(rate / 2).Take(rate / 2).All(sample => sample == 0), "a long unverified status outage remains silent");
        var uncertainPause = await Render("read-across-command", [Event(0, 0), Event(.5, .5, active: false, fresh: false), Event(.6, 1.5, sequence: 8)], 1);
        Check(uncertainPause.Result["bridged_uncertain_reads"]!.GetValue<int>() == 0 && uncertainPause.Pcm.Skip(rate / 2).Take(rate / 10).All(sample => sample == 0), "uncertain data across a real control change is not filled with guessed music");
        var volume = await Render("volume", [Event(0, 0), Event(.5, .5, sequence: 8, volume: .25)], 1);
        Check(Math.Abs(volume.Pcm[rate / 2 + 2345] - source[rate / 2 + 2345] * .25) <= 1, "volume changes retain their intended amplitude");
        var fast = await Render("speed-two", [Event(0, 0, speed: 2)], 1);
        double Tone(short[] pcm, double frequency)
        {
            double real = 0, imaginary = 0; const int start = rate / 4, count = rate / 2;
            for (int i = 0; i < count; i++) { double phase = 2 * Math.PI * frequency * i / rate; real += pcm[start + i] * Math.Cos(phase); imaginary += pcm[start + i] * Math.Sin(phase); }
            return 2 * Math.Sqrt(real * real + imaginary * imaginary) / count;
        }
        double fundamental = Tone(fast.Pcm, 1046), alias = Tone(fast.Pcm, 12000);
        Check(fundamental > 7000 && alias / fundamental < .001, "2x speed uses band-limited resampling instead of audible high-frequency aliasing");
        Check(fast.Result["resampler"]!.GetValue<string>() == "soxr", "audio diagnostics identify the high-quality resampler");
        Console.WriteLine($"PASS: {checks} audio render checks; normal-speed source error <= {error} PCM LSB, 2x alias ratio {alias / fundamental:G4}; real FFmpeg, no game/audio device access.");
        return checks;
    }
}
