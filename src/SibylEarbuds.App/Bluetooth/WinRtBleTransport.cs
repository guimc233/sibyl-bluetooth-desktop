#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
#endif
using SibylEarbuds.Core.Protocol;
using SibylEarbuds.Core.Transport;

namespace SibylEarbuds.App.Bluetooth;

/// <summary>
/// 基于 Windows 10/11 原生 WinRT BLE 接口的低功耗蓝牙通信实现
/// 特性:
/// 1. 纯净 WinRT GATT 调用，绝不触碰 A2DP 音频流，绝不占用 COM 串口，确保零音频卡顿
/// 2. 自动搜索匹配 SIBYL/杰理/炬力 GATT 服务与特征值，支持自适应特征匹配
/// 3. 支持 WriteWithoutResponse 毫秒级免响应写入
/// </summary>
public class WinRtBleTransport : IBleTransport
{
    public event Action<byte[]>? OnDataReceived;
    public event Action<bool>? OnConnectionStateChanged;
    public event Action<string>? OnLog;

    public bool IsConnected { get; private set; }
    public string? ConnectedDeviceName { get; private set; }
    public string? ConnectedDeviceId { get; private set; }

#if WINDOWS
    private BluetoothLEDevice? _bluetoothLeDevice;
    private GattCharacteristic? _writeCharacteristic;
    private GattCharacteristic? _notifyCharacteristic;
#endif

    public async Task<IReadOnlyList<DiscoveredBleDevice>> ScanDevicesAsync(int timeoutMs = 4000)
    {
        var discovered = new Dictionary<string, DiscoveredBleDevice>();
#if WINDOWS
        OnLog?.Invoke("[WinRT-BLE] 启动 Windows 10/11 蓝牙广播监听器 (AdvertisementWatcher)...");
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        watcher.Received += (sender, args) =>
        {
            string devName = args.Advertisement.LocalName;
            if (string.IsNullOrWhiteSpace(devName)) return;

            string id = args.BluetoothAddress.ToString("X");
            discovered[id] = new DiscoveredBleDevice(id, devName, args.RawSignalStrengthInDBm);
        };

        watcher.Start();
        await Task.Delay(timeoutMs);
        watcher.Stop();
        OnLog?.Invoke($"[WinRT-BLE] 扫描完成，发现 {discovered.Count} 个蓝牙设备");
#else
        OnLog?.Invoke("[WinRT-BLE] 当前运行在非 Windows 平台，建议使用虚拟模拟模式测试");
        await Task.Delay(200);
#endif
        return discovered.Values.OrderByDescending(d => d.Rssi).ToList();
    }

    public async Task<bool> ConnectAsync(string deviceId)
    {
#if WINDOWS
        try
        {
            OnLog?.Invoke($"[WinRT-BLE] 正在连接设备: {deviceId}...");
            ulong address = Convert.ToUInt64(deviceId, 16);
            _bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

            if (_bluetoothLeDevice == null)
            {
                OnLog?.Invoke("[WinRT-BLE-ERR] 无法实例化 BluetoothLEDevice，请确认设备已开机并在范围内");
                return false;
            }

            _bluetoothLeDevice.ConnectionStatusChanged += (dev, args) =>
            {
                bool connected = dev.ConnectionStatus == BluetoothConnectionStatus.Connected;
                IsConnected = connected;
                OnConnectionStateChanged?.Invoke(connected);
                OnLog?.Invoke($"[WinRT-BLE] 连接状态变更为: {dev.ConnectionStatus}");
            };

            // 获取 GATT 服务
            var gattServicesResult = await _bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (gattServicesResult.Status != GattCommunicationStatus.Success)
            {
                OnLog?.Invoke($"[WinRT-BLE-ERR] 获取 GATT 服务失败: {gattServicesResult.Status}");
                return false;
            }

            // 寻找匹配的 SIBYL 或通用特征
            GattDeviceService? targetService = null;
            foreach (var s in gattServicesResult.Services)
            {
                if (SibylUuids.CandidateServiceUuids.Contains(s.Uuid))
                {
                    targetService = s;
                    break;
                }
            }

            targetService ??= gattServicesResult.Services.FirstOrDefault();

            if (targetService == null)
            {
                OnLog?.Invoke("[WinRT-BLE-ERR] 未找到适用的 GATT 服务");
                return false;
            }

            var characteristicsResult = await targetService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (characteristicsResult.Status != GattCommunicationStatus.Success)
            {
                OnLog?.Invoke("[WinRT-BLE-ERR] 获取 GATT 特征值失败");
                return false;
            }

            foreach (var ch in characteristicsResult.Characteristics)
            {
                var props = ch.CharacteristicProperties;
                if ((props.HasFlag(GattCharacteristicProperties.Write) || props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                    && _writeCharacteristic == null)
                {
                    _writeCharacteristic = ch;
                }

                if ((props.HasFlag(GattCharacteristicProperties.Notify) || props.HasFlag(GattCharacteristicProperties.Indicate))
                    && _notifyCharacteristic == null)
                {
                    _notifyCharacteristic = ch;
                }
            }

            if (_writeCharacteristic == null)
            {
                OnLog?.Invoke("[WinRT-BLE-WARN] 未找到具有写入权限的特征值");
                return false;
            }

            if (_notifyCharacteristic != null)
            {
                await _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                _notifyCharacteristic.ValueChanged += (ch, args) =>
                {
                    using var reader = DataReader.FromBuffer(args.CharacteristicValue);
                    byte[] data = new byte[args.CharacteristicValue.Length];
                    reader.ReadBytes(data);
                    OnDataReceived?.Invoke(data);
                };
            }

            IsConnected = true;
            ConnectedDeviceId = deviceId;
            ConnectedDeviceName = _bluetoothLeDevice.Name;
            OnConnectionStateChanged?.Invoke(true);
            OnLog?.Invoke($"[WinRT-BLE] 成功建立完全隔离的低功耗通信链路，设备: {ConnectedDeviceName}");
            return true;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[WinRT-BLE-ERR] 连接异常: {ex.Message}");
            return false;
        }
#else
        await Task.Delay(100);
        return false;
#endif
    }

    public async Task DisconnectAsync()
    {
#if WINDOWS
        if (_notifyCharacteristic != null)
        {
            try
            {
                await _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.None);
            }
            catch { }
            _notifyCharacteristic = null;
        }

        _writeCharacteristic = null;

        if (_bluetoothLeDevice != null)
        {
            _bluetoothLeDevice.Dispose();
            _bluetoothLeDevice = null;
        }
#endif
        IsConnected = false;
        ConnectedDeviceId = null;
        ConnectedDeviceName = null;
        OnConnectionStateChanged?.Invoke(false);
        OnLog?.Invoke("[WinRT-BLE] 设备已断开连接");
        await Task.CompletedTask;
    }

    public async Task<bool> WriteCharacteristicAsync(byte[] data, bool writeWithoutResponse = false)
    {
#if WINDOWS
        if (_writeCharacteristic == null) return false;

        try
        {
            using var writer = new DataWriter();
            writer.WriteBytes(data);
            var buffer = writer.DetachBuffer();

            var option = writeWithoutResponse ? GattWriteOption.WriteWithoutResponse : GattWriteOption.WriteWithResponse;
            var result = await _writeCharacteristic.WriteValueWithResultAsync(buffer, option);
            return result.Status == GattCommunicationStatus.Success;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[WinRT-BLE-WRITE-ERR] {ex.Message}");
            return false;
        }
#else
        await Task.Delay(10);
        return false;
#endif
    }

    public void Dispose()
    {
        _ = DisconnectAsync();
    }
}
