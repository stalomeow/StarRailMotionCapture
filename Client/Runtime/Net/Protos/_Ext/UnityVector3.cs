using UnityEngine;

namespace HSR.MotionCapture.Net.Protos
{
    public partial class UnityVector3
    {
        public static implicit operator Vector3(UnityVector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static explicit operator UnityVector3(Vector3 v)
        {
            return new UnityVector3
            {
                X = v.x,
                Y = v.y,
                Z = v.z
            };
        }
    }
}
