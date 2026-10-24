namespace WWPoser.Services;

// Keep on-disk runtime formats at this boundary; consumers share one timeline.
public static class PlaybackSessionStatus
{
    private static readonly string[] Modes = ["empty", "stopped", "playing", "paused", "ended"];
    public static (JsonObject State, bool Fresh) Read(string directory, double maximumAge = 5)
    {
        var owned = File.Exists(Path.Combine(directory, "owned_mmd_config.json"));
        var squad = !owned && File.Exists(Path.Combine(directory, "stage_config.json"));
        var result = owned ? "mmd_result.json" : squad ? "stage_result.json" : "player_status.json";
        var status = owned ? "mmd_status.json" : squad ? "stage_status.json" : "player_status.json";
        var terminal = (owned || squad) && File.Exists(Path.Combine(directory, result));
        var path = Path.Combine(directory, terminal ? result : status);
        var before = File.GetLastWriteTimeUtc(path);
        var raw = JsonData.ReadObject(path);
        var after = File.GetLastWriteTimeUtc(path);
        var state = (JsonObject)(squad ? Contract.O(raw, "mmd_squad") : raw).DeepClone();
        if (state["mode"] is JsonValue mode && mode.TryGetValue<int>(out var index)) state["mode"] = index >= 0 && index < Modes.Length ? Modes[index] : "closed";
        state["terminal"] = terminal || state["mode"]?.GetValue<string>() == "closed";
        if (terminal) state["session_active"] = false;
        // The dynamic host publishes actor rows. Avoid reparsing the much
        // larger immutable bone/clip configuration on every playback poll.
        var mixed = owned && state["actors"] is JsonArray;
        state["source_kind"] = mixed ? "mixed_actors" : owned ? "created_character" : squad ? "current_squad" : "current_character";
        state["updated_at_utc"] = after.ToString("O");
        return (state, before == after && (terminal || (DateTime.UtcNow - after).TotalSeconds < maximumAge));
    }
}
