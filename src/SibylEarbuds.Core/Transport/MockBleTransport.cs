using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;

namespace SibylEarbuds.Core.Transport;

/// <summary>
/// 虚拟耳机模拟传输实现
/// 提供完备的固件状态仿真与数据回路，用于演示、UI 调试与自动化测试
/// </summary>
public class MockBleTransport : IBleTransport
{
    public event Action<byte[]>? OnDataReceived;
    public event Action<bool>? OnConnectionStateChanged;
    public event Action<string>? OnLog;

    public bool IsConnected { get; private set; }
    public string? ConnectedDeviceName { get; private set; }
    public string? ConnectedDeviceId { get; private set; }

    private byte _ancMode = (byte)AncModeType.NoiseReduction;
    private byte _ancDepth = (byte)AncDepthLevel.Deep;
    private bool _gameMode = false;
    private byte _leftBattery = 85;
    private byte _rightBattery = 90;
    private byte _caseBattery = 100;

    public async Task<IReadOnlyList<DiscoveredBleDevice>> ScanDevicesAsync(int timeoutMs = 4000)
    {
        OnLog?.Invoke("[MOCK] 开始扫描低功耗蓝牙设备...");
        await Task.Delay(800);

        var list = new List<DiscoveredBleDevice>
        {
            new("MOCK-DEV-01", "SIBYL B1 Pro (Dual-Mode)", -48),
            new("MOCK-DEV-02", "SIBYL S10 ANC (Headphone)", -62),
            new("MOCK-DEV-03", "SIBYL Cyber Light (TWS)", -55)
        };

        OnLog?.Invoke($"[MOCK] 发现 {list.Count} 个 SIBYL 蓝牙音频设备");
        return list;
    }

    public async Task<bool> ConnectAsync(string deviceId)
    {
        OnLog?.Invoke($"[MOCK] 正在连接设备 {deviceId}...");
        await Task.Delay(600);

        IsConnected = true;
        ConnectedDeviceId = deviceId;
        ConnectedDeviceName = deviceId.Contains("02") ? "SIBYL S10 ANC" : "SIBYL B1 Pro";
        OnConnectionStateChanged?.Invoke(true);
        OnLog?.Invoke($"[MOCK] 连接成功! GATT 服务与特征已完成握手 (完全隔离于 Windows A2DP)");

        // 立即上报当前状态
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
            OnLog?.Invoke($"[MOCK-RX] 耳机微控制器接收到有效指令: {parsed.CommandId} (Len: {data.Length} B)");

            // 模拟耳机处理响应
            switch (parsed.CommandId)
            {
                case SibylCommandId.AncMode when parsed.Payload.Length >= 1:
                    _ancMode = parsed.Payload[0];
                    if (parsed.Payload.Length >= 2) _ancDepth = parsed.Payload[1];
                    OnLog?.Invoke($"[MOCK-ANC] 耳机 ANC 模式已平滑切换为: {(AncModeType)_ancMode}, 深度: {_ancDepth}");
                    SendMockStatusReport();
                    break;

                case SibylCommandId.Equalizer when parsed.Payload.Length >= 10:
                    OnLog?.Invoke($"[MOCK-DSP] Realtek/JieLi DSP 硬件 EQ 滤波器系数重载完成 (10段增益已实时应用)");
                    break;

                case SibylCommandId.GameMode when parsed.Payload.Length >= 1:
                    _gameMode = parsed.Payload[0] == 1;
                    OnLog?.Invoke($"[MOCK-LATENCY] 耳机低延迟游戏模式: {(_gameMode ? "已激活 (38ms)" : "已关闭 (高音质)")}");
                    SendMockStatusReport();
                    break;

                case SibylCommandId.CloseTouch when parsed.Payload.Length >= 1:
                    OnLog?.Invoke($"[MOCK-TOUCH] 触控防误触锁定状态更新为: {parsed.Payload[0] == 1}");
                    break;

                case SibylCommandId.LightMode when parsed.Payload.Length >= 6:
                    OnLog?.Invoke($"[MOCK-LED] 灯效更新: 模式={parsed.Payload[0]}, RGB=({parsed.Payload[3]},{parsed.Payload[4]},{parsed.Payload[5]})");
                    break;

                case SibylCommandId.QueryInfo:
                    SendMockStatusReport();
                    break;
            }
        }
        else
        {
            OnLog?.Invoke($"[MOCK-ERR] 数据包校验失败: {parsed.ErrorMessage}");
        }

        return Task.FromResult(true);
    }

    private void SendMockStatusReport()
    {
        if (!IsConnected) return;

        // 1. 电量通知 (Battery 12)
        byte[] batPayload = [_leftBattery, _rightBattery, _caseBattery];
        var batPacket = PacketBuilder.BuildPacket(SibylCommandId.Battery, batPayload);
        OnDataReceived?.Invoke(batPacket);

        // 2. ANC 状态通知 (AncMode 9)
        byte[] ancPayload = [_ancMode, _ancDepth];
        var ancPacket = PacketBuilder.BuildPacket(SibylCommandId.AncMode, ancPayload);
        OnDataReceived?.Invoke(ancPacket);

        // 3. 游戏模式通知 (GameMode 14)
        var gamePacket = PacketBuilder.BuildPacket(SibylCommandId.GameMode, [(byte)(_gameMode ? 1 : 0)]);
        OnDataReceived?.Invoke(gamePacket);

        OnLog?.Invoke($"[MOCK-NOTIFY] 耳机电量与状态回传: 左耳={_leftBattery}% 右耳={_rightBattery}% 仓={_caseBattery}% | ANC={(AncModeType)_ancMode}");
    }

    public void Dispose()
    {
        IsConnected = false;
    }
}
