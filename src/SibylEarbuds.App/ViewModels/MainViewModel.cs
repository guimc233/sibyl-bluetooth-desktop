using System.Collections.ObjectModel;
using System.Windows;
using SibylEarbuds.App.Bluetooth;
using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;
using SibylEarbuds.Core.Services;
using SibylEarbuds.Core.Transport;

namespace SibylEarbuds.App.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private EarbudDeviceService _deviceService;
    private bool _isMockMode = true; // 默认开启以保障开箱即用，可在UI一键切真实BLE
    private bool _isScanning;
    private DiscoveredBleDevice? _selectedDevice;

    // 电池与设备状态
    private string _deviceName = "SIBYL Earbuds";
    private bool _isConnected;
    private int _leftBattery = 85;
    private int _rightBattery = 90;
    private int _caseBattery = 100;
    private bool _isLeftCharging;
    private bool _isRightCharging;
    private bool _isCaseCharging;
    private string _firmwareVersion = "V1.0.8";

    // ANC
    private AncModeType _currentAncMode = AncModeType.NoiseReduction;
    private AncDepthLevel _currentAncDepth = AncDepthLevel.Deep;

    // 游戏模式与触控
    private bool _isGameMode;
    private bool _isTouchDisabled;
    private bool _isSleepMode;
    private bool _isFindingEarphone;
    private int _selectedShutdownMinutes = 0;

    // 灯效
    private LightModeType _lightMode = LightModeType.Breathing;
    private byte _lightSpeed = 3;
    private byte _lightBrightness = 80;
    private string _lightColorHex = "#0078D4";

    // EQ
    private ObservableCollection<EqConfiguration> _eqPresets = [];
    private EqConfiguration? _selectedEqPreset;
    private int[] _eqGains = new int[10];

    // 按键映射
    private EarbudKeySettings _keySettings = new();

    // 扫描列表
    public ObservableCollection<DiscoveredBleDevice> DiscoveredDevices { get; } = [];
    public ObservableCollection<string> LogLines { get; } = [];

    // Commands
    public ICommand ScanCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SetAncCommand { get; }
    public ICommand ToggleGameModeCommand { get; }
    public ICommand ToggleTouchLockCommand { get; }
    public ICommand ToggleFindEarphoneCommand { get; }
    public ICommand ResetDefaultsCommand { get; }
    public ICommand FactoryResetCommand { get; }
    public ICommand SaveKeySettingsCommand { get; }
    public ICommand ApplyEqPresetCommand { get; }
    public ICommand SwitchTransportCommand { get; }

    public MainViewModel()
    {
        _deviceService = new EarbudDeviceService(TransportFactory.Create(_isMockMode));
        AttachServiceEvents();

        // 初始化 EQ 预设
        _eqPresets = new ObservableCollection<EqConfiguration>(EqConfiguration.GetDefaultPresets());
        _selectedEqPreset = _eqPresets.First();
        _eqGains = (int[])_selectedEqPreset.Gains.Clone();

        // 命令绑定
        ScanCommand = new AsyncRelayCommand(ScanAsync);
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        SetAncCommand = new AsyncRelayCommand(async param =>
        {
            if (param is string str && int.TryParse(str, out int modeInt))
            {
                await SetAncAsync((AncModeType)modeInt);
            }
        });
        ToggleGameModeCommand = new AsyncRelayCommand(ToggleGameModeAsync);
        ToggleTouchLockCommand = new AsyncRelayCommand(ToggleTouchLockAsync);
        ToggleFindEarphoneCommand = new AsyncRelayCommand(ToggleFindEarphoneAsync);
        ResetDefaultsCommand = new AsyncRelayCommand(async () => await _deviceService.ResetSettingsAsync(false));
        FactoryResetCommand = new AsyncRelayCommand(async () => await _deviceService.ResetSettingsAsync(true));
        SaveKeySettingsCommand = new AsyncRelayCommand(async () => await _deviceService.SaveKeySettingsAsync(KeySettings));
        ApplyEqPresetCommand = new RelayCommand(param =>
        {
            if (param is EqConfiguration preset)
            {
                SelectedEqPreset = preset;
            }
        });
        SwitchTransportCommand = new RelayCommand(ToggleMockMode);

        AddLog("[SYS] SIBYL 耳机控制中枢就绪 (Windows 10/11 优化版)");
        AddLog("[AUDIO-GUARD] 物理级隔离已生效：绝不触碰 A2DP 音频流，指令通道走纯 BLE GATT");

        // 如果是模拟模式，默认自动连接供即刻交互
        if (_isMockMode)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(300);
                await ConnectAsync();
            });
        }
    }

    #region Properties

    public bool IsMockMode
    {
        get => _isMockMode;
        set => SetProperty(ref _isMockMode, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public DiscoveredBleDevice? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public int LeftBattery
    {
        get => _leftBattery;
        set => SetProperty(ref _leftBattery, value);
    }

    public int RightBattery
    {
        get => _rightBattery;
        set => SetProperty(ref _rightBattery, value);
    }

    public int CaseBattery
    {
        get => _caseBattery;
        set => SetProperty(ref _caseBattery, value);
    }

    public bool IsLeftCharging
    {
        get => _isLeftCharging;
        set => SetProperty(ref _isLeftCharging, value);
    }

    public bool IsRightCharging
    {
        get => _isRightCharging;
        set => SetProperty(ref _isRightCharging, value);
    }

    public bool IsCaseCharging
    {
        get => _isCaseCharging;
        set => SetProperty(ref _isCaseCharging, value);
    }

    public string FirmwareVersion
    {
        get => _firmwareVersion;
        set => SetProperty(ref _firmwareVersion, value);
    }

    public AncModeType CurrentAncMode
    {
        get => _currentAncMode;
        set => SetProperty(ref _currentAncMode, value);
    }

    public AncDepthLevel CurrentAncDepth
    {
        get => _currentAncDepth;
        set => SetProperty(ref _currentAncDepth, value);
    }

    public bool IsGameMode
    {
        get => _isGameMode;
        set => SetProperty(ref _isGameMode, value);
    }

    public bool IsTouchDisabled
    {
        get => _isTouchDisabled;
        set => SetProperty(ref _isTouchDisabled, value);
    }

    public bool IsSleepMode
    {
        get => _isSleepMode;
        set => SetProperty(ref _isSleepMode, value);
    }

    public bool IsFindingEarphone
    {
        get => _isFindingEarphone;
        set => SetProperty(ref _isFindingEarphone, value);
    }

    public int SelectedShutdownMinutes
    {
        get => _selectedShutdownMinutes;
        set
        {
            if (SetProperty(ref _selectedShutdownMinutes, value))
            {
                _ = _deviceService.SetTimedShutdownAsync(value);
            }
        }
    }

    public LightModeType LightMode
    {
        get => _lightMode;
        set
        {
            if (SetProperty(ref _lightMode, value))
            {
                UpdateLightConfig();
            }
        }
    }

    public byte LightSpeed
    {
        get => _lightSpeed;
        set
        {
            if (SetProperty(ref _lightSpeed, value))
            {
                UpdateLightConfig();
            }
        }
    }

    public byte LightBrightness
    {
        get => _lightBrightness;
        set
        {
            if (SetProperty(ref _lightBrightness, value))
            {
                UpdateLightConfig();
            }
        }
    }

    public string LightColorHex
    {
        get => _lightColorHex;
        set => SetProperty(ref _lightColorHex, value);
    }

    public ObservableCollection<EqConfiguration> EqPresets
    {
        get => _eqPresets;
        set => SetProperty(ref _eqPresets, value);
    }

    public EqConfiguration? SelectedEqPreset
    {
        get => _selectedEqPreset;
        set
        {
            if (SetProperty(ref _selectedEqPreset, value) && value != null)
            {
                _eqGains = (int[])value.Gains.Clone();
                NotifyAllEqBands();
                _ = _deviceService.ApplyEqPresetAsync(value);
            }
        }
    }

    public EarbudKeySettings KeySettings
    {
        get => _keySettings;
        set => SetProperty(ref _keySettings, value);
    }

    // 10段 EQ 单独绑定属性
    public int EqBand0 { get => _eqGains[0]; set => UpdateEqGain(0, value); }
    public int EqBand1 { get => _eqGains[1]; set => UpdateEqGain(1, value); }
    public int EqBand2 { get => _eqGains[2]; set => UpdateEqGain(2, value); }
    public int EqBand3 { get => _eqGains[3]; set => UpdateEqGain(3, value); }
    public int EqBand4 { get => _eqGains[4]; set => UpdateEqGain(4, value); }
    public int EqBand5 { get => _eqGains[5]; set => UpdateEqGain(5, value); }
    public int EqBand6 { get => _eqGains[6]; set => UpdateEqGain(6, value); }
    public int EqBand7 { get => _eqGains[7]; set => UpdateEqGain(7, value); }
    public int EqBand8 { get => _eqGains[8]; set => UpdateEqGain(8, value); }
    public int EqBand9 { get => _eqGains[9]; set => UpdateEqGain(9, value); }

    #endregion

    #region Methods

    private void UpdateEqGain(int index, int gain)
    {
        if (_eqGains[index] != gain)
        {
            _eqGains[index] = gain;
            OnPropertyChanged($"EqBand{index}");
            // 防抖限流发送到耳机
            _deviceService.SetEqGains(_eqGains);
        }
    }

    private void NotifyAllEqBands()
    {
        for (int i = 0; i < 10; i++)
        {
            OnPropertyChanged($"EqBand{i}");
        }
    }

    private void UpdateLightConfig()
    {
        var config = new LightEffectConfig
        {
            Mode = LightMode,
            Speed = LightSpeed,
            Brightness = LightBrightness,
            Red = 0,
            Green = 120,
            Blue = 255
        };
        _deviceService.SetLightEffect(config);
    }

    public async Task ScanAsync()
    {
        IsScanning = true;
        DiscoveredDevices.Clear();
        AddLog("[BLE-SCAN] 正在搜索周边的 SIBYL 及兼容蓝牙耳机...");
        try
        {
            var list = await _deviceService.ScanDevicesAsync();
            foreach (var dev in list)
            {
                DiscoveredDevices.Add(dev);
            }
            if (DiscoveredDevices.Count > 0)
            {
                SelectedDevice = DiscoveredDevices[0];
            }
        }
        finally
        {
            IsScanning = false;
        }
    }

    public async Task ConnectAsync()
    {
        string targetId = SelectedDevice?.Id ?? "MOCK-DEV-01";
        AddLog($"[BLE-CONNECT] 准备连接设备: {targetId}...");
        bool success = await _deviceService.ConnectAsync(targetId);
        if (success)
        {
            AddLog($"[BLE-SUCCESS] 连接成功: {_deviceService.CurrentStatus.DeviceName}");
        }
    }

    public async Task DisconnectAsync()
    {
        await _deviceService.DisconnectAsync();
    }

    public async Task SetAncAsync(AncModeType mode)
    {
        CurrentAncMode = mode;
        await _deviceService.SetAncModeAsync(mode, CurrentAncDepth);
    }

    public async Task ToggleGameModeAsync()
    {
        bool nextState = !IsGameMode;
        IsGameMode = nextState;
        await _deviceService.SetGameModeAsync(nextState);
    }

    public async Task ToggleTouchLockAsync()
    {
        bool nextState = !IsTouchDisabled;
        IsTouchDisabled = nextState;
        await _deviceService.SetTouchLockAsync(nextState);
    }

    public async Task ToggleFindEarphoneAsync()
    {
        bool nextState = !IsFindingEarphone;
        IsFindingEarphone = nextState;
        await _deviceService.FindEarphonesAsync(nextState);
    }

    private void ToggleMockMode()
    {
        _deviceService.Dispose();
        IsMockMode = !IsMockMode;
        _deviceService = new EarbudDeviceService(TransportFactory.Create(IsMockMode));
        AttachServiceEvents();
        AddLog($"[MODE] 已切换到: {(IsMockMode ? "虚拟耳机演示模式" : "Windows 10/11 真实 WinRT BLE 模式")}");
    }

    private void AttachServiceEvents()
    {
        _deviceService.StatusUpdated += status =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsConnected = status.IsConnected;
                DeviceName = status.DeviceName;
                LeftBattery = status.LeftBattery;
                RightBattery = status.RightBattery;
                CaseBattery = status.CaseBattery;
                IsLeftCharging = status.IsLeftCharging;
                IsRightCharging = status.IsRightCharging;
                IsCaseCharging = status.IsCaseCharging;
                CurrentAncMode = status.Anc.Mode;
                CurrentAncDepth = status.Anc.Depth;
                IsGameMode = status.IsGameModeEnabled;
                IsTouchDisabled = status.IsTouchDisabled;
                FirmwareVersion = status.FirmwareVersion;
            });
        };

        _deviceService.LogMessage += AddLog;
    }

    private void AddLog(string msg)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            LogLines.Insert(0, line);
            if (LogLines.Count > 100) LogLines.RemoveAt(LogLines.Count - 1);
        });
    }

    public void Dispose()
    {
        _deviceService.Dispose();
    }

    #endregion
}
