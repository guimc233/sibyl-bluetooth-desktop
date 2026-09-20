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

        Assert.Equal(PacketBuilder.MagicHeader1, packet[0]);
        Assert.Equal(PacketBuilder.MagicHeader2, packet[1]);
        Assert.Equal((byte)SibylCommandId.AncMode, packet[2]);

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
        var packet = PacketBuilder.BuildEqPacket(originalGains);

        var parsed = PacketParser.Parse(packet);
        Assert.True(parsed.IsSuccess);
        Assert.Equal(SibylCommandId.Equalizer, parsed.CommandId);
        Assert.Equal(10, parsed.Payload.Length);

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
        Assert.Equal(1, parsed.Payload[0]);

        var status = new DeviceStatus { IsGameModeEnabled = false };
        PacketParser.ApplyToStatus(parsed, status);
        Assert.True(status.IsGameModeEnabled);
    }

    [Fact]
    public void Test_ChecksumValidation_RejectsCorruptedPacket()
    {
        var packet = PacketBuilder.BuildAncPacket(AncModeType.NoiseReduction);
        packet[^1] ^= 0xFF; // 破坏 Checksum

        var parsed = PacketParser.Parse(packet);
        Assert.False(parsed.IsSuccess);
        Assert.Contains("Checksum mismatch", parsed.ErrorMessage);
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
}
