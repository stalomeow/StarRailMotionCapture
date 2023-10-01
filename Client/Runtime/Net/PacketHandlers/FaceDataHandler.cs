using HSR.MotionCapture.Net.Protos;
using System;

namespace HSR.MotionCapture.Net.PacketHandlers
{
    [CustomPacketHandler(PacketCode.FaceData)]
    public class FaceDataHandler : IPacketHandler
    {
        public object ParsePayload(ReadOnlySpan<byte> payloadBytes)
        {
            return FaceData.Parser.ParseFrom(payloadBytes);
        }

        public void Handle(UDPSession session, PacketCode code, object payload)
        {
            FaceData data = (FaceData)payload;

            foreach (var actor in session.Actors)
            {
                actor.ResetHeadAndFace();
                actor.RotateHead(data.HeadRotation);

                foreach (var blendShape in data.BlendShapes)
                {
                    actor.SetBlendShapeWeight(blendShape.Name, blendShape.Value);
                }
            }
        }
    }
}
