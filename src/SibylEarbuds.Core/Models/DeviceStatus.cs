namespace SibylEarbuds.Core.Models;

public class DeviceStatus
{
    public string DeviceName { get; set; } = "SIBYL Earbuds";
    public string MacAddress { get; set; } = string.Empty;
    public bool IsConnected { get; set; } = false;

    // 电量 (0-100%, -1 表示未就绪或未入盒)
    public int LeftBattery { get; set; } = 85;
    public int RightBattery { get; set; } = 90;
    public int CaseBattery { get; set; } = -1;

    public bool IsLeftCharging { get; set; } = false;
    public bool IsRightCharging { get; set; } = false;
    public bool IsCaseCharging { get; set; } = false;

    // 状态
    public AncState Anc { get; set; } = new(AncModeType.NoiseReduction, AncDepthLevel.Deep);
    public EqConfiguration CurrentEq { get; set; } = new("经典 (默认)", 1, new int[10]);
    public bool IsGameModeEnabled { get; set; } = false;
    public bool IsLdacEnabled { get; set; } = false;
    public bool IsTouchDisabled { get; set; } = false;
    public bool IsSleepModeEnabled { get; set; } = false;
    public int TimedShutdownMinutes { get; set; } = 0; // 0 表示不自动关机
    public byte ToneVolume { get; set; } = 8;
    public LightEffectConfig LightEffect { get; set; } = new();
    public EarbudKeySettings KeySettings { get; set; } = new();

    public string FirmwareVersion { get; set; } = "V1.0.8";
    public int Rssi { get; set; } = -55;
}
