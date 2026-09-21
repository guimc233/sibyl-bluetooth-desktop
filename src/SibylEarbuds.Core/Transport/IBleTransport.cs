namespace SibylEarbuds.Core.Transport;

public record DiscoveredBleDevice(string Id, string Name, int Rssi)
{
    public DateTime LastSeen { get; init; } = DateTime.UtcNow;
    public bool IsSibylVerified { get; init; } = true;
    public string? ChipName { get; init; }

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
