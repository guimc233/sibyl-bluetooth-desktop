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
    public void RejectsUnknownVendorId()
    {
        // 官方产品清单之外的 VendorId 必须被忽略（设备不出现在列表中）
        Assert.False(SibylDeviceMatcher.TryParseSibylAdvertisement(SibylAdv(0xABCD), out _));
        Assert.False(SibylDeviceMatcher.IsSibylEarbuds(SibylAdv(0x1234)));
        // 厂商数据里带他机型、但 CompanyID 错误也要忽略
        Assert.False(SibylDeviceMatcher.IsSibylEarbuds(new Dictionary<ushort, byte[]> { [0x3E21] = new byte[12] }));
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
}