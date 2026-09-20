namespace SibylEarbuds.Core.Models;

/// <summary>
/// 按键/触控触发的手势动作枚举
/// </summary>
public enum KeyFunctionType : byte
{
    None = 0,               // 无作用
    PlayPause = 1,          // 播放/暂停
    PreviousTrack = 2,      // 上一曲
    NextTrack = 3,          // 下一曲
    VoiceAssistant = 4,     // 语音助手
    VolumeUp = 5,           // 音量+
    VolumeDown = 6,         // 音量-
    GameMode = 7,           // 游戏模式切换
    AncToggle = 8,          // ANC降噪切换
    AiAssistant = 15,       // AI助手
    EqCycle = 16            // EQ音效切换
}

/// <summary>
/// 触控手势类型
/// </summary>
public enum TouchGestureType : byte
{
    SingleTap = 1,   // 单击
    DoubleTap = 2,   // 双击
    TripleTap = 3,   // 三击
    LongPress = 5    // 长按
}

/// <summary>
/// 耳机侧向
/// </summary>
public enum EarbudSide : byte
{
    Left = 0,
    Right = 1
}

/// <summary>
/// 单个按键映射定义
/// </summary>
public record KeyMappingItem(
    EarbudSide Side,
    TouchGestureType Gesture,
    byte EventId,
    KeyFunctionType Function
);

public class EarbudKeySettings
{
    public KeyFunctionType LeftSingleTap { get; set; } = KeyFunctionType.PlayPause;
    public KeyFunctionType LeftDoubleTap { get; set; } = KeyFunctionType.PreviousTrack;
    public KeyFunctionType LeftTripleTap { get; set; } = KeyFunctionType.VoiceAssistant;
    public KeyFunctionType LeftLongPress { get; set; } = KeyFunctionType.AncToggle;

    public KeyFunctionType RightSingleTap { get; set; } = KeyFunctionType.PlayPause;
    public KeyFunctionType RightDoubleTap { get; set; } = KeyFunctionType.NextTrack;
    public KeyFunctionType RightTripleTap { get; set; } = KeyFunctionType.GameMode;
    public KeyFunctionType RightLongPress { get; set; } = KeyFunctionType.AncToggle;
}
