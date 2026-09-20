using System.Runtime.InteropServices;
using SibylEarbuds.Core.Transport;

namespace SibylEarbuds.App.Bluetooth;

public static class TransportFactory
{
    /// <summary>
    /// 创建蓝牙传输实例
    /// </summary>
    /// <param name="forceMock">是否强制启用模拟演示模式</param>
    public static IBleTransport Create(bool forceMock = false)
    {
        if (forceMock || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new MockBleTransport();
        }

        return new WinRtBleTransport();
    }
}
