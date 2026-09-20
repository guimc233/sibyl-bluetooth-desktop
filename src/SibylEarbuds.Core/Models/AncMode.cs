namespace SibylEarbuds.Core.Models;

/// <summary>
/// 耳机降噪模式
/// </summary>
public enum AncModeType : byte
{
    /// <summary>
    /// 开启主动降噪
    /// </summary>
    NoiseReduction = 1,

    /// <summary>
    /// 普通/关闭降噪
    /// </summary>
    Normal = 2,

    /// <summary>
    /// 通透模式
    /// </summary>
    Transparency = 3
}

/// <summary>
/// 降噪深度等级
/// </summary>
public enum AncDepthLevel : byte
{
    Standard = 0,
    Comfortable = 50,  // 舒适降噪
    Deep = 100         // 深度降噪
}

/// <summary>
/// ANC 状态模型
/// </summary>
public record AncState(AncModeType Mode, AncDepthLevel Depth = AncDepthLevel.Deep);
