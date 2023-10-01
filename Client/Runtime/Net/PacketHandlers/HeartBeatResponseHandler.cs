using HSR.MotionCapture.Net.Protos;
using System;
using UnityEngine;

namespace HSR.MotionCapture.Net.PacketHandlers
{
    [CustomPacketHandler(PacketCode.ClientHeartBeatServerResponse)]
    public class HeartBeatResponseHandler : IPacketHandler
    {
        public object ParsePayload(ReadOnlySpan<byte> payloadBytes)
        {
            return null;
        }

        public void Handle(UDPSession session, PacketCode code, object payload)
        {
            session.LastHeartBeatResponseTime = Time.realtimeSinceStartup;
            // Debug.Log("Heart beat respond");
        }
    }
}
