using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;
using SibylEarbuds.Core.Transport;

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

    // 仅模拟官方产品清单内的机型（与真实扫描器一致：只出现官方 SIBYL 耳机）
    private static readonly List<DiscoveredBleDevice> _mockDevices =
    [
        new("A0:01:01:00:00:01", "SIBYL S1 ANC (旗舰真无线)", -52)
        {
            IsSibylVerified = true,
            VendorId = 15377,           // 0x3C11 -> S1
            ModelName = "S1",
            ChipName = "杰理 AC6973D8",
            LeftBattery = 85,
            RightBattery = 90,
            CaseBattery = 100
        },
        new("A0:01:01:00:00:02", "SIBYL S7", -58)
        {
            IsSibylVerified = true,
            VendorId = 15889,           // 0x3E11 -> S7
            ModelName = "S7",
            ChipName = "杰理 AC6973D8",
            LeftBattery = 70,
            RightBattery = 75,
            CaseBattery = 60
        },
        new("A0:01:01:00:00:03", "SIBYL S10 (半入耳音乐)", -70)
        {
            IsSibylVerified = true,
            VendorId = 15895,           // 0x3E17 -> S10
            ModelName = "S10",
            ChipName = "杰理 AC6973D8",
            LeftBattery = 40,
            RightBattery = 35,
            CaseBattery = 20
        },
        new("A0:01:01:00:00:04", "SIBYL B1 (Dual-Mode)", -48)
        {
            IsSibylVerified = true,
            VendorId = 16017,           // 0x3E91 -> B1
            ModelName = "B1",
            ChipName = "杰理 AC6973D8",
            LeftBattery = 88,
            RightBattery = 92,
            CaseBattery = 100
        },
        new("A0:01:01:00:00:05", "SIBYL Y1 (低延迟游戏版)", -66)
        {
            IsSibylVerified = true,
            VendorId = 15985,           // 0x3E71 -> Y1
            ModelName = "Y1",
            ChipName = "炬力 ATS3015",
            LeftBattery = 65,
            RightBattery = 62,
            CaseBattery = -1
        }
    ];

    public Task StartContinuousScanAsync()
    {
        if (_isScanning) return Task.CompletedTask;
        _isScanning = true;
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        OnLog?.Invoke("[MOCK] 开启持续后台蓝牙扫描 (官方厂商广播过滤: 仅 SIBYL 耳机)...");

        _ = Task.Run(async () =>
        {
            int index = 0;
            while (!token.IsCancellationRequested && _isScanning)
            {
                if (index < _mockDevices.Count)
                {
                    var dev = _mockDevices[index++];
                    OnDeviceFound?.Invoke(dev);
                    OnLog?.Invoke($"[MOCK-SCAN] 发现 SIBYL 设备: {dev.Name} (机型: {dev.ModelName}, VendorId: 0x{dev.VendorId:X4}, RSSI: {dev.Rssi} dBm)");
                }
                else
                {
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
        OnLog?.Invoke("[MOCK] 单次扫描低功耗蓝牙设备 (官方厂商广播过滤)...");
        await Task.Delay(500);
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
        await Task.Delay(300);

        IsConnected = true;
        ConnectedDeviceId = deviceId;
        var matched = _mockDevices.FirstOrDefault(d => d.Id.Equals(deviceId, StringComparison.OrdinalIgnoreCase));
        ConnectedDeviceName = matched?.Name ?? "SIBYL S1 ANC";
        OnConnectionStateChanged?.Invoke(true);
        OnLog?.Invoke($"[MOCK] 连接成功! GATT 通信通道就绪（机型自动适配: {matched?.ModelName ?? "默认"}），A2DP 音频纯净隔离");

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
            OnLog?.Invoke("[MOCK] 设备已断开");
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
        byte[] batPayload = [_leftBattery, _rightBattery, _caseBattery];
        var batPacket = PacketBuilder.BuildPacket(SibylCommandId.Battery, batPayload);
        OnDataReceived?.Invoke(batPacket);

        byte[] ancPayload = [_ancMode, _ancDepth];
        var ancPacket = PacketBuilder.BuildPacket(SibylCommandId.AncMode, ancPayload);
        OnDataReceived?.Invoke(ancPacket);

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