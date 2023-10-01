using HSR.MotionCapture.Net.Protos;
using JetBrains.Annotations;
using System;

namespace HSR.MotionCapture.Net
{
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
