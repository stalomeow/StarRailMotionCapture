using HSR.MotionCapture.Net.Protos;
using JetBrains.Annotations;
using System;

namespace HSR.MotionCapture.Net
{
    public static class Packet
    {
        public delegate int WritePayloadAction<in T>(Span<byte> span, T payload);

        public const ushort CONST_HEAD = 0x2B3C;
        public const ushort CONST_TAIL = 0x4D5F;

        public static bool TryRead(Span<byte> bytes, out PacketCode code, out ReadOnlySpan<byte> payloadBytes)
        {
            code = PacketCode.None;
            payloadBytes = ReadOnlySpan<byte>.Empty;

            // sizeof(CONST_HEAD + PacketCode + PayloadLength + CONST_TAIL) == 8
            if (bytes.Length < 8)
            {
                return false;
            }

            // Check Head Const
            if (ReadUInt16(bytes[..2]) != CONST_HEAD)
            {
                return false;
            }

            int payloadLength = ReadUInt16(bytes[4..6]);

            // Check Packet Size
            if (bytes.Length != 8 + payloadLength)
            {
                return false;
            }

            // Check Tail Const
            if (ReadUInt16(bytes[^2..]) != CONST_TAIL)
            {
                return false;
            }

            code = (PacketCode)ReadUInt16(bytes[2..4]);
            payloadBytes = bytes.Slice(6, payloadLength);
            return true;
        }

        public static int Write<T>(Span<byte> buffer, PacketCode packetCode, T payload, [CanBeNull] WritePayloadAction<T> writePayloadBytes)
        {
            WriteUInt16(buffer[..2], CONST_HEAD);
            WriteUInt16(buffer[2..4], (ushort)packetCode);
            int payloadLength = writePayloadBytes?.Invoke(buffer[6..], payload) ?? 0;
            WriteUInt16(buffer[4..6], (ushort)payloadLength);
            WriteUInt16(buffer.Slice(6 + payloadLength, 2), CONST_TAIL);
            return 8 + payloadLength;
        }

        private static ushort ReadUInt16(Span<byte> bytes)
        {
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        private static void WriteUInt16(Span<byte> buffer, ushort value)
        {
            buffer[0] = (byte)((value >> 8) & 0xFF);
            buffer[1] = (byte)(value & 0xFF);
        }
    }
}
