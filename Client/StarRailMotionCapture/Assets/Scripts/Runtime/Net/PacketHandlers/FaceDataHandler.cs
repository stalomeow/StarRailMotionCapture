using HSR.MotionCapture.Net.Protos;
using System;

namespace HSR.MotionCapture.Net.PacketHandlers
{
    [CustomPacketHandler(PacketCode.FaceData)]
    public class FaceDataHandler : IPacketHandler
    {
        public object ParsePayload(PacketCode code, ReadOnlySpan<byte> payloadBytes)
        {
            return FaceData.Parser.ParseFrom(payloadBytes);
        }

        public void HandlePacketAndReleasePayload(UDPSession session, PacketCode code, object payload)
        {
            using FaceData data = (FaceData)payload;

            foreach (var actor in session.Actors)
            {
                actor.ResetHeadAndFace();
                actor.RotateHead(data.HeadRotation);

                for (int i = 0; i < data.BlendShapes.Count; i++)
                {
                    var blendShape = data.BlendShapes[i];
                    actor.SetBlendShapeWeight(blendShape.Name, blendShape.Value);
                }
            }
        }
    }
}
