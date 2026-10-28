using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using WWPoser.Services;

internal static class GazeChecks
{
    public static async Task<int> RunAsync(string root, string fixture, string body, string face, string camera)
    {
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        var gaze = Path.Combine(fixture, "gaze-7s.vmd");
        var embedded = Path.Combine(fixture, "embedded-gaze.vmd");
        void WriteGaze(string path, bool centre, bool pupilMorph = false)
        {
            using var writer = new BinaryWriter(File.Create(path));
            void Text(byte[] data, int size) { var bytes = new byte[size]; data.CopyTo(bytes, 0); writer.Write(bytes); }
            Text(Encoding.ASCII.GetBytes("Vocaloid Motion Data 0002"), 30); writer.Write(new byte[20]); writer.Write(centre ? 4u : 3u);
            void Key(byte[] name, uint frame, float x, float y, float z, float w)
            {
                Text(name, 15); writer.Write(frame); writer.Write(0f); writer.Write(0f); writer.Write(0f);
                writer.Write(x); writer.Write(y); writer.Write(z); writer.Write(w);
                for (var i = 0; i < 16; i++) writer.Write(new byte[] { 20, 20, 107, 107 });
            }
            if (centre) Key([0x83, 0x5a, 0x83, 0x93, 0x83, 0x5e, 0x81, 0x5b], 0, 0, 0, 0, 1);
            byte[] both = [0x97, 0xbc, 0x96, 0xda]; // CP932 両目
            Key(both, 0, 0, 0, 0, 1);
            Key(both, 180, 0, MathF.Sin(MathF.PI / 48), 0, MathF.Cos(MathF.PI / 48)); // right 7.5 degrees
            Key(both, 210, MathF.Sin(MathF.PI / 24), 0, 0, MathF.Cos(MathF.PI / 24)); // up 15 degrees
            writer.Write(pupilMorph ? 1u : 0u);
            if (pupilMorph) { Text(Encoding.ASCII.GetBytes("Pupil_R"), 15); writer.Write(0u); writer.Write(.8f); }
            for (var i = 0; i < 4; i++) writer.Write(0u);
        }
        WriteGaze(gaze, false); WriteGaze(embedded, true);
        var character = OwnedCharacterCatalog.Load(root).Select().CharacterId;
        var service = new PlaybackSessionService(root);
        var request = new PlaybackSessionRequest([new(body, face, CharacterId: character, GazeSource: gaze)]);
        var preview = new OwnedMmdPlaybackService(root).PreviewConfig(new([new("", CharacterId: character)], CameraMode: 2));
        Check(Contract.True(preview, "preview"), "stage preview does not require a body or VMD camera source");
        async Task<(string Directory, JsonObject Actor)> Prepare(PlaybackSessionRequest input)
        {
            var prepared = await service.PrepareAsync(input); var directory = prepared["session_dir"]!.GetValue<string>();
            return (directory, OwnedMmdPlaybackService.ActorConfigs(OwnedMmdPlaybackService.Config(directory))[0]);
        }
        float Weight((string Directory, JsonObject Actor) prepared, string name, double seconds)
        {
            var actorDirectory = Path.Combine(prepared.Directory, prepared.Actor["actor_directory"]!.GetValue<string>());
            var names = prepared.Actor["expression_target_names"]!.AsArray().Select(x => x!.GetValue<string>()).ToArray();
            using var reader = new BinaryReader(File.OpenRead(Path.Combine(actorDirectory, "expression.wwexpr")));
            reader.BaseStream.Position = 32 + (long)(Math.Round(seconds * 60) * (names.Length + 15) + Array.IndexOf(names, name)) * 4;
            return reader.ReadSingle();
        }
        var separate = await Prepare(request);
        Check(separate.Actor["gaze_enabled"]!.GetValue<bool>() && separate.Actor["expression_enabled"]!.GetValue<bool>() && separate.Actor["gaze_duration_seconds"]!.GetValue<double>() == 7,
            "gaze-only bone input enables expression runtime and retains its own duration");
        Check(OwnedMmdPlaybackService.Config(separate.Directory)["duration_seconds"]!.GetValue<double>() == 7, "longer gaze extends the shared body/face timeline");
        Check(separate.Actor["expression_mapped_names"]!.AsArray().Any(n => n!.GetValue<string>() == "Pupil_R"), "runtime owns pupil shapes as mapped targets");
        Check(Math.Abs(Weight(separate, "Pupil_R", 6) - .5f) < .002 && Weight(separate, "Pupil_L", 6) == 0 && Weight(separate, "Pupil_R", 0) == 0,
            "gaze samples neutral and right directions at exact synchronized cursor times");
        Check(Weight(separate, "Pupil_Up", 7) > .999 && Weight(separate, "Pupil_Down", 7) == 0 && Weight(separate, "A", 6) > .999,
            "gaze coexists with held mouth weights and separates vertical directions");
        Check(service.RequiresPreparation(separate.Directory, request with { Actors = [request.Actors[0] with { GazeStrength = 2 }] }), "strength changes invalidate compiled clips");
        var amplified = await Prepare(request with { Actors = [request.Actors[0] with { GazeStrength = 2 }] });
        Check(Weight(amplified, "Pupil_R", 6) > .999, "strength is applied and bounded to legal morph weights");
        var cached = await Prepare(request);
        Check(Weight(cached, "Pupil_R", 6) == Weight(separate, "Pupil_R", 6), "cached gaze retains all pupil weights");
        var faceEmbedded = await Prepare(request with { Actors = [new(body, gaze, CharacterId: character)] });
        Check(Weight(faceEmbedded, "Pupil_R", 6) > .499, "gaze automatically reads eyes from the face VMD");
        var bodyEmbedded = await Prepare(request with { Actors = [new(embedded, face, CharacterId: character)] });
        Check(Weight(bodyEmbedded, "Pupil_R", 6) > .499, "body eye tracks are used when a separate face has no eyes");
        var facePriority = await Prepare(request with { Actors = [new(embedded, gaze, CharacterId: character)] });
        Check(Math.Abs(Weight(facePriority, "Pupil_R", 6) - .5f) < .002, "face eye tracks take priority without doubling body gaze");
        var authored = Path.Combine(fixture, "authored-pupils.vmd"); WriteGaze(authored, false, true);
        var preserved = await Prepare(request with { Actors = [new(body, authored, CharacterId: character)] });
        Check(Math.Abs(Weight(preserved, "Pupil_R", 6) - .8f) < .002, "automatic extraction preserves explicitly authored pupil shapes");
        var overridden = await Prepare(request with { Actors = [new(body, authored, CharacterId: character, GazeSource: gaze)] });
        Check(Math.Abs(Weight(overridden, "Pupil_R", 6) - .5f) < .002, "separate gaze overrides conflicting authored pupil direction without accumulation");
        try { await Prepare(request with { Actors = [request.Actors[0] with { GazeSource = camera }] }); throw new Exception("camera accepted as gaze"); }
        catch (InvalidOperationException error) { Check(error.Message.Contains("目线"), "misplaced camera is rejected with a gaze diagnostic"); }
        File.Move(gaze, gaze + ".moved");
        var restored = service.ReadPreparation(separate.Directory);
        Check(File.Exists(restored.Actors[0].GazeSource) && !service.RequiresPreparation(separate.Directory, restored), "gaze snapshots survive original file relocation and preserve preparation reuse");
        var legacy = System.Text.Json.JsonSerializer.Deserialize<PlaybackActorRequest>("{\"BodySource\":\"x\"}")!;
        Check(legacy.GazeSource is null && legacy.GazeStrength == 1, "old requests keep an optional gaze track and default strength");
        Console.WriteLine($"PASS: {checks} gaze compilation, clock, cache and snapshot checks; no game access.");
        return checks;
    }
}
