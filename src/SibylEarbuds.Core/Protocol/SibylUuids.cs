namespace SibylEarbuds.Core.Protocol;

public static class SibylUuids
{
    // SIBYL 配置文件中显式绑定的主特征/服务 UUID (从 res/*.json 提取)
    public static readonly Guid SibylPrimaryServiceUuid = new("0000000B-0000-1000-8000-00805f9b34fb");
    public static readonly Guid SibylPrimaryCharacteristicUuid = new("0000000B-0000-1000-8000-00805f9b34fb");

    // 杰理 (JieLi) 平台常用 RCSP BLE 服务及特征 UUID
    public static readonly Guid JieLiServiceUuid = new("0000AE00-0000-1000-8000-00805f9b34fb");
    public static readonly Guid JieLiWriteUuid = new("0000AE01-0000-1000-8000-00805f9b34fb");
    public static readonly Guid JieLiNotifyUuid = new("0000AE02-0000-1000-8000-00805f9b34fb");

    // 经典标准电池服务 (BLE Battery Service)
    public static readonly Guid BatteryServiceUuid = new("0000180F-0000-1000-8000-00805f9b34fb");
    public static readonly Guid BatteryLevelCharUuid = new("00002A19-0000-1000-8000-00805f9b34fb");

    // 设备信息服务
    public static readonly Guid DeviceInfoServiceUuid = new("0000180A-0000-1000-8000-00805f9b34fb");
    public static readonly Guid FirmwareRevisionCharUuid = new("00002A26-0000-1000-8000-00805f9b34fb");

    /// <summary>
    /// 支持的候选服务 UUID 列表（按优先级尝试）
    /// </summary>
    public static readonly Guid[] CandidateServiceUuids =
    [
        SibylPrimaryServiceUuid,
        JieLiServiceUuid,
        new Guid("0000FEE7-0000-1000-8000-00805F9B34FB"),
        new Guid("0000FFF0-0000-1000-8000-00805F9B34FB")
    ];
}
