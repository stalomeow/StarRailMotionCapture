using HSR.MotionCapture.Net.Protos;
using System;
using UnityEngine;

namespace HSR.MotionCapture.Net.PacketHandlers
{
    [CustomPacketHandler(PacketCode.Disconnect)]
    public class DisconnectHandler : IPacketHandler
    {
        public object ParsePayload(PacketCode code, ReadOnlySpan<byte> payloadBytes)
        {
            return null;
        }

        public void Handle(UDPSession session, PacketCode code, object payload)
        {
            Debug.Log("Server is disconnected");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}
