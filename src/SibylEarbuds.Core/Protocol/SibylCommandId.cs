namespace SibylEarbuds.Core.Protocol;

/// <summary>
/// SIBYL 耳机协议核心指令 ID (严格匹配官方 ProductClient.kt)
/// </summary>
public enum SibylCommandId : byte
{
    KeyFunction = 1,             // 自定义按键功能 (CMDID_keyfunc)
    Equalizer = 2,               // 10段音乐均衡器设置 (CMDID_EQ)
    RestDefaultSettings = 4,     // 恢复默认设置 (CMDID_reset)
    ClearPairingRecords = 5,     // 清除配对记录 (CMDID_clear)
    RestoreFactorySettings = 6,  // 恢复出厂设置 (CMDID_factory)
    CloseTouch = 7,              // 关闭触控防误触 (CMDID_touch)
    InEarDetection = 8,          // 入耳检测 (CMDID_inear)
    AncMode = 9,                 // ANC降噪模式切换 (CMDID_anc)
    LightMode = 10,              // 灯效模式设置 (CMDID_led)
    Battery = 12,                // 电池电量状态 (CMDID_battery)
    FirmwareVersion = 13,        // 固件版本读取 (CMDID_version)
    GameMode = 14,               // 低延迟游戏模式开关 (CMDID_game)
    VolumeControl = 15,          // 基础音量控制 (CMDID_volume)
    LedSwitch = 16,              // 灯效开关 (CMDID_ledSwitch)
    TimedShutdown = 32,          // 定时关机分钟数 (CMDID_PowerOff)
    SleepMode = 33,              // 睡眠模式开关 (CMDID_SleepMode)
    PairName = 39,               // 修改蓝牙设备名称 (CMDID_ChangePairName)
    ToneVolumeControl = 41,      // 提示音音量调节 (CMDID_PROMPT)
    HifiMode = 42,               // HiFi 模式 (CMDID_HifiMode)
    Somatosensory1 = 77,         // 点头体感控制 (CMDID_SomatonControl1)
    Somatosensory2 = 79,         // 摇头体感控制 (CMDID_SomatonControl2)
    Somatosensory3 = 81,         // 转头体感控制 (CMDID_SomatonControl3)
    QueryInfo = 0xFA             // 查询设备属性信息 (CMDID_GETINFO = -6 = 0xFA)
}
