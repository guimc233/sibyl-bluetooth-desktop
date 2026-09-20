namespace SibylEarbuds.Core.Models;

/// <summary>
/// 10段音乐均衡器数据模型
/// 频点固定为: 31Hz, 62Hz, 125Hz, 250Hz, 500Hz, 1kHz, 2kHz, 4kHz, 8kHz, 16kHz
/// 增益范围: -8dB ~ +8dB
/// </summary>
public class EqConfiguration
{
    public const int BandCount = 10;
    public const int MinGain = -8;
    public const int MaxGain = 8;

    public static readonly int[] Frequencies = [31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000];

    public string Name { get; set; } = "默认";
    public int PresetId { get; set; } = 1;
    public int[] Gains { get; set; } = new int[BandCount];

    public EqConfiguration() { }

    public EqConfiguration(string name, int presetId, int[] gains)
    {
        Name = name;
        PresetId = presetId;
        Gains = (int[])gains.Clone();
    }

    public static List<EqConfiguration> GetDefaultPresets()
    {
        return
        [
            new EqConfiguration("经典 (默认)", 1, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
            new EqConfiguration("摇滚 (Rock)", 2, [-6, 5, -3, -2, 5, 4, -4, -3, 6, 4]),
            new EqConfiguration("抒情 (Lyric)", 3, [6, 5, 6, 1, 0, 0, 1, 3, 4, 0]),
            new EqConfiguration("流行 (Pop)", 4, [2, 1, 0, -1, -2, 0, 2, 4, 3, 2]),
            new EqConfiguration("超重低音 (Bass)", 5, [7, 6, 5, 3, 1, 0, 0, 0, 0, 0]),
            new EqConfiguration("人声清晰 (Vocal)", 6, [-2, -1, 0, 2, 4, 5, 3, 1, 0, 0]),
            new EqConfiguration("自定义 (Custom)", 0, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0])
        ];
    }
}
