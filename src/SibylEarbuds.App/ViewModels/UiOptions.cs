using SibylEarbuds.Core.Models;

namespace SibylEarbuds.App.ViewModels;

public sealed record ShutdownOption(string Display, int Minutes)
{
    public override string ToString() => Display;
}

public sealed record KeyFunctionOption(string Display, KeyFunctionType Value)
{
    public override string ToString() => Display;
}

public static class OptionCatalog
{
    public static IReadOnlyList<ShutdownOption> Shutdowns { get; } =
    [
        new("不开启", 0),
        new("15 分钟", 15),
        new("30 分钟", 30),
        new("60 分钟", 60)
    ];

    public static IReadOnlyList<KeyFunctionOption> KeyFunctions { get; } =
    [
        new("无作用", KeyFunctionType.None),
        new("播放 / 暂停", KeyFunctionType.PlayPause),
        new("上一曲", KeyFunctionType.PreviousTrack),
        new("下一曲", KeyFunctionType.NextTrack),
        new("语音助手", KeyFunctionType.VoiceAssistant),
        new("音量 +", KeyFunctionType.VolumeUp),
        new("音量 -", KeyFunctionType.VolumeDown),
        new("游戏模式", KeyFunctionType.GameMode),
        new("降噪切换", KeyFunctionType.AncToggle),
        new("EQ 循环", KeyFunctionType.EqCycle)
    ];
}
