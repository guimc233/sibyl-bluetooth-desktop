using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using SibylEarbuds.App.Bluetooth;
using SibylEarbuds.App.Services;
using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;
using SibylEarbuds.Core.Services;
using SibylEarbuds.Core.Transport;
using Windows.UI;

namespace SibylEarbuds.App.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly DispatcherQueue _dispatcher;
    private readonly Dictionary<string, DiscoveredBleDevice> _deviceMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly AudioPlayerService _audioPlayer = new();

    private EarbudDeviceService _deviceService;
    private bool _isMockMode;
    private bool _suppressRemote;
    private bool _hasAttemptedAutoConnect;

    // Discovery
    private bool _isScanning;
    private DiscoveredBleDevice? _selectedDevice;
    private string _scanStatusText = "正在监听低功耗蓝牙广播...";

    // Shell / advanced drawer
    private bool _isAdvancedSettingsOpen;

    // Connection + battery
    private string _deviceName = "SIBYL Earbuds";
    private bool _isConnected;
    private int _leftBattery = -1;
    private int _rightBattery = -1;
    private int _caseBattery = -1;
    private bool _isLeftCharging;
    private bool _isRightCharging;
    private bool _isCaseCharging;
    private string _firmwareVersion = "--";

    // ANC
    private AncModeType _currentAncMode = AncModeType.NoiseReduction;
    private AncDepthLevel _currentAncDepth = AncDepthLevel.Deep;

    // Quick switches
    private bool _isGameMode;
    private bool _isTouchDisabled;
    private bool _isLdacEnabled;
    private bool _isFindingEarphone;
    private ShutdownOption _selectedShutdown = OptionCatalog.Shutdowns[0];

    // Light effect
    private LightModeType _lightMode = LightModeType.Breathing;
    private double _lightSpeed = 3;
    private double _lightBrightness = 80;
    private Color _lightColor = Color.FromArgb(255, 0, 120, 255);

    // EQ
    private ObservableCollection<EqConfiguration> _eqPresets;
    private EqConfiguration? _selectedEqPreset;
    private readonly int[] _eqGains = new int[EqConfiguration.BandCount];

    // Key mapping (sent only on explicit save)
    private readonly EarbudKeySettings _keySettings = new();
    private KeyFunctionOption _leftSingleTap;
    private KeyFunctionOption _leftDoubleTap;
    private KeyFunctionOption _leftTripleTap;
    private KeyFunctionOption _leftLongPress;
    private KeyFunctionOption _rightSingleTap;
    private KeyFunctionOption _rightDoubleTap;
    private KeyFunctionOption _rightTripleTap;
    private KeyFunctionOption _rightLongPress;

    // Sound
    private BuiltInSoundInfo _selectedSound;
    private bool _isSoundPlaying;
    private double _soundVolume = 0.6;
    private string _currentSoundName = "未播放";

    // Capability profile
    private DeviceCapability _currentProfile = DeviceModelProfiles.MatchProfileByName("PRO");

    public MainViewModel()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _deviceService = new EarbudDeviceService(TransportFactory.Create(_isMockMode));
        AttachServiceEvents();

        _eqPresets = new ObservableCollection<EqConfiguration>(EqConfiguration.GetDefaultPresets());
        _selectedEqPreset = _eqPresets[0];
        Array.Copy(_selectedEqPreset.Gains, _eqGains, _eqGains.Length);

        _selectedSound = BuiltInSounds[0];
        _leftSingleTap = OptionFor(_keySettings.LeftSingleTap);
        _leftDoubleTap = OptionFor(_keySettings.LeftDoubleTap);
        _leftTripleTap = OptionFor(_keySettings.LeftTripleTap);
        _leftLongPress = OptionFor(_keySettings.LeftLongPress);
        _rightSingleTap = OptionFor(_keySettings.RightSingleTap);
        _rightDoubleTap = OptionFor(_keySettings.RightDoubleTap);
        _rightTripleTap = OptionFor(_keySettings.RightTripleTap);
        _rightLongPress = OptionFor(_keySettings.RightLongPress);

        ScanCommand = new AsyncRelayCommand(StartScanAsync);
        ConnectCommand = new AsyncRelayCommand(param => ConnectAsync(param as DiscoveredBleDevice));
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync);
        BackToDiscoveryCommand = new AsyncRelayCommand(BackToDiscoveryAsync);
        ToggleAdvancedSettingsCommand = new RelayCommand(() => IsAdvancedSettingsOpen = !IsAdvancedSettingsOpen);
        ToggleFindEarphoneCommand = new RelayCommand(() => _ = ToggleFindEarphoneAsync());
        ResetDefaultsCommand = new AsyncRelayCommand(() => _deviceService.ResetSettingsAsync(false));
        FactoryResetCommand = new AsyncRelayCommand(() => _deviceService.ResetSettingsAsync(true));
        SaveKeySettingsCommand = new AsyncRelayCommand(() => _deviceService.SaveKeySettingsAsync(_keySettings));
        SwitchTransportCommand = new RelayCommand(ToggleTransportMode);
        PlaySoundCommand = new RelayCommand(param => PlaySound(param as BuiltInSoundInfo));
        StopSoundCommand = new RelayCommand(() => _audioPlayer.Stop());

        _audioPlayer.PlaybackStateChanged += (playing, sound) =>
            RunOnUi(() =>
            {
                IsSoundPlaying = playing;
                if (sound is not null)
                {
                    CurrentSoundName = sound.Name;
                }
            });

        AddLog("[SYS] SIBYL 耳机控制中枢 (WinUI 3 / Windows App SDK) 启动");
        AddLog("[AUDIO-GUARD] 控制流走独立 BLE GATT，A2DP 音频链路完全隔离");

        _ = StartScanAsync();
    }

    #region Events raised for shell navigation

    public event Action? NavigateToDashboardRequested;

    public event Action? NavigateToDiscoveryRequested;

    #endregion

    #region Collections

    public ObservableCollection<DiscoveredBleDevice> DiscoveredDevices { get; } = [];

    public ObservableCollection<string> LogLines { get; } = [];

    public ObservableCollection<BuiltInSoundInfo> BuiltInSounds { get; } = new(BuiltInSoundLibrary.Sounds);

    public IReadOnlyList<ShutdownOption> ShutdownOptions { get; } = OptionCatalog.Shutdowns;

    public IReadOnlyList<KeyFunctionOption> KeyFunctionOptions { get; } = OptionCatalog.KeyFunctions;

    public BuiltInSoundInfo SoundPink => BuiltInSounds[0];
    public BuiltInSoundInfo SoundBirds => BuiltInSounds[1];
    public BuiltInSoundInfo SoundRain => BuiltInSounds[2];

    #endregion

    #region Commands

    public ICommand ScanCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand BackToDiscoveryCommand { get; }
    public ICommand ToggleAdvancedSettingsCommand { get; }
    public ICommand ToggleFindEarphoneCommand { get; }
    public ICommand ResetDefaultsCommand { get; }
    public ICommand FactoryResetCommand { get; }
    public ICommand SaveKeySettingsCommand { get; }
    public ICommand SwitchTransportCommand { get; }
    public ICommand PlaySoundCommand { get; }
    public ICommand StopSoundCommand { get; }

    #endregion

    #region Discovery properties

    public bool IsScanning
    {
        get => _isScanning;
        set => SetProperty(ref _isScanning, value);
    }

    public string ScanStatusText
    {
        get => _scanStatusText;
        set => SetProperty(ref _scanStatusText, value);
    }

    public DiscoveredBleDevice? SelectedDevice
    {
        get => _selectedDevice;
        set => SetProperty(ref _selectedDevice, value);
    }

    public bool HasDevices => DiscoveredDevices.Count > 0;

    #endregion

    #region Shell / connection properties

    public bool IsAdvancedSettingsOpen
    {
        get => _isAdvancedSettingsOpen;
        set => SetProperty(ref _isAdvancedSettingsOpen, value);
    }

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(ConnectionStatusText));
            }
        }
    }

    public string ConnectionStatusText => IsConnected ? "已连接" : "未连接";

    public DeviceCapability CurrentProfile
    {
        get => _currentProfile;
        set
        {
            if (SetProperty(ref _currentProfile, value))
            {
                OnPropertyChanged(nameof(SupportsAncDepthNow));
            }
        }
    }

    public string TransportModeText => _isMockMode ? "虚拟设备模拟器模式" : "Windows WinRT BLE 真实模式";

    public string FirmwareVersion
    {
        get => _firmwareVersion;
        set => SetProperty(ref _firmwareVersion, value);
    }

    #endregion

    #region Battery properties

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
        set
        {
            if (SetProperty(ref _caseBattery, value))
            {
                OnPropertyChanged(nameof(HasCaseBattery));
            }
        }
    }

    public bool HasCaseBattery => _caseBattery >= 0;

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

    #endregion

    #region ANC properties

    public int AncModeIndex
    {
        get => (int)_currentAncMode - 1;
        set
        {
            int clamped = Math.Clamp(value, 0, 2);
            var mode = (AncModeType)(clamped + 1);
            if (_currentAncMode == mode)
            {
                return;
            }

            _currentAncMode = mode;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AncStatusText));
            OnPropertyChanged(nameof(SupportsAncDepthNow));

            if (!_suppressRemote)
            {
                _ = _deviceService.SetAncModeAsync(mode, _currentAncDepth);
            }
        }
    }

    public int AncDepthIndex
    {
        get => _currentAncDepth == AncDepthLevel.Comfortable ? 0 : 1;
        set
        {
            var depth = value == 0 ? AncDepthLevel.Comfortable : AncDepthLevel.Deep;
            if (_currentAncDepth == depth)
            {
                return;
            }

            _currentAncDepth = depth;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AncStatusText));

            if (!_suppressRemote && _currentAncMode == AncModeType.NoiseReduction)
            {
                _ = _deviceService.SetAncModeAsync(AncModeType.NoiseReduction, depth);
            }
        }
    }

    public bool SupportsAncDepthNow => CurrentProfile.SupportsAncDepth;

    public string AncStatusText => (_currentAncMode, _currentAncDepth) switch
    {
        (AncModeType.NoiseReduction, AncDepthLevel.Comfortable) => "当前：舒适降噪 (50%)",
        (AncModeType.NoiseReduction, _) => "当前：深度降噪 (100%)",
        (AncModeType.Normal, _) => "当前：普通模式",
        (AncModeType.Transparency, _) => "当前：通透模式",
        _ => "当前：未知"
    };

    #endregion

    #region Quick switch properties

    public bool IsGameMode
    {
        get => _isGameMode;
        set
        {
            if (SetProperty(ref _isGameMode, value) && !_suppressRemote)
            {
                _ = _deviceService.SetGameModeAsync(value);
            }
        }
    }

    public bool IsTouchDisabled
    {
        get => _isTouchDisabled;
        set
        {
            if (SetProperty(ref _isTouchDisabled, value) && !_suppressRemote)
            {
                _ = _deviceService.SetTouchLockAsync(value);
            }
        }
    }

    public bool IsLdacEnabled
    {
        get => _isLdacEnabled;
        set
        {
            if (SetProperty(ref _isLdacEnabled, value) && !_suppressRemote)
            {
                _ = _deviceService.SetHighResModeAsync(value);
            }
        }
    }

    public string FindEarphoneButtonText => _isFindingEarphone ? "停止定位音" : "播放定位音";

    public ShutdownOption SelectedShutdown
    {
        get => _selectedShutdown;
        set
        {
            if (SetProperty(ref _selectedShutdown, value) && !_suppressRemote && value is not null)
            {
                _ = _deviceService.SetTimedShutdownAsync(value.Minutes);
            }
        }
    }

    #endregion

    #region Light properties

    public int LightModeIndex
    {
        get => (int)_lightMode - 1;
        set
        {
            int clamped = Math.Clamp(value, 0, 2);
            var mode = (LightModeType)(clamped + 1);
            if (_lightMode == mode)
            {
                return;
            }

            _lightMode = mode;
            OnPropertyChanged();
            if (!_suppressRemote)
            {
                UpdateLightConfig();
            }
        }
    }

    public double LightSpeed
    {
        get => _lightSpeed;
        set
        {
            if (SetProperty(ref _lightSpeed, value) && !_suppressRemote)
            {
                UpdateLightConfig();
            }
        }
    }

    public double LightBrightness
    {
        get => _lightBrightness;
        set
        {
            if (SetProperty(ref _lightBrightness, value) && !_suppressRemote)
            {
                UpdateLightConfig();
            }
        }
    }

    public Color LightColor
    {
        get => _lightColor;
        set
        {
            if (SetProperty(ref _lightColor, value) && !_suppressRemote)
            {
                UpdateLightConfig();
            }
        }
    }

    #endregion

    #region EQ properties

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
            if (!SetProperty(ref _selectedEqPreset, value) || value is null)
            {
                return;
            }

            Array.Copy(value.Gains, _eqGains, _eqGains.Length);
            NotifyAllEqBands();

            if (!_suppressRemote)
            {
                _ = _deviceService.ApplyEqPresetAsync(value);
            }
        }
    }

    public double EqBand0 { get => _eqGains[0]; set => UpdateEqGain(0, value); }
    public double EqBand1 { get => _eqGains[1]; set => UpdateEqGain(1, value); }
    public double EqBand2 { get => _eqGains[2]; set => UpdateEqGain(2, value); }
    public double EqBand3 { get => _eqGains[3]; set => UpdateEqGain(3, value); }
    public double EqBand4 { get => _eqGains[4]; set => UpdateEqGain(4, value); }
    public double EqBand5 { get => _eqGains[5]; set => UpdateEqGain(5, value); }
    public double EqBand6 { get => _eqGains[6]; set => UpdateEqGain(6, value); }
    public double EqBand7 { get => _eqGains[7]; set => UpdateEqGain(7, value); }
    public double EqBand8 { get => _eqGains[8]; set => UpdateEqGain(8, value); }
    public double EqBand9 { get => _eqGains[9]; set => UpdateEqGain(9, value); }

    #endregion

    #region Key mapping properties

    public KeyFunctionOption LeftSingleTap
    {
        get => _leftSingleTap;
        set => SetKeyOption(ref _leftSingleTap, value, v => _keySettings.LeftSingleTap = v);
    }

    public KeyFunctionOption LeftDoubleTap
    {
        get => _leftDoubleTap;
        set => SetKeyOption(ref _leftDoubleTap, value, v => _keySettings.LeftDoubleTap = v);
    }

    public KeyFunctionOption LeftTripleTap
    {
        get => _leftTripleTap;
        set => SetKeyOption(ref _leftTripleTap, value, v => _keySettings.LeftTripleTap = v);
    }

    public KeyFunctionOption LeftLongPress
    {
        get => _leftLongPress;
        set => SetKeyOption(ref _leftLongPress, value, v => _keySettings.LeftLongPress = v);
    }

    public KeyFunctionOption RightSingleTap
    {
        get => _rightSingleTap;
        set => SetKeyOption(ref _rightSingleTap, value, v => _keySettings.RightSingleTap = v);
    }

    public KeyFunctionOption RightDoubleTap
    {
        get => _rightDoubleTap;
        set => SetKeyOption(ref _rightDoubleTap, value, v => _keySettings.RightDoubleTap = v);
    }

    public KeyFunctionOption RightTripleTap
    {
        get => _rightTripleTap;
        set => SetKeyOption(ref _rightTripleTap, value, v => _keySettings.RightTripleTap = v);
    }

    public KeyFunctionOption RightLongPress
    {
        get => _rightLongPress;
        set => SetKeyOption(ref _rightLongPress, value, v => _keySettings.RightLongPress = v);
    }

    #endregion

    #region Sound properties

    public BuiltInSoundInfo SelectedSound
    {
        get => _selectedSound;
        set => SetProperty(ref _selectedSound, value);
    }

    public bool IsSoundPlaying
    {
        get => _isSoundPlaying;
        set => SetProperty(ref _isSoundPlaying, value);
    }

    public string CurrentSoundName
    {
        get => _currentSoundName;
        private set => SetProperty(ref _currentSoundName, value);
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

    #endregion

    #region Methods

    public async Task StartScanAsync()
    {
        IsScanning = true;
        ScanStatusText = "正在持续发现周边蓝牙音频设备...";
        AddLog("[BLE-SCAN] 启动低功耗蓝牙持续扫描");
        await _deviceService.Transport.StartContinuousScanAsync();
    }

    public async Task ConnectAsync(DiscoveredBleDevice? target)
    {
        var dev = target ?? SelectedDevice ?? DiscoveredDevices.FirstOrDefault();
        if (dev is null)
        {
            AddLog("[BLE-WARN] 请先选择一个耳机设备");
            return;
        }

        AddLog($"[BLE-CONNECT] 正在连接: {dev.Name} ({dev.Id})...");
        bool success = await _deviceService.ConnectAsync(dev.Id);
        if (!success)
        {
            return;
        }

        AddLog($"[BLE-SUCCESS] 连接成功: {dev.Name}");

        // 根据官方广播 VendorId 自动选择机型适配 (不再支持手动覆盖)
        if (dev.VendorId > 0)
        {
            string model = SibylDeviceMatcher.ResolveModelName(dev.VendorId);
            if (!string.IsNullOrEmpty(dev.ModelName))
            {
                model = dev.ModelName;
            }

            CurrentProfile = DeviceModelProfiles.MatchProfileByName(model);
            AddLog($"[MODEL] 自动机型适配: {model} (VendorId: 0x{dev.VendorId:X4})");
        }

        try
        {
            var settings = SettingsStorageService.Load();
            settings.LastConnectedDeviceId = dev.Id;
            settings.LastConnectedDeviceName = dev.Name;
            SettingsStorageService.Save(settings);
            AddLog("[CONFIG] 已记住设备，下次启动将自动尝试重连");
        }
        catch
        {
            // Non-fatal: memory of last device is a convenience only.
        }

        NavigateToDashboardRequested?.Invoke();
    }

    public async Task DisconnectAsync()
    {
        await _deviceService.DisconnectAsync();
        await BackToDiscoveryAsync();
    }

    private async Task BackToDiscoveryAsync()
    {
        NavigateToDiscoveryRequested?.Invoke();
        await StartScanAsync();
    }

    private async Task ToggleFindEarphoneAsync()
    {
        bool next = !_isFindingEarphone;
        _isFindingEarphone = next;
        OnPropertyChanged(nameof(FindEarphoneButtonText));
        await _deviceService.FindEarphonesAsync(next);
    }

    private void ToggleTransportMode()
    {
        _deviceService.Dispose();
        _isMockMode = !_isMockMode;
        _deviceService = new EarbudDeviceService(TransportFactory.Create(_isMockMode));
        AttachServiceEvents();

        _deviceMap.Clear();
        DiscoveredDevices.Clear();
        OnPropertyChanged(nameof(TransportModeText));
        OnPropertyChanged(nameof(HasDevices));
        AddLog($"[MODE] 已切换为 {TransportModeText}");
        _ = StartScanAsync();
    }

    private void PlaySound(BuiltInSoundInfo? sound)
    {
        var target = sound ?? SelectedSound;
        if (target is null)
        {
            return;
        }

        SelectedSound = target;
        _audioPlayer.Play(target, SoundVolume);
        AddLog($"[SOUND] 播放内置音效: {target.Name}");
    }

    private void UpdateEqGain(int index, double value)
    {
        int gain = (int)Math.Round(value);
        if (_eqGains[index] == gain)
        {
            return;
        }

        _eqGains[index] = gain;
        OnPropertyChanged($"EqBand{index}");
        _deviceService.SetEqGains(_eqGains);
    }

    private void NotifyAllEqBands()
    {
        for (int i = 0; i < EqConfiguration.BandCount; i++)
        {
            OnPropertyChanged($"EqBand{i}");
        }
    }

    private void UpdateLightConfig()
    {
        var config = new LightEffectConfig
        {
            Mode = _lightMode,
            Speed = (byte)Math.Clamp((int)Math.Round(_lightSpeed), 0, 7),
            Brightness = (byte)Math.Clamp((int)Math.Round(_lightBrightness), 0, 100),
            Red = _lightColor.R,
            Green = _lightColor.G,
            Blue = _lightColor.B
        };
        _deviceService.SetLightEffect(config);
    }

    private void SetKeyOption(
        ref KeyFunctionOption field,
        KeyFunctionOption value,
        Action<KeyFunctionType> apply,
        [CallerMemberName] string? propertyName = null)
    {
        if (value is null || ReferenceEquals(field, value))
        {
            return;
        }

        field = value;
        apply(value.Value);
        OnPropertyChanged(propertyName);
    }

    private static KeyFunctionOption OptionFor(KeyFunctionType value)
        => OptionCatalog.KeyFunctions.FirstOrDefault(o => o.Value == value) ?? OptionCatalog.KeyFunctions[0];

    private void AttachServiceEvents()
    {
        _deviceService.StatusUpdated += status => RunOnUi(() => ApplyStatus(status));

        _deviceService.Transport.OnDeviceFound += dev => RunOnUi(() => OnDeviceFound(dev));

        _deviceService.LogMessage += msg => RunOnUi(() => AddLog(msg));
    }

    private void ApplyStatus(DeviceStatus status)
    {
        _suppressRemote = true;
        try
        {
            IsConnected = status.IsConnected;
            DeviceName = status.DeviceName;
            LeftBattery = status.LeftBattery;
            RightBattery = status.RightBattery;
            CaseBattery = status.CaseBattery;
            IsLeftCharging = status.IsLeftCharging;
            IsRightCharging = status.IsRightCharging;
            IsCaseCharging = status.IsCaseCharging;

            _currentAncMode = status.Anc.Mode;
            _currentAncDepth = status.Anc.Depth;
            OnPropertyChanged(nameof(AncModeIndex));
            OnPropertyChanged(nameof(AncDepthIndex));
            OnPropertyChanged(nameof(AncStatusText));
            OnPropertyChanged(nameof(SupportsAncDepthNow));

            IsGameMode = status.IsGameModeEnabled;
            IsTouchDisabled = status.IsTouchDisabled;
            IsLdacEnabled = status.IsLdacEnabled;
            FirmwareVersion = status.FirmwareVersion;
        }
        finally
        {
            _suppressRemote = false;
        }
    }

    private void OnDeviceFound(DiscoveredBleDevice dev)
    {
        if (_deviceMap.TryGetValue(dev.Id, out _))
        {
            for (int i = 0; i < DiscoveredDevices.Count; i++)
            {
                if (DiscoveredDevices[i].Id == dev.Id)
                {
                    DiscoveredDevices[i] = dev;
                    return;
                }
            }

            return;
        }

        _deviceMap[dev.Id] = dev;
        DiscoveredDevices.Add(dev);
        SelectedDevice ??= dev;
        OnPropertyChanged(nameof(HasDevices));

        if (_hasAttemptedAutoConnect || IsConnected)
        {
            return;
        }

        var settings = SettingsStorageService.Load();
        if (!settings.AutoReconnect || string.IsNullOrEmpty(settings.LastConnectedDeviceId))
        {
            return;
        }

        bool matches = string.Equals(dev.Id, settings.LastConnectedDeviceId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(dev.Name, settings.LastConnectedDeviceName, StringComparison.OrdinalIgnoreCase);

        if (matches)
        {
            _hasAttemptedAutoConnect = true;
            AddLog($"[AUTO-CONNECT] 检测到记忆设备 {dev.Name}，自动重连中...");
            _ = ConnectAsync(dev);
        }
    }

    private void AddLog(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogLines.Insert(0, line);
        while (LogLines.Count > 200)
        {
            LogLines.RemoveAt(LogLines.Count - 1);
        }
    }

    private void RunOnUi(Action action)
    {
        if (_dispatcher.HasThreadAccess)
        {
            action();
        }
        else
        {
            _dispatcher.TryEnqueue(() => action());
        }
    }

    public void Dispose()
    {
        _audioPlayer.Dispose();
        _deviceService.Dispose();
    }

    #endregion
}
