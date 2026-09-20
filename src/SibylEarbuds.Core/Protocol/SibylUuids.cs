namespace SibylEarbuds.Core.Protocol;

public static class SibylUuids
{
    // 官方 SIBYL / 杰理 / 炬力 / 中科蓝讯 通用主服务与特征 UUID (从官方 ProductClient 提取)
    public static readonly Guid SibylServiceUuid = new("000000FE-0000-1000-8000-00805F9B34FB");
    public static readonly Guid SibylWriteUuid = new("000000F1-0000-1000-8000-00805F9B34FB");
    public static readonly Guid SibylNotifyUuid = new("000000F2-0000-1000-8000-00805F9B34FB");

    // 杰理 (JieLi) RCSP BLE 服务及特征
    public static readonly Guid JieLiServiceUuid = new("0000AE00-0000-1000-8000-00805F9B34FB");
    public static readonly Guid JieLiWriteUuid = new("0000AE01-0000-1000-8000-00805F9B34FB");
    public static readonly Guid JieLiNotifyUuid = new("0000AE02-0000-1000-8000-00805F9B34FB");

    public static readonly Guid JieLiService2Uuid = new("0000AE30-0000-1000-8000-00805F9B34FB");
    public static readonly Guid JieLiWrite2Uuid = new("0000AE31-0000-1000-8000-00805F9B34FB");
    public static readonly Guid JieLiNotify2Uuid = new("0000AE32-0000-1000-8000-00805F9B34FB");

    // 经典标准电池服务
    public static readonly Guid BatteryServiceUuid = new("0000180F-0000-1000-8000-00805F9B34FB");
    public static readonly Guid BatteryLevelCharUuid = new("00002A19-0000-1000-8000-00805F9B34FB");

    // 候选服务列表（按匹配优先级尝试）
    public static readonly Guid[] CandidateServiceUuids =
    [
        SibylServiceUuid,
        JieLiServiceUuid,
        JieLiService2Uuid,
        new Guid("0000FEE7-0000-1000-8000-00805F9B34FB"),
        new Guid("0000FFF0-0000-1000-8000-00805F9B34FB")
    ];
}
