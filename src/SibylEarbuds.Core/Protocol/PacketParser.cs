using System.Text;
using SibylEarbuds.Core.Models;

namespace SibylEarbuds.Core.Protocol;

public class ParsedPacketResult
{
    public bool IsSuccess { get; set; }
    public SibylCommandId CommandId { get; set; }
    public byte SubCmd { get; set; }
    public byte[] Payload { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public static class PacketParser
{
    /// <summary>
    /// 解析原始 BLE 接收字节流
    /// </summary>
    public static ParsedPacketResult Parse(byte[] data)
    {
        if (data == null || data.Length < 6)
        {
            return new ParsedPacketResult { IsSuccess = false, ErrorMessage = "Packet too short" };
        }

        // 检查 Header
        if (data[0] != PacketBuilder.MagicHeader1 || data[1] != PacketBuilder.MagicHeader2)
        {
            return new ParsedPacketResult { IsSuccess = false, ErrorMessage = "Invalid magic header" };
        }

        var commandId = (SibylCommandId)data[2];
        byte subCmd = data[3];
        byte payloadLen = data[4];

        if (data.Length < 5 + payloadLen + 1)
        {
            return new ParsedPacketResult { IsSuccess = false, ErrorMessage = "Incomplete payload" };
        }

        // 校验 Checksum
        byte calculatedChecksum = 0;
        for (int i = 2; i < 5 + payloadLen; i++)
        {
            calculatedChecksum = (byte)(calculatedChecksum + data[i]);
        }

        byte receivedChecksum = data[5 + payloadLen];
        if (calculatedChecksum != receivedChecksum)
        {
            return new ParsedPacketResult
            {
                IsSuccess = false,
                ErrorMessage = $"Checksum mismatch: expected {calculatedChecksum:X2}, got {receivedChecksum:X2}"
            };
        }

        var payload = new byte[payloadLen];
        if (payloadLen > 0)
        {
            Array.Copy(data, 5, payload, 0, payloadLen);
        }

        return new ParsedPacketResult
        {
            IsSuccess = true,
            CommandId = commandId,
            SubCmd = subCmd,
            Payload = payload
        };
    }

    /// <summary>
    /// 将解析后的数据更新到设备全局状态对象中
    /// </summary>
    public static bool ApplyToStatus(ParsedPacketResult packet, DeviceStatus status)
    {
        if (!packet.IsSuccess) return false;

        switch (packet.CommandId)
        {
            case SibylCommandId.QueryStatus:
                // Payload: [LeftBat], [RightBat], [CaseBat], [AncMode], [GameMode], [Flags...]
                if (packet.Payload.Length >= 3)
                {
                    status.LeftBattery = Math.Clamp(packet.Payload[0] & 0x7F, 0, 100);
                    status.IsLeftCharging = (packet.Payload[0] & 0x80) != 0;

                    status.RightBattery = Math.Clamp(packet.Payload[1] & 0x7F, 0, 100);
                    status.IsRightCharging = (packet.Payload[1] & 0x80) != 0;

                    status.CaseBattery = Math.Clamp(packet.Payload[2] & 0x7F, 0, 100);
                    status.IsCaseCharging = (packet.Payload[2] & 0x80) != 0;
                }
                if (packet.Payload.Length >= 4)
                {
                    var mode = (AncModeType)packet.Payload[3];
                    var depth = packet.Payload.Length >= 5 ? (AncDepthLevel)packet.Payload[4] : AncDepthLevel.Deep;
                    status.Anc = new AncState(mode, depth);
                }
                if (packet.Payload.Length >= 6)
                {
                    status.IsGameModeEnabled = packet.Payload[5] == 1;
                }
                return true;

            case SibylCommandId.AncMode:
                if (packet.Payload.Length >= 1)
                {
                    var mode = (AncModeType)packet.Payload[0];
                    var depth = packet.Payload.Length >= 2 ? (AncDepthLevel)packet.Payload[1] : AncDepthLevel.Deep;
                    status.Anc = new AncState(mode, depth);
                    return true;
                }
                break;

            case SibylCommandId.Equalizer:
                if (packet.Payload.Length >= EqConfiguration.BandCount)
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

            case SibylCommandId.GameMode:
                if (packet.Payload.Length >= 1)
                {
                    status.IsGameModeEnabled = packet.Payload[0] == 1;
                    return true;
                }
                break;

            case SibylCommandId.CloseTouch:
                if (packet.Payload.Length >= 1)
                {
                    status.IsTouchDisabled = packet.Payload[0] == 1;
                    return true;
                }
                break;

            case SibylCommandId.FirmwareVersion:
                if (packet.Payload.Length > 0)
                {
                    status.FirmwareVersion = Encoding.UTF8.GetString(packet.Payload).Trim('\0');
                    return true;
                }
                break;

            case SibylCommandId.PairName:
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
