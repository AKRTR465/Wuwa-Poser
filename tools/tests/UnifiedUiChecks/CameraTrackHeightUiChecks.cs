using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using WWPoser;
using WWPoser.Services;

internal static class CameraTrackHeightUiChecks
{
    public static int Run(MainWindow window, string output, Action<Task> wait)
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        T Field<T>(string name) => UiSessionFixture.Read<T>(window, name);
        object? Call(string name, params object?[] args) => typeof(MainWindow).GetMethod(name, Private)!.Invoke(window, args);
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        var original = (PlaybackSessionRequest)Call("CapturePlaybackRequest")!;
        var request = original with { CameraSource = Path.Combine(output, "camera-ui-fixture.vmd"), CameraMode = 2, CameraTrackHeightCm = 0 };
        var directory = Path.Combine(output, "track-height-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var statePath = Path.Combine(directory, "player_status.json"); var commandPath = Path.Combine(directory, "control.dat");
        var state = new JsonObject { ["clip_epoch"] = 421UL, ["last_sequence"] = 0UL, ["session_active"] = true,
            ["mode"] = "paused", ["seconds"] = 6d, ["duration_seconds"] = 20d, ["camera_mode"] = 2, ["camera_track_height_cm"] = 0d, ["placement_height"] = 13d };
        JsonData.Write(statePath, state); Call("SetRequest", request); UiSessionFixture.Own(window, directory, request, state); Call("UpdateActions");
        var commands = new List<double>();
        var acknowledge = Task.Run(async () => {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10)); ulong last = 0;
            while (commands.Count < 3)
            {
                if (File.Exists(commandPath))
                {
                    var bytes = new byte[64];
                    using (var file = new FileStream(commandPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) file.ReadExactly(bytes);
                    var sequence = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24));
                    if (sequence > last)
                    {
                        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(32)) != 18 || BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(36)) != 0)
                            throw new Exception("track-height UI changed another setting or targeted an actor");
                        last = sequence; var value = BinaryPrimitives.ReadDoubleLittleEndian(bytes.AsSpan(40)); commands.Add(value);
                        state["camera_track_height_cm"] = value; state["last_sequence"] = sequence; JsonData.Write(statePath, state);
                    }
                }
                await Task.Delay(5, deadline.Token);
            }
        });
        Field<TextBox>("cameraTrackHeight").Text = "150"; wait((Task)Call("ApplyCameraTrackHeightAsync")!);
        Check(Field<PlaybackSessionRequest>("activeRequest").CameraTrackHeightCm == 150, "applied track height is retained by the active program");
        wait((Task)Call("ShiftCameraTrackHeightAsync", -10d)!);
        Check(Field<TextBox>("cameraTrackHeight").Text == "140", "down button subtracts an absolute 10cm shift");
        wait((Task)Call("ShiftCameraTrackHeightAsync", (object?)null)!); wait(acknowledge);
        Check(commands.SequenceEqual(new[] { 150d, 140d, 0d }), "height input, down and reset send only global track-height commands");
        Check(state["seconds"]!.GetValue<double>() == 6 && state["mode"]!.GetValue<string>() == "paused" && state["placement_height"]!.GetValue<double>() == 13,
            "live track translation preserves the paused cursor and actor placement");
        UiSessionFixture.Own(window, null); Call("SetRequest", original); Call("UpdateActions");
        Console.WriteLine($"PASS {checks} live camera-track height UI checks; synthetic command acknowledgement, no game access.");
        return checks;
    }
}
