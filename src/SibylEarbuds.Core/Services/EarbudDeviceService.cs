using SibylEarbuds.Core.Dispatcher;
using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;
using SibylEarbuds.Core.Transport;

namespace SibylEarbuds.Core.Services;

public class EarbudDeviceService : IDisposable
{
    public IBleTransport Transport { get; }
    public AntiConflictDispatcher Dispatcher { get; }
    public DeviceStatus CurrentStatus { get; } = new();

    public event Action<DeviceStatus>? StatusUpdated;
    public event Action<string>? LogMessage;

    public EarbudDeviceService(IBleTransport transport)
    {
        Transport = transport;
        Dispatcher = new AntiConflictDispatcher(transport);

        Transport.OnDataReceived += HandleIncomingBleData;
        Transport.OnConnectionStateChanged += HandleConnectionStateChanged;
        Transport.OnLog += msg => LogMessage?.Invoke(msg);
        Dispatcher.LogMessage += msg => LogMessage?.Invoke(msg);
    }

    public async Task<IReadOnlyList<DiscoveredBleDevice>> ScanDevicesAsync(int timeoutMs = 4000)
    {
        return await Transport.ScanDevicesAsync(timeoutMs);
    }

    public async Task<bool> ConnectAsync(string deviceId)
    {
        bool success = await Transport.ConnectAsync(deviceId);
        if (success)
        {
            CurrentStatus.IsConnected = true;
            CurrentStatus.MacAddress = deviceId;
            CurrentStatus.DeviceName = Transport.ConnectedDeviceName ?? "SIBYL Earbuds";
            StatusUpdated?.Invoke(CurrentStatus);

            // 握手成功后，向耳机发起属性全面查询 (电量/ANC/EQ/游戏模式/版本/名称)
            await Task.Delay(300);
            var queryPacket = PacketBuilder.BuildQueryStatusPacket();
            await Dispatcher.SendCriticalCommandAsync(SibylCommandId.QueryInfo, queryPacket);
        }
        return success;
    }

    public async Task DisconnectAsync()
    {
        await Transport.DisconnectAsync();
        CurrentStatus.IsConnected = false;
        StatusUpdated?.Invoke(CurrentStatus);
    }

    /// <summary>
    /// 切换降噪模式（ANC/普通/通透）- 走独立 BLE，不打断音频
    /// </summary>
    public async Task<bool> SetAncModeAsync(AncModeType mode, AncDepthLevel depth = AncDepthLevel.Deep)
    {
        CurrentStatus.Anc = new AncState(mode, depth);
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildAncPacket(mode, depth);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.AncMode, packet);
    }

    /// <summary>
    /// 手动上传并应用 10 段均衡器增益（需用户在界面点击确认按钮下发）
    /// </summary>
    public async Task<bool> ApplyEqGainsAsync(int[] gains, int presetType = 255)
    {
        CurrentStatus.CurrentEq.Gains = (int[])gains.Clone();
        CurrentStatus.CurrentEq.PresetId = presetType;
        CurrentStatus.CurrentEq.Name = presetType == 255 ? "自定义" : "预设音效";
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildEqPacket(gains, presetType);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.Equalizer, packet);
    }

    /// <summary>
    /// 实时调整 10 段均衡器增益
    /// </summary>
    public void SetEqGains(int[] gains)
    {
        CurrentStatus.CurrentEq.Gains = (int[])gains.Clone();
        CurrentStatus.CurrentEq.PresetId = 255;
        CurrentStatus.CurrentEq.Name = "自定义";
        StatusUpdated?.Invoke(CurrentStatus);
    }

    /// <summary>
    /// 应用 EQ 预设模式（经典、摇滚、抒情等）
    /// </summary>
    public async Task<bool> ApplyEqPresetAsync(EqConfiguration preset)
    {
        CurrentStatus.CurrentEq = new EqConfiguration(preset.Name, preset.PresetId, preset.Gains);
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildEqPacket(preset.Gains, preset.PresetId);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.Equalizer, packet);
    }

    /// <summary>
    /// 开启/关闭低延迟游戏模式
    /// </summary>
    public async Task<bool> SetGameModeAsync(bool enabled)
    {
        CurrentStatus.IsGameModeEnabled = enabled;
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildGameModePacket(enabled);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.GameMode, packet);
    }

    /// <summary>
    /// 开启/关闭 Hi-Res Wireless / LDAC 高清音频解码模式 (cmdid 77)
    /// </summary>
    public async Task<bool> SetHighResModeAsync(bool enabled)
    {
        CurrentStatus.IsLdacEnabled = enabled;
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildPacket(SibylCommandId.LdacHighRes, [(byte)(enabled ? 1 : 0)]);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.LdacHighRes, packet);
    }

    /// <summary>
    /// 调整灯光效果（呼吸/常亮/关闭/颜色）
    /// </summary>
    public void SetLightEffect(LightEffectConfig config)
    {
        CurrentStatus.LightEffect = config;
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildLightModePacket(config);
        Dispatcher.EnqueueDebouncedCommand(SibylCommandId.LightMode, packet);
    }

    /// <summary>
    /// 保存按键自定义映射
    /// </summary>
    public async Task<bool> SaveKeySettingsAsync(EarbudKeySettings settings)
    {
        CurrentStatus.KeySettings = settings;
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildKeySettingsPacket(settings);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.KeyFunction, packet);
    }

    /// <summary>
    /// 开启/关闭触控锁定（防误触）
    /// </summary>
    public async Task<bool> SetTouchLockAsync(bool disabled)
    {
        CurrentStatus.IsTouchDisabled = disabled;
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildTouchLockPacket(disabled);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.CloseTouch, packet);
    }

    /// <summary>
    /// 查找耳机（播放/停止警报提示音）
    /// </summary>
    public async Task<bool> FindEarphonesAsync(bool play)
    {
        var packet = PacketBuilder.BuildFindEarphonePacket(play);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.ToneVolumeControl, packet);
    }

    /// <summary>
    /// 设置定时关机 (分钟)
    /// </summary>
    public async Task<bool> SetTimedShutdownAsync(int minutes)
    {
        CurrentStatus.TimedShutdownMinutes = minutes;
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildTimedShutdownPacket(minutes);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.TimedShutdown, packet);
    }

    /// <summary>
    /// 睡眠模式
    /// </summary>
    public async Task<bool> SetSleepModeAsync(bool enabled)
    {
        CurrentStatus.IsSleepModeEnabled = enabled;
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildSleepModePacket(enabled);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.SleepMode, packet);
    }

    /// <summary>
    /// 重命名耳机
    /// </summary>
    public async Task<bool> RenameDeviceAsync(string newName)
    {
        CurrentStatus.DeviceName = newName;
        StatusUpdated?.Invoke(CurrentStatus);

        var packet = PacketBuilder.BuildRenamePacket(newName);
        return await Dispatcher.SendCriticalCommandAsync(SibylCommandId.PairName, packet);
    }

    /// <summary>
    /// 恢复出厂设置或默认设置
    /// </summary>
    public async Task<bool> ResetSettingsAsync(bool factoryReset)
    {
        var packet = PacketBuilder.BuildResetPacket(factoryReset);
        return await Dispatcher.SendCriticalCommandAsync(
            factoryReset ? SibylCommandId.RestoreFactorySettings : SibylCommandId.RestDefaultSettings,
            packet
        );
    }

    private void HandleIncomingBleData(byte[] raw)
    {
        var result = PacketParser.Parse(raw);
        if (result.IsSuccess)
        {
            bool modified = PacketParser.ApplyToStatus(result, CurrentStatus);
            if (modified)
            {
                StatusUpdated?.Invoke(CurrentStatus);
            }
        }
    }

    private void HandleConnectionStateChanged(bool isConnected)
    {
        CurrentStatus.IsConnected = isConnected;
        StatusUpdated?.Invoke(CurrentStatus);
    }

    public void Dispose()
    {
        Transport.OnDataReceived -= HandleIncomingBleData;
        Transport.OnConnectionStateChanged -= HandleConnectionStateChanged;
        Dispatcher.Dispose();
        Transport.Dispose();
    }
}
