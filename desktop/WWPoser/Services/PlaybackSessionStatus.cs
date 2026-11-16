namespace WWPoser.Services;

// Keep on-disk runtime formats at this boundary; consumers share one timeline.
public static class PlaybackSessionStatus
{
    public static (JsonObject State, bool Fresh) Read(string directory, double maximumAge = 5)
    {
        var terminal = File.Exists(Path.Combine(directory, "mmd_result.json"));
        var path = Path.Combine(directory, terminal ? "mmd_result.json" : "mmd_status.json");
        var before = File.GetLastWriteTimeUtc(path);
        var state = JsonData.ReadObject(path);
        var after = File.GetLastWriteTimeUtc(path);
        state["terminal"] = terminal || state["mode"]?.GetValue<string>() == "closed";
        if (terminal) state["session_active"] = false;
        state["updated_at_utc"] = after.ToString("O");
        return (state, before == after && (terminal || (DateTime.UtcNow - after).TotalSeconds < maximumAge));
    }
}
