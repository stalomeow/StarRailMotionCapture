using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using static HSR.MotionCapture.BlendShapeAsset;

namespace HSR.MotionCapture
{
    public static class BlendShapeUtility
    {
        public static bool IsValidBone([NotNull] Transform rootBone, [CanBeNull] Transform bone)
        {
            return bone != null && bone.IsChildOf(rootBone);
        }

        [CanBeNull]
        public static Transform FindBone([NotNull] Transform rootBone, [CanBeNull] string bonePath)
        {
            return bonePath == null ? null : rootBone.Find(bonePath);
        }

        [CanBeNull]
        public static string GetBonePath([NotNull] Transform rootBone, [CanBeNull] Transform bone)
        {
            if (bone == null)
            {
                return null;
            }

            List<string> path = new();

            while (bone != rootBone)
            {
                // Not a valid child
                if (bone == null)
                {
                    Debug.LogError("Not a valid bone.");
                    return null;
                }

                path.Add(bone.name);
                bone = bone.parent;
            }

            // Returns Relative Bone Path
            path.Reverse();
            return string.Join('/', path);
        }

        public static void ApplyBoneModification(
            [NotNull] BoneModification modification,
            [NotNull] Transform rootBone,
            float rawWeight,
            Action<Transform> onWillApplyCallback = null,
            Action<Transform> onDidApplyCallback = null)
        {
            Transform bone = FindBone(rootBone, modification.BonePath);

            if (bone == null)
            {
                return;
            }

            onWillApplyCallback?.Invoke(bone);
            {
                float weight = modification.Curve.Evaluate(rawWeight);

                // 所有的位移、旋转、缩放都是相对于父坐标系的！不是 Space.Self！也不需要管 TRS 顺序！

                bone.GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);
                localPosition += Vector3.Lerp(Vector3.zero, modification.Translation, weight);
                localRotation = Slerp(Quaternion.identity, Quaternion.Euler(modification.Rotation), weight) * localRotation;
                bone.SetLocalPositionAndRotation(localPosition, localRotation);

                bone.localScale += Vector3.Lerp(Vector3.zero, modification.Scale, weight);
            }
            onDidApplyCallback?.Invoke(bone);

            // Quaternion.Slerp 当 t == 1 时返回的值不等于 b，不是很准
            static Quaternion Slerp(Quaternion a, Quaternion b, float t) => t switch
            {
                >= 1.0f => b,
                <= 0.0f => a,
                _ => Quaternion.SlerpUnclamped(a, b, t)
            };
        }

        public static void ApplyBlendShape(
            [NotNull] BlendShapeData blendShapeData,
            [NotNull] Transform rootBone,
            float rawWeight,
            Action<Transform> onWillModifyBoneCallback = null,
            Action<Transform> onDidModifyBoneCallback = null)
        {
            foreach (BoneModification modification in blendShapeData.BoneModifications)
            {
                ApplyBoneModification(modification, rootBone, rawWeight,
                    onWillApplyCallback: onWillModifyBoneCallback,
                    onDidApplyCallback: onDidModifyBoneCallback);
            }
        }
    }
}
