using System.IO;
using System.Text.Json.Nodes;
using WWPoser.Services;

static class CharacterFormChecks
{
    public static async Task<int> RunAsync(string root, string body, string face)
    {
        var count = 0;
        void Check(bool value, string label) { if (!value) throw new Exception(label); count++; }
        var catalog = OwnedCharacterCatalog.Load(root);
        var forms = catalog.Characters.Where(character => character.Form is not null).ToArray();
        Check(forms.Length == 4 && catalog.Characters.Count == 84, "four source-backed forms extend the original role/skin catalog");
        var selected = forms.SelectMany(form => new[] { catalog.Select(form.CharacterId[..form.CharacterId.LastIndexOf("-form-", StringComparison.Ordinal)]), form }).ToArray();
        Check(selected.All(character => character.IndependentInstance), "both defaults and alternates of each form family require independent instances");
        var service = new PlaybackSessionService(root);
        var request = new PlaybackSessionRequest(PlaybackSource.MixedActors, selected.Select((character, index) => new PlaybackActorRequest(body, face,
            CharacterId: character.CharacterId, Stage: new(index * 100, index * 10), Placement: new(OffsetX: index))).ToArray());
        var preparation = await service.PrepareAsync(request);
        var directory = preparation["session_dir"]!.GetValue<string>();
        var config = OwnedMmdPlaybackService.Config(directory); var actors = OwnedMmdPlaybackService.ActorConfigs(config);
        Check(actors.Count == 8 && actors.Select(actor => actor["actor_directory"]!.GetValue<string>()).Distinct().Count() == 8, "all four form pairs compile into independent slot directories");
        Check(actors.All(actor => actor["native_physics"]!.GetValue<bool>() && actor["independent_instance"]!.GetValue<bool>()), "native physics and instance isolation survive real MMD preparation");
        var rows = File.ReadAllLines(Path.Combine(directory, "actors.tsv")).Select(line => line.Split('\t')).ToArray();
        Check(rows.All(row => row.Length == 16 && row[15] == "1") && rows.Select(row => row[0]).Distinct().Count() == 8, "managed form requests preserve every independent slot");
        var dark = actors.Single(actor => actor["character_id"]!.GetValue<string>().EndsWith("-form-dark"));
        var darkDirectory = Path.Combine(directory, dark["actor_directory"]!.GetValue<string>());
        var darkProfile = JsonNode.Parse(File.ReadAllText(Path.Combine(darkDirectory, "character_profile.json")))!;
        Check(dark["mesh_asset_name"]!.GetValue<string>() == "R2T1DaniyaMd10011_Up2" && darkProfile["morph_count"]!.GetValue<int>() == 147 && dark["expression_enabled"]!.GetValue<bool>(), "DaniyaDark compiles expression against the 147-morph face leader");
        Check(OwnedMmdPlaybackService.NativeTargetRoutes(config,directory).Count == 8, "every form gets a separate native graph observation route");
        var changed = request with { Actors = request.Actors.Select((actor, index) => index == 0 ? actor with { CharacterId = selected[1].CharacterId } : actor).ToArray() };
        Check(service.RequiresPreparation(directory,changed), "switching one actor's form invalidates the prepared actor selection");
        var preview = new OwnedMmdPlaybackService(root).PreviewConfig(request);
        var previewActors = OwnedMmdPlaybackService.ActorConfigs(preview);
        Check(previewActors.Where(actor => actor["form"] is not null).Count() == 4 && previewActors.All(actor => actor["independent_instance"]!.GetValue<bool>()), "stage preview uses the same form and instance choices as MMD");
        dark["form"]!["child_mesh_paths"]!.AsArray().Clear();
        Check(catalog.Select(dark["character_id"]!.GetValue<string>()).Form!["child_mesh_paths"]!.AsArray().Count == 1 && previewActors.Single(actor => actor["character_id"]!.GetValue<string>().EndsWith("-form-dark"))["form"]!["child_mesh_paths"]!.AsArray().Count == 1,
            "editing a prepared form cannot mutate the catalog or another session");
        Console.WriteLine($"PASS: real offline MMD compilation for all eight form selections: {directory}");
        return count;
    }
}
