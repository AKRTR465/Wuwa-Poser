namespace WWPoser.Services;

/// <summary>WWBOOKMARK2 records a cursor in one exact compiled clip, never a pose dump.</summary>
public static class BookmarkService
{
    private static string ClipHash(string session) => Contract.S(JsonData.ReadObject(Path.Combine(session, "observe_target.json")), "local_clip_sha256");
    private static string TimelineHash(string session)
    {
        var squad = File.Exists(Path.Combine(session, "stage_config.json"));
        var owned = File.Exists(Path.Combine(session, "owned_mmd_config.json"));
        var config = JsonData.ReadObject(Path.Combine(session, squad ? "stage_config.json" : owned ? "owned_mmd_config.json" : "observe_target.json"));
        if (squad) config = Contract.O(config, "mmd_squad");
        var actors = squad ? Contract.A(config, "members").Where(a => Contract.True(a!, "enabled")) : config["actors"] is JsonArray dynamicActors ? dynamicActors.AsEnumerable() : new JsonNode?[] { config };
        var tracks = new JsonArray(actors.Select(a => (JsonNode)new JsonObject { ["slot"] = (a!["slot"] ?? a["target_slot"])?.DeepClone(), ["character"] = a["character_id"]?.DeepClone(), ["profile"] = (a["profile_id"] ?? a["character_profile_id"])?.DeepClone(), ["body"] = a["local_clip_sha256"]?.DeepClone(), ["face"] = a["expression_clip_sha256"]?.DeepClone() }).ToArray());
        return JsonData.CanonicalDigest(new JsonObject { ["actors"] = tracks, ["camera"] = config["camera_clip_sha256"]?.DeepClone() });
    }
    public static void Snapshot(string session, double seconds, string output) =>
        JsonData.Write(output, new JsonObject { ["schema"] = 3, ["format"] = "WWBOOKMARK3", ["timeline_sha256"] = TimelineHash(session), ["seconds"] = seconds });
    public static double SeekTime(string bookmarkPath, string session)
    {
        var bookmark = JsonData.ReadObject(bookmarkPath);
        var compatible = bookmark["timeline_sha256"] is not null ? Contract.S(bookmark, "timeline_sha256") == TimelineHash(session) : Contract.S(bookmark, "clip_sha256") == ClipHash(session);
        Contract.Require(compatible, "书签属于不同动作或演员组合");
        return Contract.D(bookmark, "seconds");
    }
}
