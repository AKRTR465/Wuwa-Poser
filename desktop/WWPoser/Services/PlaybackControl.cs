using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace WWPoser.Services;

public static class PlaybackControl
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> SendLocks = new(StringComparer.OrdinalIgnoreCase);
    public static byte[] Encode(ulong epoch, ulong sequence, string kind, double value = 0, uint targetSlot = 0)
    {
        if (epoch == 0 || sequence == 0) throw new ArgumentException("控制命令的 epoch 和 sequence 必须为正数");
        var id = PlaybackCommands.Validate(kind, value, targetSlot);
        var bytes = new byte[64]; "WWPCTL1\0"u8.CopyTo(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 1); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 64);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16), epoch); BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(24), sequence);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), id); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(36), targetSlot); BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(40), value);
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
            var snapshot = PlaybackSessionStatus.Read(session); var state = snapshot.State;
            if (!snapshot.Fresh && kind != "stop") throw new InvalidOperationException("播放状态已过期，请先停止并恢复本次会话");
            if (Contract.True(state, "restore_requested") && kind != "stop") throw new InvalidOperationException("播放会话正在恢复，请等待恢复完成");
            var epoch = Contract.N(state, "clip_epoch").GetValue<ulong>();
            if (!Contract.True(state, "session_active")) throw new InvalidOperationException("播放会话已结束");
            var processed = state["last_control_sequence"]?.GetValue<ulong>() ?? 0;
            var sequence = Math.Max(checked((ulong)DateTime.UtcNow.Ticks), Math.Max(processed, Contract.N(state, "last_sequence").GetValue<ulong>()) + 1);
            JsonData.AtomicWrite(Path.Combine(session, "control.dat"), Encode(epoch, sequence, kind, value, targetSlot));
            var deadline = Environment.TickCount64 + 3000;
            while (Environment.TickCount64 < deadline)
            {
                var acknowledgement = PlaybackSessionStatus.Read(session); var current = acknowledgement.State;
                if (Contract.N(current, "clip_epoch").GetValue<ulong>() != epoch) throw new InvalidOperationException("播放会话已经更换");
                if (acknowledgement.Fresh && current["last_control_sequence"] is JsonNode resultSequence)
                {
                    if (resultSequence.GetValue<ulong>() == sequence)
                    {
                        var code = Contract.N(current, "last_control_code").GetValue<uint>();
                        if (code != 0) throw new InvalidOperationException($"播放器拒绝 {kind} 命令（序号 {sequence}，错误码 {code}）");
                        return current;
                    }
                }
                else if (acknowledgement.Fresh && current["last_control_sequence"] is null && Contract.N(current, "last_sequence").GetValue<ulong>() >= sequence) return current; // Older hosts only acknowledge accepted commands.
                await Task.Delay(25, cancellation).ConfigureAwait(false);
            }
            throw new TimeoutException("命令已写入，播放器尚未确认；请查看本次状态");
        }
        finally { gate.Release(); }
    }
}
