namespace SibylEarbuds.Core.Transport;

public record DiscoveredBleDevice(string Id, string Name, int Rssi)
{
    public DateTime LastSeen { get; init; } = DateTime.UtcNow;
    public bool IsSibylVerified { get; init; } = true;
    public string? ChipName { get; init; }

    /// <summary>官方 VendorId（来自厂商广播前 2 字节，Big-Endian），用于自动机型适配。</summary>
    public int VendorId { get; init; }

    /// <summary>官方产品清单解析出的机型名（如 S1 / B1 / Y1），空表示未知机型。</summary>
    public string ModelName { get; init; } = string.Empty;

    /// <summary>广播自带的电量信息（-1 表示未知）。</summary>
    public int LeftBattery { get; init; } = -1;

    public int RightBattery { get; init; } = -1;

    public int CaseBattery { get; init; } = -1;

    public string SignalLevel => Rssi switch
    {
        >= -55 => "极佳",
        >= -70 => "良好",
        >= -85 => "一般",
        _ => "较弱"
    };

    public string SignalIcon => Rssi switch
    {
        >= -55 => "●●●● 极佳",
        >= -70 => "●●●○ 良好",
        >= -85 => "●●○○ 一般",
        _ => "●○○○ 较弱"
    };
}

public interface IBleTransport : IDisposable
{
    event Action<byte[]>? OnDataReceived;
    event Action<bool>? OnConnectionStateChanged;
    event Action<string>? OnLog;
    event Action<DiscoveredBleDevice>? OnDeviceFound; // 实时发现设备事件

    bool IsConnected { get; }
    string? ConnectedDeviceName { get; }
    string? ConnectedDeviceId { get; }

    Task StartContinuousScanAsync();
    Task StopContinuousScanAsync();
    Task<IReadOnlyList<DiscoveredBleDevice>> ScanDevicesAsync(int timeoutMs = 4000);
    Task<bool> ConnectAsync(string deviceId);
    Task DisconnectAsync();
    Task<bool> WriteCharacteristicAsync(byte[] data, bool writeWithoutResponse = false);
}
