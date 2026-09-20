using System.Text;
using SibylEarbuds.Core.Models;

namespace SibylEarbuds.Core.Protocol;

/// <summary>
/// SIBYL 蓝牙通信数据帧构建器
/// 严格匹配 APK 逆向协议，并保障轻量短包，绝不高频占用射频
/// </summary>
public static class PacketBuilder
{
    public const byte MagicHeader1 = 0xAA;
    public const byte MagicHeader2 = 0x55;

    public const byte SubCmdSet = 0x01;
    public const byte SubCmdGet = 0x02;
    public const byte SubCmdNotify = 0x03;

    /// <summary>
    /// 构建标准协议数据帧：
    /// [0xAA] [0x55] [CmdId] [SubCmd] [Len] [Payload...] [Checksum]
    /// </summary>
    public static byte[] BuildPacket(SibylCommandId commandId, byte subCmd, byte[] payload)
    {
        var len = (byte)payload.Length;
        var packet = new byte[5 + len];
        packet[0] = MagicHeader1;
        packet[1] = MagicHeader2;
        packet[2] = (byte)commandId;
        packet[3] = subCmd;
        packet[4] = len;

        if (len > 0)
        {
            Array.Copy(payload, 0, packet, 5, len);
        }

        // 计算 Checksum: 从 packet[2] 累加到 packet[4+len]
        byte checksum = 0;
        for (int i = 2; i < 5 + len; i++)
        {
            checksum = (byte)(checksum + packet[i]);
        }

        var result = new byte[packet.Length + 1];
        Array.Copy(packet, result, packet.Length);
        result[^1] = checksum;
        return result;
    }

    /// <summary>
    /// 构建 ANC 降噪模式切换包
    /// </summary>
    public static byte[] BuildAncPacket(AncModeType mode, AncDepthLevel depth = AncDepthLevel.Deep)
    {
        // Payload: [Mode: 1=ANC, 2=Normal, 3=Transparency], [Depth: 0/50/100]
        byte[] payload = [(byte)mode, (byte)depth];
        return BuildPacket(SibylCommandId.AncMode, SubCmdSet, payload);
    }

    /// <summary>
    /// 构建 10段 EQ 均衡器数据包
    /// </summary>
    public static byte[] BuildEqPacket(int[] gains)
    {
        if (gains.Length != EqConfiguration.BandCount)
        {
            throw new ArgumentException($"Gains array must have exactly {EqConfiguration.BandCount} elements.");
        }

        // 转化成有符号字节 (范围 -8 ~ +8)
        var payload = new byte[EqConfiguration.BandCount];
        for (int i = 0; i < EqConfiguration.BandCount; i++)
        {
            int clamped = Math.Clamp(gains[i], EqConfiguration.MinGain, EqConfiguration.MaxGain);
            payload[i] = (byte)(sbyte)clamped;
        }

        return BuildPacket(SibylCommandId.Equalizer, SubCmdSet, payload);
    }

    /// <summary>
    /// 构建游戏低延迟模式切换包
    /// </summary>
    public static byte[] BuildGameModePacket(bool enabled)
    {
        return BuildPacket(SibylCommandId.GameMode, SubCmdSet, [(byte)(enabled ? 1 : 0)]);
    }

    /// <summary>
    /// 构建灯效模式设置包
    /// </summary>
    public static byte[] BuildLightModePacket(LightEffectConfig config)
    {
        // Payload: [Mode (1B)], [Speed (1B)], [Brightness (1B)], [R (1B)], [G (1B)], [B (1B)]
        byte[] payload =
        [
            (byte)config.Mode,
            config.Speed,
            config.Brightness,
            config.Red,
            config.Green,
            config.Blue
        ];
        return BuildPacket(SibylCommandId.LightMode, SubCmdSet, payload);
    }

    /// <summary>
    /// 构建按键触控自定义配置包
    /// </summary>
    public static byte[] BuildKeySettingsPacket(EarbudKeySettings settings)
    {
        // Payload: 左右耳分别 4 个手势的映射
        byte[] payload =
        [
            // Left Earbud: Single, Double, Triple, Long
            (byte)settings.LeftSingleTap,
            (byte)settings.LeftDoubleTap,
            (byte)settings.LeftTripleTap,
            (byte)settings.LeftLongPress,
            // Right Earbud: Single, Double, Triple, Long
            (byte)settings.RightSingleTap,
            (byte)settings.RightDoubleTap,
            (byte)settings.RightTripleTap,
            (byte)settings.RightLongPress
        ];
        return BuildPacket(SibylCommandId.KeyFunction, SubCmdSet, payload);
    }

    /// <summary>
    /// 构建寻找耳机包 (播放/停止查找音)
    /// </summary>
    public static byte[] BuildFindEarphonePacket(bool play)
    {
        return BuildPacket(SibylCommandId.FindEarphone, SubCmdSet, [(byte)(play ? 1 : 0)]);
    }

    /// <summary>
    /// 构建定时关机包
    /// </summary>
    public static byte[] BuildTimedShutdownPacket(int minutes)
    {
        return BuildPacket(SibylCommandId.TimedShutdown, SubCmdSet, [(byte)Math.Clamp(minutes, 0, 255)]);
    }

    /// <summary>
    /// 构建睡眠模式开关包
    /// </summary>
    public static byte[] BuildSleepModePacket(bool enabled)
    {
        return BuildPacket(SibylCommandId.SleepMode, SubCmdSet, [(byte)(enabled ? 1 : 0)]);
    }

    /// <summary>
    /// 构建关闭/开启触控防误触包
    /// </summary>
    public static byte[] BuildTouchLockPacket(bool disabled)
    {
        return BuildPacket(SibylCommandId.CloseTouch, SubCmdSet, [(byte)(disabled ? 1 : 0)]);
    }

    /// <summary>
    /// 构建提示音音量调节包
    /// </summary>
    public static byte[] BuildToneVolumePacket(byte volume)
    {
        return BuildPacket(SibylCommandId.ToneVolumeControl, SubCmdSet, [volume]);
    }

    /// <summary>
    /// 构建重命名耳机蓝牙名称包
    /// </summary>
    public static byte[] BuildRenamePacket(string newName)
    {
        var bytes = Encoding.UTF8.GetBytes(newName);
        if (bytes.Length > 20)
        {
            Array.Resize(ref bytes, 20);
        }
        return BuildPacket(SibylCommandId.PairName, SubCmdSet, bytes);
    }

    /// <summary>
    /// 构建重置设置包 (恢复默认或出厂设置)
    /// </summary>
    public static byte[] BuildResetPacket(bool factoryReset)
    {
        var cmd = factoryReset ? SibylCommandId.RestoreFactorySettings : SibylCommandId.RestDefaultSettings;
        return BuildPacket(cmd, SubCmdSet, [0x01]);
    }

    /// <summary>
    /// 构建状态查询包
    /// </summary>
    public static byte[] BuildQueryStatusPacket()
    {
        return BuildPacket(SibylCommandId.QueryStatus, SubCmdGet, []);
    }
}
