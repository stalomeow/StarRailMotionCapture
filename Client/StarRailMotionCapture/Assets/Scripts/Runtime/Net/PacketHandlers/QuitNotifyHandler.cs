using HSR.MotionCapture.Net.Protos;
using System;
using UnityEngine;

namespace HSR.MotionCapture.Net.PacketHandlers
{
    [CustomPacketHandler(PacketCode.QuitNotify)]
    public class QuitNotifyHandler : IPacketHandler
    {
        public object ParsePayload(PacketCode code, ReadOnlySpan<byte> payloadBytes)
        {
            return null;
        }

        public void HandlePacketAndReleasePayload(UDPSession session, PacketCode code, object payload)
        {
            Debug.Log("Server is quit!");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
