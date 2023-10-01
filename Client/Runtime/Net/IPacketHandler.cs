using System;
using HSR.MotionCapture.Net.Protos;

namespace HSR.MotionCapture.Net
{
    public interface IPacketHandler
    {
        object ParsePayload(ReadOnlySpan<byte> payloadBytes);

        void Handle(UDPSession session, PacketCode code, object payload);
    }
}
