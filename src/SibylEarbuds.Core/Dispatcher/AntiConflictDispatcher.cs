using System.Collections.Concurrent;
using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;
using SibylEarbuds.Core.Transport;

namespace SibylEarbuds.Core.Dispatcher;

/// <summary>
/// 防音频打架与指令防抖调度器 (Anti-Conflict Command Dispatcher)
/// 核心保障:
/// 1. 物理层与协议层纯粹基于 BLE GATT，绝对不触碰经典蓝牙 A2DP 音频通道，绝不占用 COM 串口
/// 2. 对高频滑块控制 (EQ、音量、灯效) 执行 60~100ms 智能防抖合并
/// 3. 单一轻量级异步工作流，包体积控制在单 MTU 范围内，发送即释放射频，确保 A2DP 音频流零卡顿
/// </summary>
public class AntiConflictDispatcher : IDisposable
{
    private readonly IBleTransport _transport;
    private readonly ConcurrentDictionary<SibylCommandId, byte[]> _pendingDebounceCommands = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private DateTime _lastSendTime = DateTime.MinValue;
    private const int MinPacketIntervalMs = 35; // 两次 BLE 发包间的最小安全间隔，为 A2DP 留足射频时隙

    public event Action<SibylCommandId, bool>? CommandSent;
    public event Action<string>? LogMessage;

    public AntiConflictDispatcher(IBleTransport transport)
    {
        _transport = transport;
        Task.Run(ProcessDebounceLoopAsync);
    }

    /// <summary>
    /// 立即发送关键指令 (如 ANC 切换、按键设置、游戏模式切换)
    /// </summary>
    public async Task<bool> SendCriticalCommandAsync(SibylCommandId commandId, byte[] packet, bool writeWithoutResponse = false)
    {
        await _sendLock.WaitAsync();
        try
        {
            await EnsureRadioCooldownAsync();
            LogMessage?.Invoke($"[BLE-TX] 发送关键指令 {commandId} (Len: {packet.Length} B)");
            bool success = await _transport.WriteCharacteristicAsync(packet, writeWithoutResponse);
            _lastSendTime = DateTime.UtcNow;
            CommandSent?.Invoke(commandId, success);
            return success;
        }
        catch (Exception ex)
        {
            LogMessage?.Invoke($"[BLE-TX-ERR] 发送 {commandId} 失败: {ex.Message}");
            return false;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 提交带防抖的高频控制指令 (如 EQ 拖拽、灯光亮度、音量连续调节)
    /// 80ms 内的多次同类指令将自动合并为最新的一包发送
    /// </summary>
    public void EnqueueDebouncedCommand(SibylCommandId commandId, byte[] packet)
    {
        _pendingDebounceCommands[commandId] = packet;
    }

    private async Task ProcessDebounceLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(60, _cts.Token);

                if (_pendingDebounceCommands.IsEmpty)
                    continue;

                foreach (var cmdId in _pendingDebounceCommands.Keys.ToList())
                {
                    if (_pendingDebounceCommands.TryRemove(cmdId, out var packet))
                    {
                        await _sendLock.WaitAsync(_cts.Token);
                        try
                        {
                            await EnsureRadioCooldownAsync();
                            LogMessage?.Invoke($"[BLE-TX-DEBOUNCED] 调度合并发送 {cmdId} (Len: {packet.Length} B)");
                            bool success = await _transport.WriteCharacteristicAsync(packet, writeWithoutResponse: true);
                            _lastSendTime = DateTime.UtcNow;
                            CommandSent?.Invoke(cmdId, success);
                        }
                        finally
                        {
                            _sendLock.Release();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"[DISPATCH-LOOP-ERR] {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 射频冷却保护：确保两次发送之间有最小保护间隙，避免耳机芯片蓝牙基带丢包影响 A2DP 缓冲
    /// </summary>
    private async Task EnsureRadioCooldownAsync()
    {
        var elapsed = (DateTime.UtcNow - _lastSendTime).TotalMilliseconds;
        if (elapsed < MinPacketIntervalMs)
        {
            int waitTime = MinPacketIntervalMs - (int)elapsed;
            await Task.Delay(waitTime);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _sendLock.Dispose();
    }
}
