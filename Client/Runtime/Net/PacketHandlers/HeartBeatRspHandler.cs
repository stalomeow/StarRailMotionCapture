using HSR.MotionCapture.Net.Protos;
using System;
using UnityEngine;

namespace HSR.MotionCapture.Net.PacketHandlers
{
    [CustomPacketHandler(PacketCode.HeartBeatRsp)]
    public class HeartBeatRspHandler : IPacketHandler
    {
        public object ParsePayload(PacketCode code, ReadOnlySpan<byte> payloadBytes)
        {
            return null;
        }

        public void HandlePacketAndReleasePayload(UDPSession session, PacketCode code, object payload)
        {
            session.LastHeartBeatResponseTime = Time.realtimeSinceStartup;
            // Debug.Log("Heart beat rsp");
        }
    }
}
