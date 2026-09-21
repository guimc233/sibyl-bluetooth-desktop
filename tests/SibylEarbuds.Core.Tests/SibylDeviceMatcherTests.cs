using SibylEarbuds.Core.Protocol;
using Xunit;

namespace SibylEarbuds.Core.Tests;

public class SibylDeviceMatcherTests
{
    [Fact]
    public void Test_Match_ByNamePrefixes()
    {
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds("SIBYL S1 ANC"));
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds("SIBYL B6 Cyber"));
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds("WEDOING S10"));
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds("B1 Pro Earbuds"));
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds("S11 LDAC"));
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds("Y1 Game Headset"));
        Assert.False(SibylDeviceMatcher.IsSibylEarbuds("Apple AirPods Pro"));
        Assert.False(SibylDeviceMatcher.IsSibylEarbuds("Sony WH-1000XM5"));
    }

    [Fact]
    public void Test_Match_ByManufacturerData()
    {
        // 包含 0x3E21 或 0x12CC 厂商字段
        var dict = new Dictionary<ushort, byte[]>
        {
            { 0x3E21, [0x01, 0x02] }
        };
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds(null, null, dict));

        var dictPayload = new Dictionary<ushort, byte[]>
        {
            { 0x05D6, [0xCC, 0x12, 0x00] } // payload 包含 0x12CC
        };
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds("Unknown Device", null, dictPayload));
    }

    [Fact]
    public void Test_Match_ByServiceUuids()
    {
        var uuids = new List<Guid> { SibylUuids.SibylServiceUuid };
        Assert.True(SibylDeviceMatcher.IsSibylEarbuds("Custom", uuids, null));
    }
}
