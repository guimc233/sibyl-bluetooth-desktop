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
/// 2. 严格匹配 SIBYL 官方 00FE (Write 00F1 / Notify 00F2) 及杰理/中科蓝讯协议特征
/// 3. 支持跨所有服务智能自适应探测，保障所有双模蓝牙耳机通道畅通
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

            // 获取所有 GATT 服务
            var gattServicesResult = await _bluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (gattServicesResult.Status != GattCommunicationStatus.Success)
            {
                OnLog?.Invoke($"[WinRT-BLE-ERR] 获取 GATT 服务失败: {gattServicesResult.Status}");
                return false;
            }

            _writeCharacteristic = null;
            _notifyCharacteristic = null;

            // 1. 优先在官方 Sibyl 服务 00FE 中查找 00F1 / 00F2
            foreach (var service in gattServicesResult.Services)
            {
                OnLog?.Invoke($"[WinRT-BLE] 扫描到 GATT 服务: {service.Uuid}");
                var charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                if (charResult.Status != GattCommunicationStatus.Success) continue;

                foreach (var ch in charResult.Characteristics)
                {
                    OnLog?.Invoke($"[WinRT-BLE]   -> 特征值: {ch.Uuid} (属性: {ch.CharacteristicProperties})");

                    if (ch.Uuid == SibylUuids.SibylWriteUuid || ch.Uuid == SibylUuids.JieLiWriteUuid || ch.Uuid == SibylUuids.JieLiWrite2Uuid)
                    {
                        _writeCharacteristic = ch;
                    }
                    if (ch.Uuid == SibylUuids.SibylNotifyUuid || ch.Uuid == SibylUuids.JieLiNotifyUuid || ch.Uuid == SibylUuids.JieLiNotify2Uuid)
                    {
                        _notifyCharacteristic = ch;
                    }
                }
            }

            // 2. 如果官方特定特征未直接命中，则自适应寻找非标准系统服务中具备读写和Notify能力的特征
            if (_writeCharacteristic == null || _notifyCharacteristic == null)
            {
                foreach (var service in gattServicesResult.Services)
                {
                    // 跳过 Generic Access (1800), Generic Attribute (1801), Device Info (180A)
                    string sUuid = service.Uuid.ToString().ToUpperInvariant();
                    if (sUuid.StartsWith("00001800") || sUuid.StartsWith("00001801") || sUuid.StartsWith("0000180A"))
                    {
                        continue;
                    }

                    var charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (charResult.Status != GattCommunicationStatus.Success) continue;

                    foreach (var ch in charResult.Characteristics)
                    {
                        var props = ch.CharacteristicProperties;
                        if (_writeCharacteristic == null &&
                            (props.HasFlag(GattCharacteristicProperties.Write) || props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse)))
                        {
                            _writeCharacteristic = ch;
                        }

                        if (_notifyCharacteristic == null &&
                            (props.HasFlag(GattCharacteristicProperties.Notify) || props.HasFlag(GattCharacteristicProperties.Indicate)))
                        {
                            _notifyCharacteristic = ch;
                        }
                    }

                    if (_writeCharacteristic != null && _notifyCharacteristic != null)
                        break;
                }
            }

            if (_writeCharacteristic == null)
            {
                OnLog?.Invoke("[WinRT-BLE-WARN] 未找到具有写入权限的控制特征值");
                return false;
            }

            OnLog?.Invoke($"[WinRT-BLE-TARGET] 选定写入通道: {_writeCharacteristic.Uuid}");

            if (_notifyCharacteristic != null)
            {
                OnLog?.Invoke($"[WinRT-BLE-TARGET] 选定回传通道: {_notifyCharacteristic.Uuid}");
                var cccdResult = await _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
                
                if (cccdResult.Status == GattCommunicationStatus.Success)
                {
                    OnLog?.Invoke("[WinRT-BLE] 成功订阅耳机数据回传通知 (Notify Enabled)");
                }
                else
                {
                    OnLog?.Invoke($"[WinRT-BLE-WARN] 订阅回传通知状态: {cccdResult.Status}");
                }

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
        if (_writeCharacteristic == null)
        {
            OnLog?.Invoke("[WinRT-BLE-WRITE-ERR] 写入特征未就绪，无法发送指令");
            return false;
        }

        try
        {
            using var writer = new DataWriter();
            writer.WriteBytes(data);
            var buffer = writer.DetachBuffer();

            var props = _writeCharacteristic.CharacteristicProperties;
            GattWriteOption option = GattWriteOption.WriteWithResponse;

            if (writeWithoutResponse && props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
            {
                option = GattWriteOption.WriteWithoutResponse;
            }
            else if (!props.HasFlag(GattCharacteristicProperties.Write) && props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
            {
                option = GattWriteOption.WriteWithoutResponse;
            }

            var result = await _writeCharacteristic.WriteValueWithResultAsync(buffer, option);
            if (result.Status == GattCommunicationStatus.Success)
            {
                return true;
            }

            // 如果首次写入由于选项不匹配失败，尝试换一种写入模式重试一次
            var retryOption = (option == GattWriteOption.WriteWithResponse)
                ? GattWriteOption.WriteWithoutResponse
                : GattWriteOption.WriteWithResponse;

            var retryResult = await _writeCharacteristic.WriteValueWithResultAsync(buffer, retryOption);
            if (retryResult.Status == GattCommunicationStatus.Success)
            {
                return true;
            }

            OnLog?.Invoke($"[WinRT-BLE-WRITE-ERR] 写入特征值未成功: {retryResult.Status}, 错误码: {retryResult.ProtocolError}");
            return false;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"[WinRT-BLE-WRITE-ERR] 异常: {ex.Message}");
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
