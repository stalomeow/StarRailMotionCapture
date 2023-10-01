using System;
using System.Collections;
using System.Collections.Generic;
using HSR.MotionCapture.Net.Protos;
using UnityEngine;

namespace HSR.MotionCapture.Net.PacketHandlers
{
    [CustomPacketHandler(PacketCode.PoseData)]
    public class PoseDataHandler : IPacketHandler
    {
        public object ParsePayload(ReadOnlySpan<byte> payloadBytes)
        {
            throw new NotImplementedException();
        }

        public void Handle(UDPSession session, PacketCode code, object payload)
        {
            throw new NotImplementedException();
        }
    }
}
