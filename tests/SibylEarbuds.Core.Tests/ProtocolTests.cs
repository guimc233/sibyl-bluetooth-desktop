using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;
using SibylEarbuds.Core.Services;
using SibylEarbuds.Core.Transport;
using Xunit;

namespace SibylEarbuds.Core.Tests;

public class ProtocolTests
{
    [Fact]
    public void Test_BuildAndParse_AncPacket()
    {
        var packet = PacketBuilder.BuildAncPacket(AncModeType.Transparency, AncDepthLevel.Comfortable);

        Assert.Equal(PacketBuilder.StartByte, packet[0]); // 0xFF
        Assert.Equal(3, packet[2]); // Len = 1(cmd) + 2(payload) = 3
        Assert.Equal((byte)SibylCommandId.AncMode, packet[3]); // 9
        Assert.Equal(PacketBuilder.EndByte, packet[^1]); // 0xAA

        var parsed = PacketParser.Parse(packet);
        Assert.True(parsed.IsSuccess);
        Assert.Equal(SibylCommandId.AncMode, parsed.CommandId);
        Assert.Equal(2, parsed.Payload.Length);
        Assert.Equal((byte)AncModeType.Transparency, parsed.Payload[0]);
        Assert.Equal((byte)AncDepthLevel.Comfortable, parsed.Payload[1]);

        var status = new DeviceStatus();
        bool applied = PacketParser.ApplyToStatus(parsed, status);
        Assert.True(applied);
        Assert.Equal(AncModeType.Transparency, status.Anc.Mode);
        Assert.Equal(AncDepthLevel.Comfortable, status.Anc.Depth);
    }

    [Fact]
    public void Test_BuildAndParse_EqPacket()
    {
        int[] originalGains = [-6, 5, -3, -2, 5, 4, -4, -3, 6, 4];
        var packet = PacketBuilder.BuildEqPacket(originalGains, 2);

        var parsed = PacketParser.Parse(packet);
        Assert.True(parsed.IsSuccess);
        Assert.Equal(SibylCommandId.Equalizer, parsed.CommandId);
        Assert.Equal(11, parsed.Payload.Length); // 1(type) + 10(bands)

        var status = new DeviceStatus();
        bool applied = PacketParser.ApplyToStatus(parsed, status);
        Assert.True(applied);
        Assert.Equal(originalGains, status.CurrentEq.Gains);
    }

    [Fact]
    public void Test_BuildAndParse_GameModePacket()
    {
        var packet = PacketBuilder.BuildGameModePacket(true);
        var parsed = PacketParser.Parse(packet);

        Assert.True(parsed.IsSuccess);
        Assert.Equal(SibylCommandId.GameMode, parsed.CommandId);
        Assert.Single(parsed.Payload);
        Assert.Equal(1, parsed.Payload[0]);

        var status = new DeviceStatus { IsGameModeEnabled = false };
        PacketParser.ApplyToStatus(parsed, status);
        Assert.True(status.IsGameModeEnabled);
    }

    [Fact]
    public void Test_PacketValidation_RejectsCorruptedPacket()
    {
        var packet = PacketBuilder.BuildAncPacket(AncModeType.NoiseReduction);
        packet[^1] = 0x00; // 破坏 0xAA 尾字节

        var parsed = PacketParser.Parse(packet);
        Assert.False(parsed.IsSuccess);
        Assert.Contains("Invalid 0xAA", parsed.ErrorMessage);
    }

    [Fact]
    public async Task Test_MockTransport_FullLoop()
    {
        using var mock = new MockBleTransport();
        using var service = new EarbudDeviceService(mock);

        var devices = await service.ScanDevicesAsync();
        Assert.NotEmpty(devices);

        bool connected = await service.ConnectAsync(devices[0].Id);
        Assert.True(connected);
        Assert.True(service.CurrentStatus.IsConnected);

        // 切换 ANC
        bool ancResult = await service.SetAncModeAsync(AncModeType.NoiseReduction, AncDepthLevel.Deep);
        Assert.True(ancResult);
        Assert.Equal(AncModeType.NoiseReduction, service.CurrentStatus.Anc.Mode);

        // 应用摇滚 EQ
        var rock = EqConfiguration.GetDefaultPresets().First(p => p.Name.Contains("摇滚"));
        bool eqResult = await service.ApplyEqPresetAsync(rock);
        Assert.True(eqResult);
        Assert.Equal(rock.Gains, service.CurrentStatus.CurrentEq.Gains);

        // 切换游戏模式
        bool gameResult = await service.SetGameModeAsync(true);
        Assert.True(gameResult);
        Assert.True(service.CurrentStatus.IsGameModeEnabled);

        await service.DisconnectAsync();
        Assert.False(service.CurrentStatus.IsConnected);
    }

    [Fact]
    public void Test_DeviceModelProfiles_MappingAndMatching()
    {
        var profiles = DeviceModelProfiles.GetAllProfiles();
        Assert.NotEmpty(profiles);

        // 验证 S1 支持全套功能
        var s1 = DeviceModelProfiles.MatchProfileByName("SIBYL S1 ANC");
        Assert.Equal("S1", s1.ModelId);
        Assert.True(s1.SupportsAnc);
        Assert.True(s1.SupportsTimedShutdown);
        Assert.True(s1.SupportsSleepMode);

        // 验证 B6 独有 RGB 炫彩灯效
        var b6 = DeviceModelProfiles.MatchProfileByName("SIBYL B6 Cyber");
        Assert.Equal("B6", b6.ModelId);
        Assert.True(b6.SupportsLightEffect);

        // 验证 S10 半入耳无主动降噪
        var s10 = DeviceModelProfiles.MatchProfileByName("SIBYL S10");
        Assert.Equal("S10", s10.ModelId);
        Assert.False(s10.SupportsAnc);

        // 验证 B8 支持体感控制
        var b8 = DeviceModelProfiles.MatchProfileByName("SIBYL B8 Motion");
        Assert.Equal("B8", b8.ModelId);
        Assert.True(b8.SupportsSomatosensory);
    }

    [Fact]
    public void Test_BuiltInSounds_AndOfficialDspTunings()
    {
        // 验证 3 个自带白噪音/疗愈音效
        var sounds = BuiltInSoundLibrary.Sounds;
        Assert.Equal(3, sounds.Count);
        Assert.Contains(sounds, s => s.Type == BuiltInSoundType.PinkNoise);
        Assert.Contains(sounds, s => s.Type == BuiltInSoundType.ForestBirds);
        Assert.Contains(sounds, s => s.Type == BuiltInSoundType.SummerRain);

        // 验证 3 个官方 DSP 硬件经典调音
        var dspTunings = BuiltInSoundLibrary.OfficialDspTunings;
        Assert.Equal(3, dspTunings.Count);
        Assert.Contains(dspTunings, t => t.Name.Contains("经典"));
        Assert.Contains(dspTunings, t => t.Name.Contains("摇滚"));
        Assert.Contains(dspTunings, t => t.Name.Contains("抒情"));
    }
}
