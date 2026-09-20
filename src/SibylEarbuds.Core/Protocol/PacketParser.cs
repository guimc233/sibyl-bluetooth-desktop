using System.Text;
using SibylEarbuds.Core.Models;

namespace SibylEarbuds.Core.Protocol;

public class ParsedPacketResult
{
    public bool IsSuccess { get; set; }
    public SibylCommandId CommandId { get; set; }
    public byte[] Payload { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public static class PacketParser
{
    /// <summary>
    /// 解析官方 0xFF ... 0xAA 数据流
    /// 格式: [0xFF] [SeqIndex] [Len] [CmdId] [Payload...] [0xAA]
    /// 其中 Len 是 CmdId + Payload 的长度
    /// </summary>
    public static ParsedPacketResult Parse(byte[] data)
    {
        if (data == null || data.Length < 5)
        {
            return new ParsedPacketResult { IsSuccess = false, ErrorMessage = "Packet too short" };
        }

        // 寻找 0xFF 起始字节
        int start = -1;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == PacketBuilder.StartByte)
            {
                start = i;
                break;
            }
        }

        if (start < 0 || start + 4 >= data.Length)
        {
            return new ParsedPacketResult { IsSuccess = false, ErrorMessage = "No valid 0xFF header" };
        }

        int len = data[start + 2] & 0xFF; // CmdId + Payload 的总长度
        int packetTotalLen = len + 4; // 1(0xFF) + 1(seq) + 1(len) + len + 1(0xAA)

        if (start + packetTotalLen > data.Length)
        {
            return new ParsedPacketResult { IsSuccess = false, ErrorMessage = "Incomplete packet payload" };
        }

        if (data[start + packetTotalLen - 1] != PacketBuilder.EndByte)
        {
            return new ParsedPacketResult { IsSuccess = false, ErrorMessage = "Invalid 0xAA tail byte" };
        }

        var cmdId = (SibylCommandId)data[start + 3];
        int payloadLen = len - 1;
        byte[] payload = new byte[payloadLen];
        if (payloadLen > 0)
        {
            Array.Copy(data, start + 4, payload, 0, payloadLen);
        }

        return new ParsedPacketResult
        {
            IsSuccess = true,
            CommandId = cmdId,
            Payload = payload
        };
    }

    /// <summary>
    /// 将官方回包解析结果应用到 DeviceStatus
    /// </summary>
    public static bool ApplyToStatus(ParsedPacketResult packet, DeviceStatus status)
    {
        if (!packet.IsSuccess) return false;

        switch (packet.CommandId)
        {
            case SibylCommandId.Battery: // 12 (0x0C)
                // 官方 checkBattery: frame[1]=Left, frame[2]=Right, frame[3]=Case
                if (packet.Payload.Length >= 3)
                {
                    byte b1 = packet.Payload[0];
                    byte b2 = packet.Payload[1];
                    byte b3 = packet.Payload[2];

                    status.LeftBattery = Math.Clamp(b1 & 0x7F, 0, 100);
                    status.IsLeftCharging = (b1 & 0x80) != 0;

                    status.RightBattery = Math.Clamp(b2 & 0x7F, 0, 100);
                    status.IsRightCharging = (b2 & 0x80) != 0;

                    status.CaseBattery = Math.Clamp(b3 & 0x7F, 0, 100);
                    status.IsCaseCharging = (b3 & 0x80) != 0;
                    return true;
                }
                break;

            case SibylCommandId.AncMode: // 9 (0x09)
                // 官方 checkANCMode: payload[0]=mode, payload[1]=depth
                if (packet.Payload.Length >= 1)
                {
                    var mode = (AncModeType)packet.Payload[0];
                    var depth = packet.Payload.Length >= 2 ? (AncDepthLevel)packet.Payload[1] : AncDepthLevel.Deep;
                    status.Anc = new AncState(mode, depth);
                    return true;
                }
                break;

            case SibylCommandId.Equalizer: // 2 (0x02)
                // 官方 checkEQData: payload[0]=type, payload[1..10]=10段增益
                if (packet.Payload.Length >= 11)
                {
                    var gains = new int[EqConfiguration.BandCount];
                    for (int i = 0; i < EqConfiguration.BandCount; i++)
                    {
                        gains[i] = (sbyte)packet.Payload[i + 1];
                    }
                    status.CurrentEq = new EqConfiguration("耳机当前音效", packet.Payload[0], gains);
                    return true;
                }
                else if (packet.Payload.Length >= 10)
                {
                    var gains = new int[EqConfiguration.BandCount];
                    for (int i = 0; i < EqConfiguration.BandCount; i++)
                    {
                        gains[i] = (sbyte)packet.Payload[i];
                    }
                    status.CurrentEq = new EqConfiguration("耳机当前音效", 0, gains);
                    return true;
                }
                break;

            case SibylCommandId.GameMode: // 14 (0x0E)
                if (packet.Payload.Length >= 1)
                {
                    status.IsGameModeEnabled = packet.Payload[0] == 1;
                    return true;
                }
                break;

            case SibylCommandId.CloseTouch: // 7 (0x07)
                if (packet.Payload.Length >= 1)
                {
                    status.IsTouchDisabled = packet.Payload[0] == 1;
                    return true;
                }
                break;

            case SibylCommandId.SleepMode: // 33 (0x21)
                if (packet.Payload.Length >= 1)
                {
                    status.IsSleepModeEnabled = packet.Payload[0] == 1;
                    return true;
                }
                break;

            case SibylCommandId.TimedShutdown: // 32 (0x20)
                if (packet.Payload.Length >= 2)
                {
                    status.TimedShutdownMinutes = packet.Payload[0] | (packet.Payload[1] << 8);
                    return true;
                }
                else if (packet.Payload.Length >= 1)
                {
                    status.TimedShutdownMinutes = packet.Payload[0];
                    return true;
                }
                break;

            case SibylCommandId.FirmwareVersion: // 13 (0x0D)
                // 官方 checkVersion: v1.v2.v3
                if (packet.Payload.Length >= 3)
                {
                    status.FirmwareVersion = $"V{packet.Payload[0]}.{packet.Payload[1]}.{packet.Payload[2]}";
                    return true;
                }
                else if (packet.Payload.Length > 0)
                {
                    status.FirmwareVersion = Encoding.UTF8.GetString(packet.Payload).Trim('\0');
                    return true;
                }
                break;

            case SibylCommandId.PairName: // 39 (0x27)
                if (packet.Payload.Length > 0)
                {
                    status.DeviceName = Encoding.UTF8.GetString(packet.Payload).Trim('\0');
                    return true;
                }
                break;
        }

        return false;
    }
}
