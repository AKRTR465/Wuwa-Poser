namespace WWPoser.Services;

public sealed record OwnedMmdDisplayStatus(string Phase, string Message);

// File/status presentation only. No process, window, or native-runtime access.
public static class OwnedMmdPresentation
{
    public static string PreparationSummary(string sourceName, string directory, JsonObject config, bool current)
    {
        var clip = config["clip_duration_seconds"]!.GetValue<double>();
        var duration = config["duration_ms"]!.GetValue<int>() / 1000d;
        var speed = config["speed"]!.GetValue<double>();
        var loop = config["loop"]!.GetValue<bool>();
        var durationLabel = duration == 0 ? "不限时" : FormattableString.Invariant($"{duration:F3} 秒");
        var physics = config["native_physics"]?.GetValue<bool>() != false ? "游戏原生物理" : Contract.True(config, "physics_enabled") ? "衣物惯性增强" : "直接动作";
        return FormattableString.Invariant($"角色：{config["character_display_name"]}\n素材：{sourceName}\n身体时长：{clip:F3} 秒 · {config["frame_count"]} 帧 / {config["fps"]} FPS · {config["bone_count"]} 骨\n本次播放：{durationLabel} · {speed:0.###} 倍速 · {(loop ? "循环" : "单次")} · {physics}\n准备目录：{directory}\n{(current ? "已准备，可核对后开始。" : "角色或素材已修改，开始时将重新准备。")}");
    }

    public static OwnedMmdDisplayStatus RuntimeStatus(JsonObject state, bool finished, bool terminal)
    {
        var frames = state["body_frames"] ?? state["frames"] ?? JsonValue.Create(0);
        if (finished)
            return new("停止恢复 · 待确认画面", "停止恢复结果已记录。确认画面后点击“停止并恢复”完成本次操作。");
        if (Contract.True(state, "failed"))
            return new($"动作已中止 · {frames} 帧", $"身体播放报告错误 {state["error_code"]}；请停止并恢复，保留当前会话直到收尾完成。");
        if (terminal || Contract.True(state, "closing"))
            return new($"停止恢复中 · {frames} 帧", "正在等待身体姿态恢复和角色移除。");
        if (Contract.True(state, "static_display"))
            return new("角色已显示，正在准备动作", "角色加载与骨架就绪后自动开始；可随时点击“停止并恢复”。");
        if (Contract.True(state, "writing"))
            return new($"动作播放中 · {frames} 帧", "请在游戏画面确认动作、脚底和服装，完成后停止并恢复。");
        if (Contract.True(state, "ready"))
            return new($"身体动作已就绪 · {frames} 帧", "角色加载与骨架就绪后自动开始；可随时停止并恢复。");
        return new("正在创建、加载所选角色", "角色加载与骨架就绪后自动开始；可通过“停止并恢复”结束本次操作。");
    }
}
