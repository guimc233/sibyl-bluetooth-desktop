namespace SibylEarbuds.Core.Models;

public enum LightModeType : byte
{
    Steady = 1,     // 常亮
    Breathing = 2,  // 呼吸
    Off = 3         // 关闭
}

public class LightEffectConfig
{
    public LightModeType Mode { get; set; } = LightModeType.Breathing;
    public byte Speed { get; set; } = 3;            // 0 ~ 7
    public byte Brightness { get; set; } = 80;      // 0 ~ 100
    public byte Red { get; set; } = 0;
    public byte Green { get; set; } = 120;
    public byte Blue { get; set; } = 255;           // 默认青蓝天光

    public string HexColor => $"#{Red:X2}{Green:X2}{Blue:X2}";
}
