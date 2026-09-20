namespace SibylEarbuds.Core.Transport;

public record DiscoveredBleDevice(string Id, string Name, int Rssi);

public interface IBleTransport : IDisposable
{
    event Action<byte[]>? OnDataReceived;
    event Action<bool>? OnConnectionStateChanged;
    event Action<string>? OnLog;

    bool IsConnected { get; }
    string? ConnectedDeviceName { get; }
    string? ConnectedDeviceId { get; }

    Task<IReadOnlyList<DiscoveredBleDevice>> ScanDevicesAsync(int timeoutMs = 4000);
    Task<bool> ConnectAsync(string deviceId);
    Task DisconnectAsync();
    Task<bool> WriteCharacteristicAsync(byte[] data, bool writeWithoutResponse = false);
}
