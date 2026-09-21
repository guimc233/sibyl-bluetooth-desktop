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

public class WinRtBleTransport : IBleTransport
{
    public event Action<byte[]>? OnDataReceived;
    public event Action<bool>? OnConnectionStateChanged;
    public event Action<string>? OnLog;
    public event Action<DiscoveredBleDevice>? OnDeviceFound;

    public bool IsConnected { get; private set; }
    public string? ConnectedDeviceName { get; private set; }
    public string? ConnectedDeviceId { get; private set; }

#if WINDOWS
    private BluetoothLEAdvertisementWatcher? _continuousWatcher;
    private BluetoothLEDevice? _bluetoothLeDevice;
    private GattCharacteristic? _writeCharacteristic;
    private GattCharacteristic? _notifyCharacteristic;
#endif

    public Task StartContinuousScanAsync()
    {
#if WINDOWS
        try
        {
            if (_continuousWatcher != null)
            {
                if (_continuousWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
                    return Task.CompletedTask;
                try { _continuousWatcher.Stop(); } catch { }
            }

            OnLog?.Invoke("[WinRT-BLE] 启动 Windows 10/11 连续蓝牙广播监听...");
            _continuousWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            _continuousWatcher.Received += (sender, args) =>
            {
                string devName = args.Advertisement.LocalName;
                string id = args.BluetoothAddress.ToString("X");

                // 收集广播的 Service UUIDs
                var serviceUuids = args.Advertisement.ServiceUuids.ToList();

                // 收集 Manufacturer Data
                var manData = new Dictionary<ushort, byte[]>();
                foreach (var md in args.Advertisement.ManufacturerData)
                {
                    using var reader = DataReader.FromBuffer(md.Data);
                    byte[] bytes = new byte[md.Data.Length];
                    reader.ReadBytes(bytes);
                    manData[md.CompanyId] = bytes;
                }

                // 采用官方 APK 精确过滤算法：判断是否为 SIBYL 耳机
                bool isSibyl = SibylDeviceMatcher.IsSibylEarbuds(devName, serviceUuids, manData);

                // 如果名称为空且不是 SIBYL 广播特征，则忽略无用 Beacon
                if (string.IsNullOrWhiteSpace(devName) && !isSibyl) return;

                string displayName = string.IsNullOrWhiteSpace(devName)
                    ? (isSibyl ? "SIBYL 无线耳机 (待命名)" : $"未知蓝牙设备 [{id}]")
                    : devName;

                var discovered = new DiscoveredBleDevice(id, displayName, args.RawSignalStrengthInDBm)
                {
                    IsSibylVerified = isSibyl,
                    LastSeen = DateTime.UtcNow
                };

                OnDeviceFound?.Invoke(discovered);
            };

            _continuousWatcher.Start();
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[WinRT-BLE-ERR] 启动连续扫描失败: {ex.Message}");
        }
#else
        OnLog?.Invoke("[WinRT-BLE] 非 Windows 环境");
#endif
        return Task.CompletedTask;
    }

    public Task StopContinuousScanAsync()
    {
#if WINDOWS
        if (_continuousWatcher != null)
        {
            try
            {
                _continuousWatcher.Stop();
            }
            catch { }
            _continuousWatcher = null;
            OnLog?.Invoke("[WinRT-BLE] 停止连续蓝牙扫描");
        }
#endif
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<DiscoveredBleDevice>> ScanDevicesAsync(int timeoutMs = 4000)
    {
        var discovered = new Dictionary<string, DiscoveredBleDevice>();
#if WINDOWS
        OnLog?.Invoke("[WinRT-BLE] 启动单次扫描...");
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        watcher.Received += (sender, args) =>
        {
            string devName = args.Advertisement.LocalName;
            string id = args.BluetoothAddress.ToString("X");
            var serviceUuids = args.Advertisement.ServiceUuids.ToList();
            bool isSibyl = SibylDeviceMatcher.IsSibylEarbuds(devName, serviceUuids, null);

            if (string.IsNullOrWhiteSpace(devName) && !isSibyl) return;

            string displayName = string.IsNullOrWhiteSpace(devName) ? $"SIBYL 耳机 [{id}]" : devName;
            var item = new DiscoveredBleDevice(id, displayName, args.RawSignalStrengthInDBm)
            {
                IsSibylVerified = isSibyl
            };
            discovered[id] = item;
            OnDeviceFound?.Invoke(item);
        };

        watcher.Start();
        await Task.Delay(timeoutMs);
        watcher.Stop();
#else
        await Task.Delay(200);
#endif
        return discovered.Values.OrderByDescending(d => d.Rssi).ToList();
    }

    public async Task<bool> ConnectAsync(string deviceId)
    {
        await StopContinuousScanAsync();
#if WINDOWS
        try
        {
            OnLog?.Invoke($"[WinRT-BLE] 正在连接设备: {deviceId}...");
            ulong address = Convert.ToUInt64(deviceId, 16);
            _bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(address);

            if (_bluetoothLeDevice == null)
            {
                OnLog?.Invoke("[WinRT-BLE-ERR] 无法实例化 BluetoothLEDevice，请确认设备在有效范围内且未被其他应用独占");
                return false;
            }

            _bluetoothLeDevice.ConnectionStatusChanged += (dev, args) =>
            {
                bool connected = dev.ConnectionStatus == BluetoothConnectionStatus.Connected;
                IsConnected = connected;
                OnConnectionStateChanged?.Invoke(connected);
                OnLog?.Invoke($"[WinRT-BLE] 蓝牙底层连接状态变更为: {dev.ConnectionStatus}");
            };

            var gattServicesResult = await _bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (gattServicesResult.Status != GattCommunicationStatus.Success)
            {
                OnLog?.Invoke($"[WinRT-BLE-ERR] 获取 GATT 服务失败: {gattServicesResult.Status}");
                return false;
            }

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
        _ = StopContinuousScanAsync();
        _ = DisconnectAsync();
    }
}
