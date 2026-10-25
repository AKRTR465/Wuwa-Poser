using System.IO;
using System.IO.Compression;
using System.Text.Json.Nodes;
using WWPoser.Services;

internal static class PresetChecks
{
    public static async Task<int> RunAsync(string fixture)
    {
        var directory = Path.Combine(fixture, "presets"); Directory.CreateDirectory(directory);
        var service = new PresetService(Path.Combine(directory, "cache")); int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        async Task Rejected(Func<Task> run, string message)
        {
            try { await run(); }
            catch (Exception e) when (e is InvalidDataException or FileNotFoundException or OperationCanceledException) { checks++; return; }
            throw new Exception(message);
        }
        string Material(string name, string content) { var path = Path.Combine(directory, name); File.WriteAllText(path, content); return path; }
        var body = Material("舞蹈动作.vmd", "body fixture"); var face = Material("表情.vmd", "face fixture");
        var camera = Material("镜头.vmd", "camera fixture"); var music = Material("音乐.flac", "music fixture");
        var copy = Material("shared.vmd", "body fixture");
        var request = new PlaybackSessionRequest(PlaybackSource.MixedActors, [
            new(body, face, .875, new(12, -23, 4.5, -90, false), 1505, true, "role1505-skin81001505", 11, new(123, -456, 7, 45)),
            new(copy, face, 1.2, new(-31, 22, 5, 90, true), 1505, true, "role1505-skin81001505", 23, new(-78, 90, 2, -45)),
            new("", CharacterId: "role1211-skin81001211-form-dark", Enabled: false)
        ], camera, 2, 1.25, true, 123.5, false, true, "stale/session", 234, false, true);
        var preset = new ShowPreset("测试预设", request, new(music, -1.25, .67), new(true, 60, 1440), ["角色", "重复演员", "暗形态"]);
        var path = Path.Combine(directory, "full.wwpreset"); await service.SaveAsync(path, preset);
        using (var zip = ZipFile.OpenRead(path)) Check(zip.Entries.Count == 5, "shared bytes stored once plus manifest");
        var loaded = await service.LoadAsync(path); var restored = loaded.Preset; var actors = restored.Playback.Actors;
        Check(loaded.Warnings.Count == 0 && actors.Count == 3, "full preset loads all actors");
        Check(actors.Select(a => a.Slot).SequenceEqual(new uint[] { 1, 2, 3 }) && actors[0].CharacterId == actors[1].CharacterId && actors[2].CharacterId == request.Actors[2].CharacterId, "duplicates and alternate forms remain independent and ordered");
        Check(actors[0].Stage == request.Actors[0].Stage && actors[1].Placement == request.Actors[1].Placement && actors[0].Scale == .875 && !actors[2].Enabled, "stage, offsets, scale and enabled flags roundtrip");
        Check(restored.Playback with { Actors = request.Actors, CameraSource = request.CameraSource, StageSession = request.StageSession } == request, "all playback settings roundtrip");
        Check(restored.Recording == preset.Recording && restored.Music?.OffsetSeconds == -1.25 && restored.Music.Volume == .67 && restored.ActorNames!.SequenceEqual(preset.ActorNames!), "recording, music settings and names roundtrip");
        Check(actors[0].BodySource == actors[1].BodySource && File.ReadAllText(actors[0].BodySource) == "body fixture" && File.ReadAllText(restored.Music!.AudioFile) == "music fixture", "resources restore original bytes");
        Check(restored.Playback.StageSession is null, "runtime session paths are stripped");
        File.WriteAllText(actors[0].BodySource, "bad cached bytes");
        Check(File.ReadAllText((await service.LoadAsync(path)).Preset.Playback.Actors[0].BodySource) == "body fixture", "corrupt cache repaired from archive");
        foreach (var source in new[] { body, copy, face, camera, music }) File.Move(source, source + ".moved");
        var relocated = Path.Combine(directory, "relocated.wwpreset"); File.Copy(path, relocated);
        Check((await service.LoadAsync(relocated)).Warnings.Count == 0, "moving preset and removing all original sources works");
        var savedBytes = File.ReadAllBytes(path);
        await Rejected(() => service.SaveAsync(path, preset), "missing source save succeeded");
        Check(savedBytes.SequenceEqual(File.ReadAllBytes(path)), "failed save preserves existing preset");
        using (var cancel = new CancellationTokenSource())
        {
            cancel.Cancel(); await Rejected(() => service.SaveAsync(path, restored, cancel.Token), "cancelled save succeeded");
        }
        Check(savedBytes.SequenceEqual(File.ReadAllBytes(path)) && !Directory.EnumerateFiles(directory, "*.partial").Any(), "cancel preserves old file and removes temporary output");
        var draft = new ShowPreset("未完成", new(PlaybackSource.MixedActors, [new("", Enabled: false)], CameraMode: 2), new("", 2, .5));
        var draftPath = Path.Combine(directory, "draft.wwpreset"); await service.SaveAsync(draftPath, draft);
        Check((await service.LoadAsync(draftPath)).Preset.Music?.OffsetSeconds == 2, "disabled incomplete draft and empty music settings survive");
        await service.SaveAsync(draftPath, draft with { Playback = draft.Playback with { Actors = [] } });
        Check((await service.LoadAsync(draftPath)).Preset.Playback.Actors.Count == 0, "zero actor draft allowed");
        string Variant(string name, Action<ZipArchive> mutate)
        {
            var target = Path.Combine(directory, name + ".wwpreset"); File.Copy(path, target);
            using (var zip = ZipFile.Open(target, ZipArchiveMode.Update)) mutate(zip);
            return target;
        }
        static void ChangeManifest(ZipArchive zip, Action<JsonObject> change)
        {
            var entry = zip.GetEntry("preset.json")!; JsonObject document;
            using (var reader = new StreamReader(entry.Open())) document = JsonNode.Parse(reader.ReadToEnd())!.AsObject();
            change(document); entry.Delete(); using var writer = new StreamWriter(zip.CreateEntry("preset.json").Open()); writer.Write(document.ToJsonString());
        }
        var missing = Variant("missing", zip => zip.Entries.First(e => e.FullName.EndsWith("舞蹈动作.vmd")).Delete());
        var missingResult = await service.LoadAsync(missing);
        Check(missingResult.Preset.Playback.Actors[0].BodySource == "" && missingResult.Warnings.Any(w => w.Contains("缺少素材")), "missing archive resource marked incomplete");
        var corrupt = Variant("corrupt", zip => {
            var entry = zip.Entries.First(e => e.FullName.StartsWith("assets/")); var name = entry.FullName; var length = entry.Length; entry.Delete();
            using var stream = zip.CreateEntry(name).Open(); stream.Write(new byte[length]);
        });
        await Rejected(() => service.LoadAsync(corrupt), "corrupt resource accepted");
        var future = Variant("future", zip => ChangeManifest(zip, doc => doc["schema"] = 99));
        await Rejected(() => service.LoadAsync(future), "future schema accepted");
        var traversal = Variant("traversal", zip => ChangeManifest(zip, doc => doc["assets"]![0]!["entry"] = "../escape.vmd"));
        await Rejected(() => service.LoadAsync(traversal), "path traversal accepted");
        var external = Variant("external", zip => ChangeManifest(zip, doc => doc["preset"]!["playback"]!["cameraSource"] = "C:/outside.vmd"));
        await Rejected(() => service.LoadAsync(external), "external path reference accepted");
        var malformed = Variant("malformed", zip => ChangeManifest(zip, doc => doc["preset"]!["playback"]!["actors"] = null));
        await Rejected(() => service.LoadAsync(malformed), "malformed actor list accepted");
        await Rejected(() => service.SaveAsync(draftPath, draft with { Playback = draft.Playback with { Speed = double.NaN } }), "nonfinite settings accepted");
        var resave = Path.Combine(directory, "resaved.wwpreset"); await service.SaveAsync(resave, restored);
        Check((await service.LoadAsync(resave)).Preset.Playback.Actors.Count == 3, "loaded resources can be resaved without originals");
        Console.WriteLine($"PASS {checks} portable show-preset checks.");
        return checks;
    }
}
