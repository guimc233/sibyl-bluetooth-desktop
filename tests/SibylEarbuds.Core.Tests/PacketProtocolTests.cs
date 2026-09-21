using SibylEarbuds.Core.Models;
using SibylEarbuds.Core.Protocol;
using Xunit;

namespace SibylEarbuds.Core.Tests;

public class PacketProtocolTests
{
    [Fact]
    public void BuildPacket_ProducesOfficialFrameLayout()
    {
        byte[] packet = PacketBuilder.BuildPacket(SibylCommandId.GameMode, [0x01]);

        Assert.Equal(PacketBuilder.StartByte, packet[0]);
        Assert.Equal(0x02, packet[2]); // Len = payload + cmd id
        Assert.Equal((byte)SibylCommandId.GameMode, packet[3]);
        Assert.Equal(0x01, packet[4]);
        Assert.Equal(PacketBuilder.EndByte, packet[^1]);
        Assert.Equal(6, packet.Length);
    }

    [Fact]
    public void Parse_RoundTripsBuiltPacket()
    {
        byte[] packet = PacketBuilder.BuildAncPacket(AncModeType.NoiseReduction, AncDepthLevel.Comfortable);

        var parsed = PacketParser.Parse(packet);

        Assert.True(parsed.IsSuccess);
        Assert.Equal(SibylCommandId.AncMode, parsed.CommandId);
        Assert.Equal([(byte)AncModeType.NoiseReduction, (byte)AncDepthLevel.Comfortable], parsed.Payload);

        // 验证非降噪模式（普通与通透）下第二字节严格为 0
        byte[] transPacket = PacketBuilder.BuildAncPacket(AncModeType.Transparency, AncDepthLevel.Deep);
        var transParsed = PacketParser.Parse(transPacket);
        Assert.Equal(0, transParsed.Payload[1]);
    }

    [Fact]
    public void Parse_RejectsPacketWithBadTail()
    {
        byte[] packet = PacketBuilder.BuildGameModePacket(true);
        packet[^1] = 0x00;

        var parsed = PacketParser.Parse(packet);

        Assert.False(parsed.IsSuccess);
    }

    [Fact]
    public void ApplyToStatus_DecodesBatteryAndChargingFlags()
    {
        var status = new DeviceStatus();
        byte[] packet = PacketBuilder.BuildPacket(SibylCommandId.Battery, [0x85, 0x40, 0x64]);

        var parsed = PacketParser.Parse(packet);
        bool applied = PacketParser.ApplyToStatus(parsed, status);

        Assert.True(applied);
        Assert.Equal(5, status.LeftBattery);
        Assert.True(status.IsLeftCharging);
        Assert.Equal(64, status.RightBattery);
        Assert.False(status.IsRightCharging);
        Assert.Equal(100, status.CaseBattery);
    }

    [Fact]
    public void BuildEqPacket_ClampsGainsToOneByteSignedRange()
    {
        int[] gains = [-99, 99, 0, 0, 0, 0, 0, 0, 0, 0];

        byte[] packet = PacketBuilder.BuildEqPacket(gains, presetType: 2);
        var parsed = PacketParser.Parse(packet);

        Assert.True(parsed.IsSuccess);
        Assert.Equal(2, parsed.Payload[0]);
        Assert.Equal(-8, (sbyte)parsed.Payload[1]);
        Assert.Equal(8, (sbyte)parsed.Payload[2]);
    }

    [Fact]
    public void BuildTimedShutdownPacket_IsLittleEndianMinutes()
    {
        byte[] packet = PacketBuilder.BuildTimedShutdownPacket(300);
        var parsed = PacketParser.Parse(packet);

        Assert.True(parsed.IsSuccess);
        Assert.Equal(300, parsed.Payload[0] | (parsed.Payload[1] << 8));
    }

    [Fact]
    public void ApplyToStatus_DecodesFirmwareVersion()
    {
        var status = new DeviceStatus();
        byte[] packet = PacketBuilder.BuildPacket(SibylCommandId.FirmwareVersion, [1, 2, 3]);

        PacketParser.ApplyToStatus(PacketParser.Parse(packet), status);

        Assert.Equal("V1.2.3", status.FirmwareVersion);
    }
}
