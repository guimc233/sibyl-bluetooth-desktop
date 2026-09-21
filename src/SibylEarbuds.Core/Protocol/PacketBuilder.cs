using System.Text;
using SibylEarbuds.Core.Models;

namespace SibylEarbuds.Core.Protocol;

/// <summary>
/// SIBYL 蓝牙通信数据帧构建器 (严格匹配官方 APK ProductClient.createCMDData)
/// 格式: [0xFF] [SeqIndex] [Len] [CmdId] [Payload...] [0xAA]
/// 其中 Len = (payload?.Length ?? 0) + 1
/// </summary>
public static class PacketBuilder
{
    public const byte StartByte = 0xFF;
    public const byte EndByte = 0xAA; // 官方 TransportLayerPacket.SYNC_WORD = -86 (0xAA)

    private static int _seqIndex = 0;

    /// <summary>
    /// 构建官方标准协议数据帧：
    /// [0xFF] [SeqIndex] [Len] [CmdId] [Payload...] [0xAA]
    /// </summary>
    public static byte[] BuildPacket(SibylCommandId commandId, byte[]? payload = null)
    {
        int payloadLen = payload?.Length ?? 0;
        byte[] packet = new byte[payloadLen + 5];

        packet[0] = StartByte;
        packet[1] = (byte)(Interlocked.Increment(ref _seqIndex) & 0xFF);
        packet[2] = (byte)(payloadLen + 1); // 包含 CmdId 本身在内的数据长度
        packet[3] = (byte)commandId;

        if (payload != null && payloadLen > 0)
        {
            Array.Copy(payload, 0, packet, 4, payloadLen);
        }

        packet[^1] = EndByte;
        return packet;
    }

    /// <summary>
    /// 构建 ANC 降噪模式切换包 (官方 ProductClient.ancModelChange)
    /// 注意：官方协议规范中，仅在降噪模式 (NoiseReduction=1) 下第二字节才为深度 (1=舒适, 2=深度)；
    /// 在普通模式 (2) 和通透模式 (3) 下，第二字节必须为 0，否则耳机会误触发深度降噪！
    /// </summary>
    public static byte[] BuildAncPacket(AncModeType mode, AncDepthLevel depth = AncDepthLevel.Deep)
    {
        byte depthByte = mode == AncModeType.NoiseReduction ? (byte)depth : (byte)0;
        byte[] payload = [(byte)mode, depthByte];
        return BuildPacket(SibylCommandId.AncMode, payload);
    }

    /// <summary>
    /// 构建 10段 EQ 均衡器数据包 (官方 ProductClient.getSetEqCMD)
    /// </summary>
    public static byte[] BuildEqPacket(int[] gains, int presetType = 1)
    {
        var payload = new byte[1 + EqConfiguration.BandCount];
        payload[0] = (byte)presetType;
        for (int i = 0; i < EqConfiguration.BandCount && i < gains.Length; i++)
        {
            int clamped = Math.Clamp(gains[i], EqConfiguration.MinGain, EqConfiguration.MaxGain);
            payload[i + 1] = (byte)(sbyte)clamped;
        }
        return BuildPacket(SibylCommandId.Equalizer, payload);
    }

    /// <summary>
    /// 构建游戏低延迟模式切换包 (官方 ProductClient.gameSwitch)
    /// </summary>
    public static byte[] BuildGameModePacket(bool enabled)
    {
        return BuildPacket(SibylCommandId.GameMode, [(byte)(enabled ? 1 : 0)]);
    }

    /// <summary>
    /// 构建灯效模式设置包 (官方 ProductClient.ledChange)
    /// </summary>
    public static byte[] BuildLightModePacket(LightEffectConfig config)
    {
        byte[] payload =
        [
            (byte)config.Mode,
            config.Speed,
            config.Brightness,
            config.Red,
            config.Green,
            config.Blue
        ];
        return BuildPacket(SibylCommandId.LightMode, payload);
    }

    /// <summary>
    /// 构建按键触控自定义配置包 (单手势更新: cmdID + funID)
    /// </summary>
    public static byte[] BuildKeySettingPacket(byte gestureEventId, byte functionCmd)
    {
        return BuildPacket(SibylCommandId.KeyFunction, [gestureEventId, functionCmd]);
    }

    /// <summary>
    /// 构建批量按键手势包（支持 单/双/三/四击/长按）
    /// </summary>
    public static byte[] BuildKeySettingsPacket(EarbudKeySettings settings, bool supportsQuadruple = true)
    {
        byte[] payload = supportsQuadruple
            ? [
                1, (byte)settings.LeftSingleTap,
                2, (byte)settings.LeftDoubleTap,
                3, (byte)settings.LeftTripleTap,
                4, (byte)settings.LeftQuadrupleTap,
                5, (byte)settings.LeftLongPress,
                17, (byte)settings.RightSingleTap,
                18, (byte)settings.RightDoubleTap,
                19, (byte)settings.RightTripleTap,
                20, (byte)settings.RightQuadrupleTap,
                21, (byte)settings.RightLongPress
            ]
            : [
                1, (byte)settings.LeftSingleTap,
                2, (byte)settings.LeftDoubleTap,
                3, (byte)settings.LeftTripleTap,
                5, (byte)settings.LeftLongPress,
                17, (byte)settings.RightSingleTap,
                18, (byte)settings.RightDoubleTap,
                19, (byte)settings.RightTripleTap,
                21, (byte)settings.RightLongPress
            ];
        return BuildPacket(SibylCommandId.KeyFunction, payload);
    }

    /// <summary>
    /// 构建定时关机包 (官方 ProductClient.setPowerTime，2字节 Little-Endian 分钟数)
    /// </summary>
    public static byte[] BuildTimedShutdownPacket(int minutes)
    {
        byte[] payload = [(byte)(minutes & 255), (byte)((minutes >> 8) & 255)];
        return BuildPacket(SibylCommandId.TimedShutdown, payload);
    }

    /// <summary>
    /// 构建睡眠模式开关包 (官方 ProductClient.sendSleepMode)
    /// </summary>
    public static byte[] BuildSleepModePacket(bool enabled)
    {
        return BuildPacket(SibylCommandId.SleepMode, [(byte)(enabled ? 1 : 0)]);
    }

    /// <summary>
    /// 构建寻找耳机包 (通过提示音音量或属性触发发声)
    /// </summary>
    public static byte[] BuildFindEarphonePacket(bool play)
    {
        return BuildPacket(SibylCommandId.ToneVolumeControl, [(byte)(play ? 100 : 0)]);
    }

    /// <summary>
    /// 构建关闭/开启触控防误触包 (官方 ProductClient.btnTouchSwitch)
    /// </summary>
    public static byte[] BuildTouchLockPacket(bool disabled)
    {
        return BuildPacket(SibylCommandId.CloseTouch, [(byte)(disabled ? 1 : 0)]);
    }

    /// <summary>
    /// 构建提示音音量调节包 (官方 ProductClient.setPromptVolume)
    /// </summary>
    public static byte[] BuildToneVolumePacket(byte volume)
    {
        return BuildPacket(SibylCommandId.ToneVolumeControl, [volume]);
    }

    /// <summary>
    /// 构建重命名耳机蓝牙名称包 (官方 ProductClient.setPairName)
    /// </summary>
    public static byte[] BuildRenamePacket(string newName)
    {
        var bytes = Encoding.UTF8.GetBytes(newName);
        if (bytes.Length > 20)
        {
            Array.Resize(ref bytes, 20);
        }
        return BuildPacket(SibylCommandId.PairName, bytes);
    }

    /// <summary>
    /// 构建重置设置包 (官方 ProductClient.resetDefaultSetting / factoryReset)
    /// </summary>
    public static byte[] BuildResetPacket(bool factoryReset)
    {
        var cmd = factoryReset ? SibylCommandId.RestoreFactorySettings : SibylCommandId.RestDefaultSettings;
        return BuildPacket(cmd, null);
    }

    /// <summary>
    /// 构建状态查询包 (官方 ProductClient.getAttrValue，CMDID_GETINFO = 0xFA)
    /// </summary>
    public static byte[] BuildQueryStatusPacket(byte[]? queryCmdIds = null)
    {
        queryCmdIds ??= [12, 9, 2, 14, 13, 39, 7, 32, 33];
        return BuildPacket(SibylCommandId.QueryInfo, queryCmdIds);
    }
}
