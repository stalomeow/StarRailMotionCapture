using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HSR.MotionCapture
{
    public class BoneTransformSnapshot
    {
        public readonly Transform Bone;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;

        public BoneTransformSnapshot([NotNull] Transform bone)
        {
            Bone = bone;
            bone.GetLocalPositionAndRotation(out LocalPosition, out LocalRotation);
            LocalScale = bone.localScale;
        }

        public void Apply()
        {
            Bone.SetLocalPositionAndRotation(LocalPosition, LocalRotation);
            Bone.localScale = LocalScale;
        }

        public static void TakeMany(IEnumerable<Transform> bones, List<BoneTransformSnapshot> outSnapshots, bool clearList = true)
        {
            if (clearList)
            {
                outSnapshots.Clear();
            }

            outSnapshots.AddRange(bones.Select(bone => new BoneTransformSnapshot(bone)));
        }

        public static void ApplyMany(IEnumerable<BoneTransformSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                snapshot.Apply();
            }
        }
    }
}
