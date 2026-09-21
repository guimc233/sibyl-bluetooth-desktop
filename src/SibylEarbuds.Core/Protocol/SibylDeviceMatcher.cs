namespace SibylEarbuds.Core.Protocol;

/// <summary>
/// 从官方 Android 客户端 (BLEScanManager) 逆向提取的 SIBYL 耳机广播识别结果。
/// 厂商数据 payload 结构 (紧跟 2 字节 Company ID 之后):
///   [0..1]  VendorId  机型 ID (Big-Endian)
///   [2..4]  左/右/仓 电量 (bit7 为充电标志)
///   [5]     Status
///   [6..11] BLE MAC
///   [12..17] 可选第二 MAC (长度 &gt;= 18 时存在)
/// 官方要求 payload 长度 &gt;= 12 且 VendorId 必须在产品清单中，否则设备直接忽略。
/// </summary>
public readonly record struct SibylAdvertisement(
    int VendorId,
    string ModelName,
    int LeftBattery,
    int RightBattery,
    int CaseBattery);

/// <summary>
/// SIBYL 官方广播识别（厂商数据过滤器），与 Android 端 BLEScanManager 完全一致：
/// 扫描器只关心 CompanyID = 0xC912 的厂商广播，根据 VendorId 在官方产品清单中
/// 查到机型才纳入设备列表 —— 因此设备列表只会出现官方 SIBYL 耳机。
/// </summary>
public static class SibylDeviceMatcher
{
    /// <summary>
    /// 官方 FILTERID / flageId = 51474 (0xC912)，即广播中的厂商 Company ID。
    /// </summary>
    public const ushort SibylManufacturerId = 51474;

    /// <summary>
    /// 官方要求的最小广播 payload 长度。
    /// </summary>
    public const int MinAdvertisementLength = 12;

    /// <summary>
    /// 官方产品清单 (res/raw/earphone_list.json)：VendorId → 机型名。
    /// Reader: 15377(0x3C11)=S1, 15889(0x3E11)=S7, 15895(0x3E17)=S10,
    ///         15921(0x3E31)=B1, 16017(0x3E91)=B1, 15985(0x3E71)=Y1, 16193(0x3F41)=Y1
    /// </summary>
    public static readonly IReadOnlyDictionary<ushort, string> KnownModels =
        new Dictionary<ushort, string>
        {
            [15377] = "S1",
            [15889] = "S7",
            [15895] = "S10",
            [15921] = "B1",
            [16017] = "B1",
            [15985] = "Y1",
            [16193] = "Y1"
        };

    /// <summary>
    /// 综合判定广播包是否来自官方 SIBYL 耳机（严格模式，仅当可通过
    /// <see cref="TryParseSibylAdvertisement"/> 解析出已知机型时为 true）。
    /// </summary>
    public static bool IsSibylEarbuds(IReadOnlyDictionary<ushort, byte[]>? manufacturerData)
        => TryParseSibylAdvertisement(manufacturerData, out _);

    /// <summary>
    /// 官方 APK 广播识别算法：
    /// 1. 必须存在 CompanyID = <see cref="SibylManufacturerId"/> 的厂商数据；
    /// 2. payload 长度必须 &gt;= <see cref="MinAdvertisementLength"/>；
    /// 3. 前 2 字节 (Big-Endian) 的 VendorId 必须在官方产品清单中。
    /// 条件不满足时设备将被直接忽略，不会出现在列表中。
    /// </summary>
    public static bool TryParseSibylAdvertisement(
        IReadOnlyDictionary<ushort, byte[]>? manufacturerData,
        out SibylAdvertisement advertisement)
    {
        advertisement = default;

        if (manufacturerData is null ||
            !manufacturerData.TryGetValue(SibylManufacturerId, out byte[]? payload) ||
            payload is null ||
            payload.Length < MinAdvertisementLength)
        {
            return false;
        }

        int vendorId = ((payload[0] & 0xFF) << 8) | (payload[1] & 0xFF);
        if (!KnownModels.TryGetValue((ushort)vendorId, out string? modelName))
        {
            return false;
        }

        advertisement = new SibylAdvertisement(
            vendorId,
            modelName,
            payload.Length > 2 ? payload[2] & 0x7F : -1,
            payload.Length > 3 ? payload[3] & 0x7F : -1,
            payload.Length > 4 ? payload[4] & 0x7F : -1);

        return true;
    }

    /// <summary>
    /// 反查 VendorId 对应的官方机型名，未知时返回通用 "PRO"。
    /// </summary>
    public static string ResolveModelName(int vendorId)
        => KnownModels.TryGetValue((ushort)vendorId, out string? name) ? name : "PRO";
}