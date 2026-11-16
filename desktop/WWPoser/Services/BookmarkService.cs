namespace WWPoser.Services;

/// <summary>WWBOOKMARK3 records a cursor in one exact actor and camera timeline, never a pose dump.</summary>
public static class BookmarkService
{
    private static string TimelineHash(string session)
    {
        var config = JsonData.ReadObject(Path.Combine(session, "owned_mmd_config.json"));
        var tracks = new JsonArray(Contract.A(config, "actors").Select(a => (JsonNode)new JsonObject
        {
            ["slot"] = a!["slot"]?.DeepClone(), ["character"] = a["character_id"]?.DeepClone(), ["profile"] = a["profile_id"]?.DeepClone(),
            ["body"] = a["local_clip_sha256"]?.DeepClone(), ["face"] = a["expression_clip_sha256"]?.DeepClone()
        }).ToArray());
        return JsonData.CanonicalDigest(new JsonObject { ["actors"] = tracks, ["camera"] = config["camera_clip_sha256"]?.DeepClone() });
    }
    public static void Snapshot(string session, double seconds, string output) =>
        JsonData.Write(output, new JsonObject { ["schema"] = 3, ["format"] = "WWBOOKMARK3", ["timeline_sha256"] = TimelineHash(session), ["seconds"] = seconds });
    public static double SeekTime(string bookmarkPath, string session)
    {
        var bookmark = JsonData.ReadObject(bookmarkPath);
        Contract.Require(Contract.S(bookmark, "timeline_sha256") == TimelineHash(session), "书签属于不同动作或演员组合");
        return Contract.D(bookmark, "seconds");
    }
}
