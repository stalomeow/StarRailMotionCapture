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

        public void Handle(UDPSession session, PacketCode code, object payload)
        {
            using PoseData poseData = (PoseData)payload;

            // Vector3 hand1 = (Vector3)poseData.Landmarks[15] * 20;
            // Vector3 hand2 = (Vector3)poseData.Landmarks[17] * 20;
            // Vector3 hand3 = (Vector3)poseData.Landmarks[19] * 20;
            // Vector3 handNormalOut = TriangleNormal(hand1, hand2, hand3);
            //
            // Vector3 p1 = (Vector3)poseData.Landmarks[11] * 20;
            // Vector3 p2 = (Vector3)poseData.Landmarks[13] * 20;
            // Vector3 p3 = (Vector3)poseData.Landmarks[15] * 20;
            //
            // foreach (var actor in session.Actors)
            // {
            //     Transform leftUpperArm = actor.GetHumanBodyBone(HumanBodyBones.LeftUpperArm);
            //     Transform leftLowerArm = actor.GetHumanBodyBone(HumanBodyBones.LeftLowerArm);
            //     Transform leftHand = actor.GetHumanBodyBone(HumanBodyBones.LeftHand);
            //
            //     leftUpperArm.rotation = LookRotation((p2 - p1).normalized, Vector3.up);
            //     leftLowerArm.rotation = LookRotation((p3 - p2).normalized, handNormalOut);
            //     leftHand.rotation = LookRotation(((hand2 + hand3) * 0.5f - hand1).normalized, handNormalOut);
            // }
            //
            // for (int i = 0; i < poseData.Landmarks.Count; i++)
            // {
            //     session.Points.GetChild(i).localPosition = (Vector3)poseData.Landmarks[i] * 20;
            // }

            // Vector3 p1 = (Vector3)poseData.Landmarks[11] * 20;
            // Vector3 p2 = (Vector3)poseData.Landmarks[12] * 20;
            // // Vector3 p3 = (Vector3)poseData.Landmarks[23] * 20;
            // // Vector3 p4 = (Vector3)poseData.Landmarks[24] * 20;
            //
            // // Vector3 c1 = 0.5f * (p1 + p2);
            // // Vector3 c2 = 0.5f * (p3 + p4);
            //
            // foreach (var actor in session.Actors)
            // {
            //     Transform spine = actor.GetHumanBodyBone(HumanBodyBones.UpperChest);
            //
            //     float angle = Vector2.SignedAngle(p2 - p1, Vector2.right);
            //     Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
            //     actor.LastUCR = Quaternion.Slerp(rot, actor.LastUCR, 0.6f);
            //     spine.localRotation = actor.LastUCR * actor.UpperChest.LocalRotation;
            // }
        }

        private static Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).normalized;
        }

        private static Quaternion LookRotation(Vector3 forward, Vector3 upwards)
        {
            // x -> forward
            // y -> forward x upwards
            // z -> forward x y
            Vector3 y = Vector3.Cross(forward, upwards);
            Vector3 z = Vector3.Cross(forward, y);
            return Quaternion.LookRotation(z, y);
        }

        private static Quaternion LookRotation2(Vector3 forward, Vector3 upwards)
        {
            // x -> forward
            // y -> forward x upwards
            // z -> forward x y
            Vector3 y = Vector3.Cross(forward, upwards);
            Vector3 z = Vector3.Cross(forward, y);
            return Quaternion.LookRotation(z, y);
        }
    }
}
