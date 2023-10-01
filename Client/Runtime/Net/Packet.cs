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

        public static bool TryRead(byte[] bytes, out PacketCode code, out ReadOnlySpan<byte> payloadBytes)
        {
            ushort head = ReadUInt16(bytes, 0);
            if (head != CONST_HEAD)
            {
                code = default;
                payloadBytes = default;
                return false;
            }

            ushort packetCode = ReadUInt16(bytes, 2);
            code = (PacketCode)packetCode;
            ushort payloadLength = ReadUInt16(bytes, 4);
            payloadBytes = new ReadOnlySpan<byte>(bytes, 6, payloadLength);

            ushort tail = ReadUInt16(bytes, 6 + payloadLength);
            if (tail != CONST_TAIL)
            {
                code = default;
                payloadBytes = default;
                return false;
            }

            return true;
        }

        public static int Write<T>(byte[] buffer, PacketCode packetCode, T payload, [CanBeNull] WritePayloadAction<T> writePayloadBytes)
        {
            WriteUInt16(buffer, 0, CONST_HEAD);
            WriteUInt16(buffer, 2, (ushort)packetCode);
            int payloadLength = writePayloadBytes?.Invoke(buffer.AsSpan(6), payload) ?? 0;
            WriteUInt16(buffer, 4, (ushort)payloadLength);
            WriteUInt16(buffer, 6 + payloadLength, CONST_TAIL);
            return 8 + payloadLength;
        }

        private static ushort ReadUInt16(byte[] bytes, int startIndex)
        {
            int b1 = bytes[startIndex];
            int b2 = bytes[startIndex + 1];
            return (ushort)((b1 << 8) | b2);
        }

        private static void WriteUInt16(byte[] buffer, int startIndex, ushort value)
        {
            buffer[startIndex] = (byte)((value >> 8) & 0xFF);
            buffer[startIndex + 1] = (byte)(value & 0xFF);
        }
    }
}
