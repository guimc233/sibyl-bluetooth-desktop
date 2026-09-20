using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SibylEarbuds.App.Bluetooth;
using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;
using SibylEarbuds.Core.Services;
using SibylEarbuds.Core.Transport;

namespace SibylEarbuds.App.ViewModels;

public enum AppNavigationPage
{
    DeviceDiscovery,  // 页面 1：周边设备发现与连接页
    DeviceDashboard   // 页面 2：耳机状态与深度控制中心页
}

public class MainViewModel : ViewModelBase, IDisposable
{
    private EarbudDeviceService _deviceService;
    private bool _isMockMode = true;
    private AppNavigationPage _currentPage = AppNavigationPage.DeviceDiscovery;

    // 扫描与发现设备
    private bool _isScanning = true;
    private DiscoveredBleDevice? _selectedDevice;
    private readonly Dictionary<string, DiscoveredBleDevice> _deviceMap = new(StringComparer.OrdinalIgnoreCase);

    // 隐蔽高级设置抽屉
    private bool _isAdvancedSettingsOpen = false;

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

    // 自带音频播放与型号能力映射
    private readonly Services.AudioPlayerService _audioPlayer = new();
    private DeviceCapability _currentProfile = DeviceModelProfiles.MatchProfileByName("S1");
    private BuiltInSoundInfo? _selectedSound;
    private bool _isSoundPlaying;
    private double _soundVolume = 0.6;

    // 列表集合
    public ObservableCollection<DiscoveredBleDevice> DiscoveredDevices { get; } = [];
    public ObservableCollection<string> LogLines { get; } = [];
    public ObservableCollection<DeviceCapability> AllModelProfiles { get; } = new(DeviceModelProfiles.GetAllProfiles());
    public ObservableCollection<BuiltInSoundInfo> BuiltInSounds { get; } = new(BuiltInSoundLibrary.Sounds);

    // Commands
    public ICommand ScanCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand BackToDiscoveryCommand { get; }
    public ICommand ToggleAdvancedSettingsCommand { get; }

    public ICommand SetAncCommand { get; }
    public ICommand ToggleGameModeCommand { get; }
    public ICommand ToggleTouchLockCommand { get; }
    public ICommand ToggleFindEarphoneCommand { get; }
    public ICommand ResetDefaultsCommand { get; }
    public ICommand FactoryResetCommand { get; }
    public ICommand SaveKeySettingsCommand { get; }
    public ICommand ApplyEqPresetCommand { get; }
    public ICommand SwitchTransportCommand { get; }
    public ICommand PlaySoundCommand { get; }
    public ICommand StopSoundCommand { get; }

    public MainViewModel()
    {
        _deviceService = new EarbudDeviceService(TransportFactory.Create(_isMockMode));
        AttachServiceEvents();

        _eqPresets = new ObservableCollection<EqConfiguration>(EqConfiguration.GetDefaultPresets());
        _selectedEqPreset = _eqPresets.First();
        _eqGains = (int[])_selectedEqPreset.Gains.Clone();

        ScanCommand = new AsyncRelayCommand(StartScanAsync);
        ConnectCommand = new AsyncRelayCommand(param => ConnectAsync(param as DiscoveredBleDevice));
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        BackToDiscoveryCommand = new AsyncRelayCommand(async () =>
        {
            CurrentPage = AppNavigationPage.DeviceDiscovery;
            await StartScanAsync();
        });
        ToggleAdvancedSettingsCommand = new RelayCommand(() => IsAdvancedSettingsOpen = !IsAdvancedSettingsOpen);

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

        _selectedSound = BuiltInSounds.FirstOrDefault();
        PlaySoundCommand = new RelayCommand(param =>
        {
            if (param is BuiltInSoundInfo sound)
            {
                SelectedSound = sound;
                _audioPlayer.Play(sound, SoundVolume);
                AddLog($"[SOUND] 播放自带音效: {sound.Name}");
            }
            else if (SelectedSound != null)
            {
                _audioPlayer.Play(SelectedSound, SoundVolume);
                AddLog($"[SOUND] 播放自带音效: {SelectedSound.Name}");
            }
        });
        StopSoundCommand = new RelayCommand(() =>
        {
            _audioPlayer.Stop();
            AddLog("[SOUND] 停止音效播放");
        });

        _audioPlayer.PlaybackStateChanged += (playing, sound) =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsSoundPlaying = playing;
            });
        };

        AddLog("[SYS] SIBYL 耳机控制中枢 (Windows 11 UWP Style) 启动");
        AddLog("[AUDIO-GUARD] 物理音频隔离生效：Windows A2DP 音频纯净保留，控制走独立 BLE");

        // 默认进入发现页并立刻启动持续扫描
        _ = StartScanAsync();
    }

    #region Properties

    public AppNavigationPage CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public bool IsAdvancedSettingsOpen
    {
        get => _isAdvancedSettingsOpen;
        set => SetProperty(ref _isAdvancedSettingsOpen, value);
    }

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

    public DeviceCapability CurrentProfile
    {
        get => _currentProfile;
        set => SetProperty(ref _currentProfile, value);
    }

    public BuiltInSoundInfo? SelectedSound
    {
        get => _selectedSound;
        set => SetProperty(ref _selectedSound, value);
    }

    public bool IsSoundPlaying
    {
        get => _isSoundPlaying;
        set => SetProperty(ref _isSoundPlaying, value);
    }

    public double SoundVolume
    {
        get => _soundVolume;
        set
        {
            if (SetProperty(ref _soundVolume, value))
            {
                _audioPlayer.SetVolume(value);
            }
        }
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

    public async Task StartScanAsync()
    {
        IsScanning = true;
        AddLog("[BLE-SCAN] 正在持续发现周边蓝牙音频设备...");
        await _deviceService.Transport.StartContinuousScanAsync();
    }

    public async Task ConnectAsync(DiscoveredBleDevice? target = null)
    {
        var dev = target ?? SelectedDevice ?? DiscoveredDevices.FirstOrDefault();
        if (dev == null)
        {
            AddLog("[BLE-WARN] 请在列表中选择一个耳机设备");
            return;
        }

        AddLog($"[BLE-CONNECT] 正在连接设备: {dev.Name} ({dev.Id})...");
        bool success = await _deviceService.ConnectAsync(dev.Id);
        if (success)
        {
            AddLog($"[BLE-SUCCESS] 连接成功: {dev.Name}，自动切入控制详情页");
            CurrentPage = AppNavigationPage.DeviceDashboard;
        }
    }

    public async Task DisconnectAsync()
    {
        await _deviceService.DisconnectAsync();
        CurrentPage = AppNavigationPage.DeviceDiscovery;
        await StartScanAsync();
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
        _deviceMap.Clear();
        DiscoveredDevices.Clear();
        AddLog($"[MODE] 已切换到: {(IsMockMode ? "虚拟蓝牙演示模式" : "Windows 10/11 WinRT BLE 真实模式")}");
        _ = StartScanAsync();
    }

    private void AttachServiceEvents()
    {
        _deviceService.StatusUpdated += status =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsConnected = status.IsConnected;
                DeviceName = status.DeviceName;
                CurrentProfile = DeviceModelProfiles.MatchProfileByName(status.DeviceName);
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

        _deviceService.Transport.OnDeviceFound += dev =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (!_deviceMap.ContainsKey(dev.Id))
                {
                    _deviceMap[dev.Id] = dev;
                    DiscoveredDevices.Add(dev);
                    if (SelectedDevice == null) SelectedDevice = dev;
                }
                else
                {
                    // 更新 RSSI 与信号
                    int index = -1;
                    for (int i = 0; i < DiscoveredDevices.Count; i++)
                    {
                        if (DiscoveredDevices[i].Id == dev.Id)
                        {
                            index = i;
                            break;
                        }
                    }
                    if (index >= 0)
                    {
                        DiscoveredDevices[index] = dev;
                    }
                }
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
        _audioPlayer.Dispose();
        _deviceService.Dispose();
    }

    #endregion
}
