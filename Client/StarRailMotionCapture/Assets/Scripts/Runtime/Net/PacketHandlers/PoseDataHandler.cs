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
        public object ParsePayload(PacketCode code, ReadOnlySpan<byte> payloadBytes)
        {
            return PoseData.Parser.ParseFrom(payloadBytes);
        }

        public void HandlePacketAndReleasePayload(UDPSession session, PacketCode code, object payload)
        {
            using PoseData poseData = (PoseData)payload;
            // ...
        }

        // private static Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
        // {
        //     return Vector3.Cross(b - a, c - a).normalized;
        // }
        //
        // private static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
        // {
        //     // x -> forward
        //     // y -> forward x upwards
        //     // z -> forward x y
        //     Vector3 y = Vector3.Cross(forward, upwards);
        //     Vector3 z = Vector3.Cross(forward, y);
        //     return Quaternion.LookRotation(z, y);
        // }
        //
        // private static Quaternion LookRotation2(Vector3 forward, Vector3 upwards)
        // {
        //     // x -> forward
        //     // y -> forward x upwards
        //     // z -> forward x y
        //     Vector3 y = Vector3.Cross(forward, upwards);
        //     Vector3 z = Vector3.Cross(forward, y);
        //     return Quaternion.LookRotation(z, y);
        // }
    }
}
