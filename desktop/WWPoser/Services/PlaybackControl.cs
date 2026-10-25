using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace WWPoser.Services;

public static class PlaybackControl
{
    private static readonly Dictionary<string, uint> Kinds = new() { ["play"] = 1, ["pause"] = 2, ["seek"] = 3, ["step"] = 4, ["loop"] = 5, ["speed"] = 6, ["stop"] = 7, ["physics"] = 8, ["camera"] = 9, ["offset_x"] = 10, ["offset_y"] = 11, ["height"] = 12, ["yaw"] = 13, ["in_place"] = 14, ["reset_placement"] = 15, ["time_limit"] = 16 };
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SendLocks = new(StringComparer.OrdinalIgnoreCase);
    static PlaybackControl() { Kinds["ground_height"] = 17; }
    public static byte[] Encode(ulong epoch, ulong sequence, string kind, double value = 0, uint targetSlot = 0)
    {
        var bytes = new byte[64]; "WWPCTL1\0"u8.CopyTo(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 1); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 64);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16), epoch); BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(24), sequence);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), Kinds[kind]); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), targetSlot); BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(40), value);
        uint checksum = 2166136261; foreach (var b in bytes.AsSpan(0, 60)) checksum = unchecked((checksum ^ b) * 16777619);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(60), checksum); return bytes;
    }
    public static JsonObject Status(string session) => PlaybackSessionStatus.Read(session).State;
    public static async Task<JsonObject> SendAsync(string session, string kind, double value = 0, CancellationToken cancellation = default, uint targetSlot = 0)
    {
        var gate = SendLocks.GetOrAdd(Path.GetFullPath(session), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            var state = Status(session); var epoch = Contract.N(state, "clip_epoch").GetValue<ulong>();
            if (!Contract.True(state, "session_active")) throw new InvalidOperationException("播放会话已结束");
            var sequence = Math.Max(checked((ulong)DateTime.UtcNow.Ticks), Contract.N(state, "last_sequence").GetValue<ulong>() + 1);
            JsonData.AtomicWrite(Path.Combine(session, "control.dat"), Encode(epoch, sequence, kind, value, targetSlot));
            var deadline = Environment.TickCount64 + 3000;
            while (Environment.TickCount64 < deadline)
            {
                var current = Status(session);
                if (Contract.N(current, "clip_epoch").GetValue<ulong>() != epoch) throw new InvalidOperationException("播放会话已经更换");
                if (Contract.N(current, "last_sequence").GetValue<ulong>() >= sequence) return current;
                await Task.Delay(25, cancellation).ConfigureAwait(false);
            }
            throw new TimeoutException("命令已写入，播放器尚未确认；请查看本次状态");
        }
        finally { gate.Release(); }
    }
}

public static class RecoveryService
{
    private static bool Finished(string session) => File.Exists(Path.Combine(session, "player_status.json")) && Contract.S(PlaybackControl.Status(session), "mode") == "closed" && Contract.True(PlaybackControl.Status(session), "restoration_complete");
    public static async Task FinishCurrentAsync(string session, CancellationToken cancellation = default)
    {
        JsonData.AtomicWrite(Path.Combine(session, "stop.flag"), []);
        var deadline = Environment.TickCount64 + 15000;
        while (!Finished(session) && !CarrierLoader.NotLoaded(session) && Environment.TickCount64 < deadline)
            await Task.Delay(100, cancellation);
        if (!Finished(session)) throw new TimeoutException("尚未确认播放停止与恢复；保留本次会话以便重试。");
    }
}
