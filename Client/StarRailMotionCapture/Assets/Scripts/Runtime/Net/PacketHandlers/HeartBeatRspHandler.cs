using HSR.MotionCapture.Net.Protos;
using System;

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
            session.RefreshLastHeartBeatResponseTime();
            // Debug.Log("Heart beat rsp");
        }
    }
}
