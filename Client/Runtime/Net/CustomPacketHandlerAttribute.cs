using HSR.MotionCapture.Net.Protos;
using JetBrains.Annotations;
using System;
using UnityEngine.Scripting;

namespace HSR.MotionCapture.Net
{
    [RequireAttributeUsages]
    [BaseTypeRequired(typeof(IPacketHandler))]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CustomPacketHandlerAttribute : Attribute
    {
        public PacketCode HandlePacketCode { get; }

        public CustomPacketHandlerAttribute(PacketCode handlePacketCode)
        {
            HandlePacketCode = handlePacketCode;
        }
    }
}
