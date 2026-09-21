namespace SibylEarbuds.Core.Models;

/// <summary>
/// SIBYL 全系列耳机型号能力映射库 (从 APK raw 资源文件逆向提炼，默认开启两级降噪调节)
/// </summary>
public static class DeviceModelProfiles
{
    private static readonly Dictionary<string, DeviceCapability> _profiles = new(StringComparer.OrdinalIgnoreCase);

    static DeviceModelProfiles()
    {
        // 1. S1 (旗舰真无线)
        Register(new DeviceCapability
        {
            ModelId = "S1",
            DisplayName = "SIBYL S1 (旗舰真无线)",
            ChipDescription = "杰理 (AC6973D8 高配)",
            ChipPlatform = BluetoothChipPlatform.JieLi,
            VendorHex = "3E21",
            FlageHex = "12CC",
            SupportsAnc = true,
            SupportsAncDepth = true, // 支持两级降噪调节 (舒适/深度)
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsKeyCustomization = true,
            SupportsTouchLock = true,
            SupportsTimedShutdown = true,
            SupportsSleepMode = true,
            SupportsFindEarphone = true,
            SupportsToneVolume = true,
            SupportsWhiteNoise = true
        });

        // 2. B6 (电竞炫彩灯效机型)
        Register(new DeviceCapability
        {
            ModelId = "B6",
            DisplayName = "SIBYL B6 (RGB电竞耳机)",
            ChipDescription = "杰理 (AC697N 炫彩系列)",
            ChipPlatform = BluetoothChipPlatform.JieLi,
            VendorHex = "3E21",
            FlageHex = "12CC",
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsLightEffect = true,
            SupportsKeyCustomization = true,
            SupportsTouchLock = true,
            SupportsFindEarphone = true,
            SupportsToneVolume = true,
            SupportsWhiteNoise = true
        });

        // 3. B1 (标准主力机型)
        Register(new DeviceCapability
        {
            ModelId = "B1",
            DisplayName = "SIBYL B1",
            ChipDescription = "杰理 (AC6973D8)",
            ChipPlatform = BluetoothChipPlatform.JieLi,
            VendorHex = "3E21",
            FlageHex = "12CC",
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsKeyCustomization = true,
            SupportsQuadrupleTap = false, // 官方 APK B1 仅支持 单/双/三击/长按
            SupportsFindEarphone = true,
            SupportsWhiteNoise = true
        });

        // 4. S10 (半入耳音乐系列)
        Register(new DeviceCapability
        {
            ModelId = "S10",
            DisplayName = "SIBYL S10",
            ChipDescription = "杰理 (AC6973D8 低功耗)",
            ChipPlatform = BluetoothChipPlatform.JieLi,
            VendorHex = "3E22",
            FlageHex = "12CC",
            SupportsAnc = false, // 半入耳物理无主动降噪
            SupportsAncDepth = false,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsKeyCustomization = true,
            SupportsFindEarphone = true,
            SupportsToneVolume = true,
            SupportsWhiteNoise = true
        });

        // 5. S11 & S11_LDAC (Hi-Res高清系列)
        Register(new DeviceCapability
        {
            ModelId = "S11",
            DisplayName = "SIBYL S11 / S11 Pro",
            ChipDescription = "瑞昱/杰理 双模平台",
            ChipPlatform = BluetoothChipPlatform.Realtek,
            VendorHex = "3E22",
            FlageHex = "12CC",
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsKeyCustomization = true,
            SupportsFindEarphone = true,
            SupportsToneVolume = true,
            SupportsLdacHiRes = true,
            SupportsWhiteNoise = true
        });

        // 6. B8 (体感控制系列)
        Register(new DeviceCapability
        {
            ModelId = "B8",
            DisplayName = "SIBYL B8 (体感感应版)",
            ChipDescription = "杰理 (内置加速度传感器)",
            ChipPlatform = BluetoothChipPlatform.JieLi,
            VendorHex = "3E21",
            FlageHex = "12CC",
            SupportsAnc = false,
            SupportsAncDepth = false,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsSomatosensory = true,
            SupportsKeyCustomization = true,
            SupportsFindEarphone = true,
            SupportsWhiteNoise = true
        });

        // 7. B14 (长续航音乐版)
        Register(new DeviceCapability
        {
            ModelId = "B14",
            DisplayName = "SIBYL B14",
            ChipDescription = "杰理/炬力平台",
            ChipPlatform = BluetoothChipPlatform.JieLi,
            SupportsAnc = false,
            SupportsAncDepth = false,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsTouchLock = true,
            SupportsLdacHiRes = true,
            SupportsToneVolume = true,
            SupportsFindEarphone = true,
            SupportsWhiteNoise = true
        });

        // 8. Y1 (炬力平台)
        Register(new DeviceCapability
        {
            ModelId = "Y1",
            DisplayName = "SIBYL Y1",
            ChipDescription = "炬力 (ATS3015 低延迟芯片)",
            ChipPlatform = BluetoothChipPlatform.Actions,
            VendorHex = "3D11",
            FlageHex = "12CC",
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsKeyCustomization = true,
            SupportsFindEarphone = true,
            SupportsWhiteNoise = true
        });

        // 9. Y7 / Y7_MAX
        Register(new DeviceCapability
        {
            ModelId = "Y7",
            DisplayName = "SIBYL Y7 / Y7 Max",
            ChipDescription = "炬力/杰理双模",
            ChipPlatform = BluetoothChipPlatform.Actions,
            VendorHex = "3D11",
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsKeyCustomization = true,
            SupportsFindEarphone = true,
            SupportsToneVolume = true,
            SupportsWhiteNoise = true
        });

        // 10. CH500 (头戴主动降噪)
        Register(new DeviceCapability
        {
            ModelId = "CH500",
            DisplayName = "SIBYL CH500 (头戴主动降噪)",
            ChipDescription = "瑞昱 (BBPro DSP 增强)",
            ChipPlatform = BluetoothChipPlatform.Realtek,
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsFindEarphone = true,
            SupportsWhiteNoise = true
        });

        // 11. WF200 / WS200 LDAC
        Register(new DeviceCapability
        {
            ModelId = "WS200",
            DisplayName = "SIBYL WS200 PRO (LDAC)",
            ChipDescription = "Realtek 高清无损音频平台",
            ChipPlatform = BluetoothChipPlatform.Realtek,
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsLdacHiRes = true,
            SupportsKeyCustomization = true,
            SupportsQuadrupleTap = true,
            SupportsFindEarphone = true,
            SupportsToneVolume = true,
            SupportsWhiteNoise = true
        });

        Register(new DeviceCapability
        {
            ModelId = "WF200",
            DisplayName = "SIBYL WF200 / WS200 LDAC",
            ChipDescription = "Realtek 高清无损音频平台",
            ChipPlatform = BluetoothChipPlatform.Realtek,
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsLdacHiRes = true,
            SupportsKeyCustomization = true,
            SupportsQuadrupleTap = true,
            SupportsFindEarphone = true,
            SupportsToneVolume = true,
            SupportsWhiteNoise = true
        });

        // 12. PRO (通用全功能兼容方案)
        Register(new DeviceCapability
        {
            ModelId = "PRO",
            DisplayName = "SIBYL 通用全能模式 (Auto)",
            ChipDescription = "自适应识别 (JieLi / Realtek / Actions)",
            ChipPlatform = BluetoothChipPlatform.Generic,
            SupportsAnc = true,
            SupportsAncDepth = true,
            SupportsEq = true,
            SupportsGameMode = true,
            SupportsLightEffect = true,
            SupportsKeyCustomization = true,
            SupportsTouchLock = true,
            SupportsTimedShutdown = true,
            SupportsSleepMode = true,
            SupportsFindEarphone = true,
            SupportsLdacHiRes = true,
            SupportsToneVolume = true,
            SupportsWhiteNoise = true
        });
    }

    private static void Register(DeviceCapability capability)
    {
        _profiles[capability.ModelId] = capability;
    }

    public static IReadOnlyList<DeviceCapability> GetAllProfiles()
    {
        return _profiles.Values.ToList();
    }

    /// <summary>
    /// 根据扫描到的设备名称智能匹配最佳能力配置表
    /// </summary>
    public static DeviceCapability MatchProfileByName(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
            return _profiles["PRO"];

        string upper = deviceName.ToUpperInvariant();
        foreach (var kvp in _profiles.OrderByDescending(p => p.Key.Length))
        {
            if (kvp.Key == "PRO") continue;
            if (upper.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }

        return _profiles["PRO"];
    }
}
