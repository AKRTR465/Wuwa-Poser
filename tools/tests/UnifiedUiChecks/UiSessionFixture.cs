using System.Reflection;
using System.Text.Json.Nodes;
using WWPoser;
using WWPoser.Services;

internal static class UiSessionFixture
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static T Read<T>(object target, string name) => (T)(target.GetType().GetField(name, Members) is { } field
        ? field.GetValue(target) : target.GetType().GetProperty(name, Members)!.GetValue(target))!;

    // Replace only the game launch in these offline checks. The window reads
    // the same controller and native ownership that a real launch establishes.
    internal static void Own(MainWindow window, string? directory, PlaybackSessionRequest? request = null,
        JsonObject? state = null, bool preview = false, PlaybackSessionPhase phase = PlaybackSessionPhase.Active)
    {
        var player = Read<PlaybackSessionService>(window, "player");
        var owned = Read<OwnedMmdPlaybackService>(player, "owned");
        typeof(OwnedMmdPlaybackService).GetProperty(nameof(owned.ActiveSession))!.SetValue(owned, directory);
        void Set(string name, object? value) => typeof(PlaybackSessionService).GetProperty(name)!.SetValue(player, value);
        Set(nameof(player.ActiveRequest), directory is null ? null : request);
        Set(nameof(player.Phase), directory is null ? PlaybackSessionPhase.Idle : phase);
        Set(nameof(player.IsPreview), directory is not null && preview);
        Set(nameof(player.Snapshot), directory is not null && !preview && phase == PlaybackSessionPhase.Active
            ? (state ?? new JsonObject { ["session_active"] = true }, true) : null);
    }
}
