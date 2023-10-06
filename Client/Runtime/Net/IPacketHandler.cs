using System;
using HSR.MotionCapture.Net.Protos;
using UnityEngine.Scripting;

namespace HSR.MotionCapture.Net
{
    [RequireImplementors]
    public interface IPacketHandler
    {
        object ParsePayload(PacketCode code, ReadOnlySpan<byte> payloadBytes);

        void Handle(UDPSession session, PacketCode code, object payload);
    }
}
