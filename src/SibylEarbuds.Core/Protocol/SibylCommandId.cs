namespace SibylEarbuds.Core.Protocol;

/// <summary>
/// SIBYL 耳机协议核心指令 ID (从 APK 反编译资源和逻辑逆向得出)
/// </summary>
public enum SibylCommandId : byte
{
    QueryStatus = 0x00,                 // 查询状态与电量
    KeyFunction = 0x01,                // 自定义按键/触控功能
    Equalizer = 0x02,                  // 10段音乐均衡器设置
    FindEarphone = 0x03,               // 寻找耳机 (播放声音/停止)
    RestDefaultSettings = 0x04,        // 恢复默认设置
    ClearPairingRecords = 0x05,        // 清除配对记录
    RestoreFactorySettings = 0x06,     // 恢复出厂设置
    CloseTouch = 0x07,                 // 关闭触控功能
    AncMode = 0x09,                    // ANC降噪模式切换
    LightMode = 0x0A,                  // 灯效模式设置
    FirmwareVersion = 0x0D,            // 固件版本读取
    GameMode = 0x0E,                   // 低延迟游戏模式开关
    VolumeControl = 0x0F,              // 基础音量控制
    TimedShutdown = 0x20,              // 定时关机 (分钟数)
    SleepMode = 0x21,                  // 睡眠模式开关
    PairName = 0x27,                   // 修改设备蓝牙名称
    ToneVolumeControl = 0x29,          // 提示音音量调节
    SomatosensoryControl = 0x4C,       // 体感控制
    LdacHiRes = 0x4D                   // 高清解码开关
}
