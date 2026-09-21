namespace SibylEarbuds.Core.Protocol;

/// <summary>
/// 从官方 Android 客户端 (BLEScanManager) 逆向提取的 SIBYL 耳机广播识别结果。
/// 厂商数据 payload 结构 (紧跟 2 字节 Company ID 之后):
///   [0..1]  VendorId  机型 ID (Big-Endian)
///   [2..4]  左/右/仓 电量 (bit7 为充电标志)
///   [5]     Status
///   [6..11] BLE MAC
///   [12..17] 可选第二 MAC (长度 &gt;= 18 时存在)
/// </summary>
public readonly record struct SibylAdvertisement(
    int VendorId,
    string ModelName,
    int LeftBattery,
    int RightBattery,
    int CaseBattery);

/// <summary>
/// SIBYL 设备多层智能识别与过滤（确保列表中仅出现 SIBYL 耳机并自动识别机型）：
/// 1. 官方 0xC912 厂商广播与 VendorId 识别（提取官方型号与电量）；
/// 2. 杰理/炬力/瑞昱常用 Company ID (0x3E21 / 0x12CC / 0x3D11 / 0x05D6) 特征识别；
/// 3. SIBYL 专用 GATT Service UUID (0x00FE / 0xAE00 / 0xAE30) 识别；
/// 4. 设备本地广播名称前缀判定（必须包含 SIBYL 或特定官方型号前缀，彻底过滤无关非 SIBYL 设备）。
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
    /// </summary>
    public static readonly IReadOnlyDictionary<ushort, string> KnownModels =
        new Dictionary<ushort, string>
        {
            [15377] = "S1",   // 0x3C11
            [15889] = "S7",   // 0x3E11
            [15895] = "S10",  // 0x3E17
            [15921] = "B1",   // 0x3E31
            [16017] = "B1",   // 0x3E91
            [15985] = "Y1",   // 0x3E71
            [16193] = "Y1"    // 0x3F41
        };

    /// <summary>
    /// 官方定义的已知型号前缀，用于严格过滤非 SIBYL 蓝牙设备。
    /// </summary>
    public static readonly string[] KnownNamePrefixes =
    [
        "SIBYL", "WEDOING", "B1", "S1", "S7", "S10", "S11",
        "B6", "B8", "B14", "Y1", "Y7", "Y8", "Y9", "CH500",
        "WF200", "WS200"
    ];

    /// <summary>
    /// 仅基于厂商广播数据校验是否为官方 SIBYL 耳机。
    /// </summary>
    public static bool IsSibylEarbuds(IReadOnlyDictionary<ushort, byte[]>? manufacturerData)
        => TryParseSibylAdvertisement(manufacturerData, out _);

    /// <summary>
    /// 官方 APK 广播识别算法：
    /// 1. 必须存在 CompanyID = <see cref="SibylManufacturerId"/> 的厂商数据；
    /// 2. payload 长度必须 &gt;= <see cref="MinAdvertisementLength"/>；
    /// 3. 前 2 字节 (Big-Endian) 的 VendorId 必须在官方产品清单中。
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
    /// 综合判定是否为 SIBYL 耳机（多层过滤，严防非 SIBYL 设备混入，同时确保各型号 SIBYL 耳机均能搜到）：
    /// 1. 广播厂商数据命中官方 0xC912；
    /// 2. 广播厂商数据命中 0x3E21 / 0x12CC / 0x3D11 / 0x05D6 等双模芯片特征；
    /// 3. 广播服务 UUID 包含官方 SIBYL 控制通道；
    /// 4. 设备名称包含 SIBYL 或官方知名型号。
    /// </summary>
    public static bool TryMatchSibylDevice(
        string? localName,
        IReadOnlyDictionary<ushort, byte[]>? manufacturerData,
        IEnumerable<Guid>? serviceUuids,
        out SibylAdvertisement adv,
        out string identifiedModel)
    {
        adv = default;
        identifiedModel = "PRO";

        // 1. 优先尝试从官方厂商广播精准解析
        if (TryParseSibylAdvertisement(manufacturerData, out adv))
        {
            identifiedModel = adv.ModelName;
            return true;
        }

        // 2. 检查广播厂商自定义字段
        if (manufacturerData != null)
        {
            foreach (var kvp in manufacturerData)
            {
                if (kvp.Key == SibylManufacturerId || kvp.Key == 0x3E21 || kvp.Key == 0x3E22 ||
                    kvp.Key == 0x3D11 || kvp.Key == 0x12CC || kvp.Key == 0x05D6)
                {
                    identifiedModel = ExtractModelFromName(localName);
                    adv = new SibylAdvertisement(0, identifiedModel, -1, -1, -1);
                    return true;
                }

                var bytes = kvp.Value;
                if (bytes != null && bytes.Length >= 2)
                {
                    for (int i = 0; i < bytes.Length - 1; i++)
                    {
                        ushort word = (ushort)(bytes[i] | (bytes[i + 1] << 8));
                        if (word == 0x12CC || word == 0x3E21 || word == 0x3E22 || word == 0x3D11)
                        {
                            identifiedModel = ExtractModelFromName(localName);
                            adv = new SibylAdvertisement(0, identifiedModel, -1, -1, -1);
                            return true;
                        }
                    }
                }
            }
        }

        // 3. 检查专用服务 UUID 广播
        if (serviceUuids != null)
        {
            foreach (var uuid in serviceUuids)
            {
                if (SibylUuids.CandidateServiceUuids.Contains(uuid))
                {
                    identifiedModel = ExtractModelFromName(localName);
                    adv = new SibylAdvertisement(0, identifiedModel, -1, -1, -1);
                    return true;
                }
            }
        }

        // 4. 名称严格过滤（只允许 SIBYL 品牌设备）
        if (!string.IsNullOrWhiteSpace(localName))
        {
            string upper = localName.Trim().ToUpperInvariant();

            // 包含 "SIBYL" 必定是 SIBYL 耳机
            if (upper.Contains("SIBYL"))
            {
                identifiedModel = ExtractModelFromName(localName);
                adv = new SibylAdvertisement(0, identifiedModel, -1, -1, -1);
                return true;
            }

            // 检查已知官方型号前缀
            foreach (var prefix in KnownNamePrefixes)
            {
                if (upper.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase) ||
                    upper.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase) ||
                    upper.StartsWith(prefix + "_", StringComparison.OrdinalIgnoreCase) ||
                    upper.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    identifiedModel = prefix;
                    adv = new SibylAdvertisement(0, identifiedModel, -1, -1, -1);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 从设备名称中智能解析官方型号（如 "SIBYL S1 ANC" -> "S1", "SIBYL B6 Cyber" -> "B6"）。
    /// </summary>
    public static string ExtractModelFromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "PRO";
        }

        string upper = name.Trim().ToUpperInvariant();
        string[] candidates = ["CH500", "WF200", "WS200", "S11", "S10", "B14", "Y7", "Y8", "Y9", "S7", "S1", "B1", "B6", "B8", "Y1"];
        foreach (var m in candidates)
        {
            if (upper.Contains(m))
            {
                return m;
            }
        }

        return "PRO";
    }

    /// <summary>
    /// 反查 VendorId 对应的官方机型名，未知时返回通用 "PRO"。
    /// </summary>
    public static string ResolveModelName(int vendorId)
        => KnownModels.TryGetValue((ushort)vendorId, out string? name) ? name : "PRO";
}
