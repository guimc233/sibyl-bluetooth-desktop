using SibylEarbuds.Core.Protocol;
using Xunit;

namespace SibylEarbuds.Core.Tests;

public class SibylDeviceMatcherTests
{
    /// <summary>构造官方格式的厂商广播 payload（CompanyID 之后的 12 字节）。</summary>
    private static Dictionary<ushort, byte[]> SibylAdv(int vendorId, byte left = 85, byte right = 90, byte @case = 100)
    {
        byte[] payload = new byte[12];
        payload[0] = (byte)(vendorId >> 8);
        payload[1] = (byte)vendorId;
        payload[2] = left;
        payload[3] = right;
        payload[4] = @case;
        payload[5] = 0x00; // status
        for (int i = 6; i < 12; i++)
        {
            payload[i] = (byte)(i * 7); // MAC 填充位
        }

        return new Dictionary<ushort, byte[]>
        {
            [SibylDeviceMatcher.SibylManufacturerId] = payload
        };
    }

    [Fact]
    public void RecognizedOfficialModels_Resolve()
    {
        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(15377), out var s1));
        Assert.Equal("S1", s1.ModelName);

        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(15889), out var s7));
        Assert.Equal("S7", s7.ModelName);

        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(15895), out var s10));
        Assert.Equal("S10", s10.ModelName);

        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(15921), out var b1a));
        Assert.Equal("B1", b1a.ModelName);

        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(16017), out var b1b));
        Assert.Equal("B1", b1b.ModelName);

        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(15985), out var y1));
        Assert.Equal("Y1", y1.ModelName);

        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(16193), out var y1b));
        Assert.Equal("Y1", y1b.ModelName);
    }

    [Fact]
    public void DecodesVendorIdAndBattery()
    {
        var adv = SibylAdv(15377, left: 60, right: 72, @case: 100);
        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(adv, out var parsed));

        Assert.Equal(15377, parsed.VendorId);
        Assert.Equal(60, parsed.LeftBattery);
        Assert.Equal(72, parsed.RightBattery);
        Assert.Equal(100, parsed.CaseBattery);
    }

    [Fact]
    public void RejectsUnknownCompanyId()
    {
        // 非 51474 且无 SIBYL 特征的厂商数据必须被忽略
        var unknownCompanyAdv = new Dictionary<ushort, byte[]>
        {
            [0x1234] = new byte[12]
        };
        Assert.False(SibylDeviceMatcher.IsSibylEarbuds(unknownCompanyAdv));

        // 官方广播中的未知新机型 VendorId 会自动回退为 PRO 通用适配，而不会被丢弃
        Assert.True(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(0xABCD), out var fallback));
        Assert.Equal("PRO", fallback.ModelName);
    }

    [Fact]
    public void RejectsMissingOrShortManufacturerData()
    {
        // 无厂商数据
        Assert.False(SibylDeviceMatcher.IsSibylEarbuds(null));
        // 长度不足 12 字节的 payload
        var shortPayload = new Dictionary<ushort, byte[]>
        {
            [SibylDeviceMatcher.SibylManufacturerId] = [0x3C, 0x11, 0x55]
        };
        Assert.False(SibylDeviceMatcher.IsSibylEarbuds(shortPayload));
    }

    [Fact]
    public void ResolveModelName_FallsBackToGeneric()
    {
        Assert.Equal("S1", SibylDeviceMatcher.ResolveModelName(15377));
        Assert.Equal("PRO", SibylDeviceMatcher.ResolveModelName(9999));
    }

    [Fact]
    public void TryMatchSibylDevice_MatchesSibylNames_AndRejectsNonSibyl()
    {
        // 必须匹配各种 SIBYL 品牌设备并自动提取型号
        Assert.True(SibylDeviceMatcher.TryMatchSibylDevice("SIBYL S1 ANC", null, null, out _, out string m1));
        Assert.Equal("S1", m1);

        Assert.True(SibylDeviceMatcher.TryMatchSibylDevice("SIBYL B6 Cyber", null, null, out _, out string m2));
        Assert.Equal("B6", m2);

        Assert.True(SibylDeviceMatcher.TryMatchSibylDevice("SIBYL S10", null, null, out _, out string m3));
        Assert.Equal("S10", m3);

        Assert.True(SibylDeviceMatcher.TryMatchSibylDevice("SIBYL S11 LDAC", null, null, out _, out string m4));
        Assert.Equal("S11", m4);

        Assert.True(SibylDeviceMatcher.TryMatchSibylDevice("SIBYL Y1 Game", null, null, out _, out string m5));
        Assert.Equal("Y1", m5);

        Assert.True(SibylDeviceMatcher.TryMatchSibylDevice("SIBYL Earbuds", null, null, out _, out string m6));
        Assert.Equal("PRO", m6);

        // 必须彻底拒绝非 SIBYL 设备（如苹果、索尼、华为、小米等）
        Assert.False(SibylDeviceMatcher.TryMatchSibylDevice("Apple AirPods Pro", null, null, out _, out _));
        Assert.False(SibylDeviceMatcher.TryMatchSibylDevice("Sony WH-1000XM5", null, null, out _, out _));
        Assert.False(SibylDeviceMatcher.TryMatchSibylDevice("HUAWEI FreeBuds 4", null, null, out _, out _));
        Assert.False(SibylDeviceMatcher.TryMatchSibylDevice("Xiaomi Buds 4", null, null, out _, out _));
        Assert.False(SibylDeviceMatcher.TryMatchSibylDevice("DESKTOP-ABCDEF", null, null, out _, out _));
        Assert.False(SibylDeviceMatcher.TryMatchSibylDevice(null, null, null, out _, out _));
        Assert.False(SibylDeviceMatcher.TryMatchSibylDevice("", null, null, out _, out _));
    }

    [Fact]
    public void TryMatchSibylDevice_MatchesServiceUuid()
    {
        var uuids = new List<Guid> { SibylUuids.SibylServiceUuid };
        Assert.True(SibylDeviceMatcher.TryMatchSibylDevice("My Headphone", null, uuids, out _, out string model));
        Assert.Equal("PRO", model);
    }
}
