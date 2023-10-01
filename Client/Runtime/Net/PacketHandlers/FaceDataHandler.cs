using HSR.MotionCapture.Net.Protos;
using System;
using UnityEngine;

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
                actor.RotateHead(MediaPipeToUnityRotation(data.HeadRotation));

                foreach (var blendShape in data.BlendShapes)
                {
                    actor.SetBlendShapeWeight(blendShape.Name, blendShape.Value);
                }
            }
        }

        private static Quaternion MediaPipeToUnityRotation(FaceData.Types.Quaternion rotation)
        {
            // Unity 是左手系。Y 轴向上，Z 轴向前，X 轴向右
            // mediapipe 是右手系。Y 轴向上，Z 轴向前，X 轴向左
            // rotation.w == cos(theta/2) 偶函数，不用管
            return new Quaternion(rotation.X, -rotation.Y, -rotation.Z, rotation.W);
        }
    }
}
