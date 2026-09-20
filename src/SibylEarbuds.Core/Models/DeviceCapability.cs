namespace SibylEarbuds.Core.Models;

/// <summary>
/// 蓝牙主控芯片平台
/// </summary>
public enum BluetoothChipPlatform
{
    JieLi,      // 杰理 (AC6973D8 / AC697N 系列, VendorHex: 3E21 / 3E22)
    Actions,    // 炬力 (ATS3015 系列, VendorHex: 3D11)
    Realtek,    // 瑞昱 (BBPro / RTL8773 系列, libDspConfig)
    Generic     // 通用 BLE 双模协议
}

/// <summary>
/// 设备功能支持能力集合
/// </summary>
public class DeviceCapability
{
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ChipDescription { get; set; } = string.Empty;
    public BluetoothChipPlatform ChipPlatform { get; set; } = BluetoothChipPlatform.Generic;
    public string VendorHex { get; set; } = "3E21";
    public string FlageHex { get; set; } = "12CC";

    // 功能支持开关 (从 APK raw/*.json 逆向解析得出)
    public bool SupportsAnc { get; set; }               // ANC 降噪模式 (降噪/通透/正常)
    public bool SupportsAncDepth { get; set; }          // ANC 深度多档调节 (50%/100%)
    public bool SupportsEq { get; set; } = true;        // 10段音乐均衡器
    public bool SupportsGameMode { get; set; } = true;  // 38ms 低延迟游戏模式
    public bool SupportsLightEffect { get; set; }       // RGB 炫彩灯效模式 (如 B6)
    public bool SupportsKeyCustomization { get; set; } = true; // 按键手势映射
    public bool SupportsTouchLock { get; set; }         // 触控锁定 (防误触)
    public bool SupportsTimedShutdown { get; set; }     // 定时自动关机
    public bool SupportsSleepMode { get; set; }         // 睡眠模式
    public bool SupportsFindEarphone { get; set; } = true; // 寻找耳机发声
    public bool SupportsLdacHiRes { get; set; }         // Hi-Res Wireless / LDAC 高清解码
    public bool SupportsSomatosensory { get; set; }     // 体感控制 (如 B8)
    public bool SupportsToneVolume { get; set; }        // 提示音音量调节
    public bool SupportsWhiteNoise { get; set; } = true;// 疗愈白噪音
    public bool SupportsRename { get; set; } = true;    // 修改蓝牙名称
}
