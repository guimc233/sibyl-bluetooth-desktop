namespace SibylEarbuds.Core.Protocol;

/// <summary>
/// SIBYL 官方广播包校验与耳机设备判定逻辑 (从 APK 逆向提取)
/// </summary>
public static class SibylDeviceMatcher
{
    // 官方定义的常用厂商标识与型号前缀
    public static readonly string[] KnownNamePrefixes =
    [
        "SIBYL", "WEDOING", "B1", "S1", "S7", "S10", "S11",
        "B6", "B8", "B14", "Y1", "Y7", "Y8", "Y9", "CH500",
        "WF200", "WS200"
    ];

    // 官方 APK 广播包含的特定 Service UUID 列表 (16位/128位)
    public static readonly ushort[] KnownServiceUuids16 =
    [
        0xAE00, 0xAE01, 0xAE02, // 杰理
        0x000B,                 // SIBYL Primary
        0xFEE7, 0xFFF0          // 炬力/瑞昱常用
    ];

    /// <summary>
    /// 综合判定是否为 SIBYL 耳机设备
    /// 支持三种判断层级：
    /// 1. 广播服务 UUID 匹配
    /// 2. Manufacturer Specific Data 杰理/炬力 0x3E21 / 0x12CC 特征
    /// 3. 设备本地名称前缀或白名单
    /// </summary>
    public static bool IsSibylEarbuds(
        string? localName,
        IEnumerable<Guid>? serviceUuids = null,
        IReadOnlyDictionary<ushort, byte[]>? manufacturerData = null)
    {
        // 1. 优先检查 Service UUIDs
        if (serviceUuids != null)
        {
            foreach (var uuid in serviceUuids)
            {
                if (SibylUuids.CandidateServiceUuids.Contains(uuid))
                {
                    return true;
                }
            }
        }

        // 2. 检查 Manufacturer Data (如 0x3E21 / 0x12CC / 杰理厂家ID)
        if (manufacturerData != null)
        {
            foreach (var kvp in manufacturerData)
            {
                // 杰理常用 Company ID: 0x05D6 等，或厂商自定义字段 0x3E21
                if (kvp.Key == 0x3E21 || kvp.Key == 0x3E22 || kvp.Key == 0x3D11 || kvp.Key == 0x12CC)
                {
                    return true;
                }

                // 检查 payload 中是否包含 0x12CC 或 0x3E21
                var bytes = kvp.Value;
                if (bytes != null && bytes.Length >= 2)
                {
                    for (int i = 0; i < bytes.Length - 1; i++)
                    {
                        ushort word = (ushort)(bytes[i] | (bytes[i + 1] << 8));
                        if (word == 0x12CC || word == 0x3E21 || word == 0x3E22 || word == 0x3D11)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        // 3. 检查设备名称前缀
        if (!string.IsNullOrWhiteSpace(localName))
        {
            string upper = localName.Trim().ToUpperInvariant();
            foreach (var prefix in KnownNamePrefixes)
            {
                if (upper.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    upper.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
