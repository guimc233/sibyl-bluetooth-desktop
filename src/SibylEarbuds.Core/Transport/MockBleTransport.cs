using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;

namespace SibylEarbuds.Core.Transport;

public class MockBleTransport : IBleTransport
{
    public event Action<byte[]>? OnDataReceived;
    public event Action<bool>? OnConnectionStateChanged;
    public event Action<string>? OnLog;
    public event Action<DiscoveredBleDevice>? OnDeviceFound;

    public bool IsConnected { get; private set; }
    public string? ConnectedDeviceName { get; private set; }
    public string? ConnectedDeviceId { get; private set; }

    private byte _ancMode = (byte)AncModeType.NoiseReduction;
    private byte _ancDepth = (byte)AncDepthLevel.Deep;
    private bool _gameMode = false;
    private byte _leftBattery = 85;
    private byte _rightBattery = 90;
    private byte _caseBattery = 100;

    private bool _isScanning = false;
    private CancellationTokenSource? _scanCts;

    private static readonly List<DiscoveredBleDevice> _mockDevices =
    [
        new("SIBYL-B1-PRO", "SIBYL B1 Pro (Dual-Mode)", -48),
        new("SIBYL-S1-ANC", "SIBYL S1 ANC (旗舰真无线)", -54),
        new("SIBYL-B6-RGB", "SIBYL B6 (RGB电竞)", -62),
        new("SIBYL-S10-TWS", "SIBYL S10 (半入耳音乐)", -70),
        new("SIBYL-S11-LDAC", "SIBYL S11 LDAC (Hi-Res)", -58),
        new("SIBYL-Y1-LOWLATENCY", "SIBYL Y1 (低延迟游戏版)", -66)
    ];

    public Task StartContinuousScanAsync()
    {
        if (_isScanning) return Task.CompletedTask;
        _isScanning = true;
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        OnLog?.Invoke("[MOCK] 开启持续后台蓝牙扫描...");

        _ = Task.Run(async () =>
        {
            int index = 0;
            while (!token.IsCancellationRequested && _isScanning)
            {
                if (index < _mockDevices.Count)
                {
                    var dev = _mockDevices[index++];
                    OnDeviceFound?.Invoke(dev);
                    OnLog?.Invoke($"[MOCK-SCAN] 发现设备: {dev.Name} (RSSI: {dev.Rssi} dBm)");
                }
                else
                {
                    // 模拟微弱信号波动
                    var rand = new Random();
                    var dev = _mockDevices[rand.Next(_mockDevices.Count)];
                    var updated = dev with { Rssi = -45 - rand.Next(35), LastSeen = DateTime.UtcNow };
                    OnDeviceFound?.Invoke(updated);
                }

                await Task.Delay(1200, token);
            }
        }, token);

        return Task.CompletedTask;
    }

    public Task StopContinuousScanAsync()
    {
        _isScanning = false;
        _scanCts?.Cancel();
        _scanCts = null;
        OnLog?.Invoke("[MOCK] 停止持续后台扫描");
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<DiscoveredBleDevice>> ScanDevicesAsync(int timeoutMs = 4000)
    {
        OnLog?.Invoke("[MOCK] 单次扫描低功耗蓝牙设备...");
        await Task.Delay(600);
        foreach (var dev in _mockDevices)
        {
            OnDeviceFound?.Invoke(dev);
        }
        return _mockDevices;
    }

    public async Task<bool> ConnectAsync(string deviceId)
    {
        await StopContinuousScanAsync();
        OnLog?.Invoke($"[MOCK] 正在连接设备 {deviceId}...");
        await Task.Delay(400);

        IsConnected = true;
        ConnectedDeviceId = deviceId;
        var matched = _mockDevices.FirstOrDefault(d => d.Id == deviceId);
        ConnectedDeviceName = matched?.Name ?? "SIBYL S1 ANC";
        OnConnectionStateChanged?.Invoke(true);
        OnLog?.Invoke($"[MOCK] 连接成功! GATT 通信就绪，与 A2DP 物理隔离");

        SendMockStatusReport();
        return true;
    }

    public Task DisconnectAsync()
    {
        if (IsConnected)
        {
            IsConnected = false;
            ConnectedDeviceId = null;
            ConnectedDeviceName = null;
            OnConnectionStateChanged?.Invoke(false);
            OnLog?.Invoke("[MOCK] 设备已安全断开");
        }
        return Task.CompletedTask;
    }

    public Task<bool> WriteCharacteristicAsync(byte[] data, bool writeWithoutResponse = false)
    {
        if (!IsConnected)
        {
            OnLog?.Invoke("[MOCK-ERR] 未连接设备，无法写入");
            return Task.FromResult(false);
        }

        var parsed = PacketParser.Parse(data);
        if (parsed.IsSuccess)
        {
            switch (parsed.CommandId)
            {
                case SibylCommandId.AncMode when parsed.Payload.Length >= 1:
                    _ancMode = parsed.Payload[0];
                    if (parsed.Payload.Length >= 2) _ancDepth = parsed.Payload[1];
                    break;
                case SibylCommandId.GameMode when parsed.Payload.Length >= 1:
                    _gameMode = parsed.Payload[0] == 1;
                    break;
                case SibylCommandId.Battery:
                case SibylCommandId.QueryInfo:
                    SendMockStatusReport();
                    break;
            }
        }
        return Task.FromResult(true);
    }

    private void SendMockStatusReport()
    {
        if (!IsConnected) return;

        // 1. 发送电量包 (SibylCommandId.Battery)
        byte[] batPayload = [_leftBattery, _rightBattery, _caseBattery];
        var batPacket = PacketBuilder.BuildPacket(SibylCommandId.Battery, batPayload);
        OnDataReceived?.Invoke(batPacket);

        // 2. 发送 ANC 状态包 (SibylCommandId.AncMode)
        byte[] ancPayload = [_ancMode, _ancDepth];
        var ancPacket = PacketBuilder.BuildPacket(SibylCommandId.AncMode, ancPayload);
        OnDataReceived?.Invoke(ancPacket);

        // 3. 发送游戏模式包 (SibylCommandId.GameMode)
        byte[] gamePayload = [(byte)(_gameMode ? 1 : 0)];
        var gamePacket = PacketBuilder.BuildPacket(SibylCommandId.GameMode, gamePayload);
        OnDataReceived?.Invoke(gamePacket);
    }

    public void Dispose()
    {
        _ = StopContinuousScanAsync();
        IsConnected = false;
    }
}
