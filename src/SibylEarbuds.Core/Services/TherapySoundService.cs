using SibylEarbuds.Core.Models;

namespace SibylEarbuds.Core.Services;

/// <summary>
/// App 内置自带的 3 个白噪音/疗愈音效类型
/// </summary>
public enum BuiltInSoundType
{
    PinkNoise = 1,      // 粉红噪音 (mp3_fenhong.MP3)
    ForestBirds = 2,    // 森林鸟鸣 (mp3_niaojiao.MP3)
    SummerRain = 3      // 淅沥细雨 (mp3_xiaoyu.MP3)
}

public record BuiltInSoundInfo(
    BuiltInSoundType Type,
    string Name,
    string Description,
    string FileName,
    string IconEmoji
);

/// <summary>
/// 内置音效与疗愈音乐管理器
/// </summary>
public static class BuiltInSoundLibrary
{
    public static readonly IReadOnlyList<BuiltInSoundInfo> Sounds =
    [
        new(
            BuiltInSoundType.PinkNoise,
            "粉红噪音 (Pink Noise)",
            "科学调谐的 1/f 功率谱滤波噪音，舒缓平滑耳鸣，提升深度睡眠与专注力",
            "mp3_fenhong.mp3",
            "🌸"
        ),
        new(
            BuiltInSoundType.ForestBirds,
            "清晨鸟鸣 (Forest Birds)",
            "林间清脆鸟语与晨风声学环境，配合通透模式有效释放长时间佩戴耳机耳压",
            "mp3_niaojiao.mp3",
            "🐦"
        ),
        new(
            BuiltInSoundType.SummerRain,
            "淅沥细雨 (Summer Rain)",
            "舒缓均匀的雨滴声学掩蔽，配合 ANC 深度降噪打造极致宁静工作阅读空间",
            "mp3_xiaoyu.mp3",
            "🌧️"
        )
    ];

    /// <summary>
    /// APK 官方自带的 3 个最经典 DSP 硬件音效调音处理
    /// </summary>
    public static readonly IReadOnlyList<EqConfiguration> OfficialDspTunings =
    [
        new("经典原声 (Classic)", 1, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
        new("澎湃摇滚 (Rock)", 2, [-6, 5, -3, -2, 5, 4, -4, -3, 6, 4]),
        new("温润抒情 (Lyric)", 3, [6, 5, 6, 1, 0, 0, 1, 3, 4, 0])
    ];
}
