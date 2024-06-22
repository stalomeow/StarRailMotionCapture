using UnityEngine;

namespace HSR.MotionCapture.Net.Protos
{
    public partial class UnityQuaternion
    {
        public static implicit operator Quaternion(UnityQuaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        public static explicit operator UnityQuaternion(Quaternion q)
        {
            return new UnityQuaternion
            {
                W = q.w,
                X = q.x,
                Y = q.y,
                Z = q.z
            };
        }
    }
}
