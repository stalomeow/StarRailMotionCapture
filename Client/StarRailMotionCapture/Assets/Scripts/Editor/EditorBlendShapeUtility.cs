using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static HSR.MotionCapture.BlendShapeAsset;

namespace HSR.MotionCapture.Editor
{
    public static class EditorBlendShapeUtility
    {
        private static class PropPaths
        {
            public static readonly string[] LocalPosition =
            {
                "m_LocalPosition.x",
                "m_LocalPosition.y",
                "m_LocalPosition.z"
            };
            public static readonly string[] LocalRotation =
            {
                "m_LocalRotation.x",
                "m_LocalRotation.y",
                "m_LocalRotation.z",
                "m_LocalRotation.w"
            };
            // public static readonly string[] LocalEulerAnglesHint =
            // {
            //     "m_LocalEulerAnglesHint.x",
            //     "m_LocalEulerAnglesHint.y",
            //     "m_LocalEulerAnglesHint.z"
            // };
            public static readonly string[] LocalScale =
            {
                "m_LocalScale.x",
                "m_LocalScale.y",
                "m_LocalScale.z"
            };
        }

        public static bool IsValidBonePropertyModification(UndoPropertyModification modification, Transform rootBone)
        {
            PropertyModification prevValue = modification.previousValue;

            if (prevValue.target is not Transform bone || !BlendShapeUtility.IsValidBone(rootBone, bone))
            {
                return false;
            }

            string propertyPath = prevValue.propertyPath;
            return PropPaths.LocalPosition.Contains(propertyPath)
                   || PropPaths.LocalRotation.Contains(propertyPath)
                   // 不可以处理 LocalEulerAnglesHint。不然在 SceneView 里 Rotate 就不会被加到 Undo History 里！
                   // || PropPaths.LocalEulerAnglesHint.Contains(propertyPath)
                   || PropPaths.LocalScale.Contains(propertyPath);
        }

        public static void ApplyBonePropertyModifications(IEnumerable<UndoPropertyModification> modifications)
        {
            Dictionary<Transform, BoneTransformSnapshot> bonePropMap = new();

            foreach (UndoPropertyModification modification in modifications)
            {
                Transform bone = (Transform)modification.currentValue.target;

                if (!bonePropMap.TryGetValue(bone, out BoneTransformSnapshot snapshot))
                {
                    snapshot = new BoneTransformSnapshot(bone);
                    bonePropMap.Add(bone, snapshot);
                }

                string propertyPath = modification.currentValue.propertyPath;
                string value = modification.currentValue.value;
                int axis;

                if ((axis = Array.IndexOf(PropPaths.LocalPosition, propertyPath)) >= 0)
                {
                    snapshot.LocalPosition[axis] = float.Parse(value);
                }
                else if ((axis = Array.IndexOf(PropPaths.LocalRotation, propertyPath)) >= 0)
                {
                    snapshot.LocalRotation[axis] = float.Parse(value);
                }
                else if ((axis = Array.IndexOf(PropPaths.LocalScale, propertyPath)) >= 0)
                {
                    snapshot.LocalScale[axis] = float.Parse(value);
                }
            }

            // 必须整体一起设置！
            // Unity 内部使用四元数表示旋转，只有单位四元数才能表示旋转
            // 如果一个分量一个分量地设置 Transform.localRotation，Unity 再做归一化，然后四元数的分量就全变了
            foreach (var snapshot in bonePropMap.Values)
            {
                snapshot.Apply();
            }
        }

        public static BoneModification CreateBoneModification(
            [NotNull] Transform rootBone,
            [NotNull] Transform bone,
            [NotNull] Dictionary<string, string> modifiedPropValues)
        {
            Vector3 prevLocalPosition = bone.localPosition;
            Quaternion prevLocalRotation = bone.localRotation;
            Vector3 prevLocalScale = bone.localScale;

            Vector3 currLocalPosition = prevLocalPosition;
            Quaternion currLocalRotation = prevLocalRotation;
            Vector3 currLocalScale = prevLocalScale;

            foreach ((string propPath, string propValue) in modifiedPropValues)
            {
                int axis;

                if ((axis = Array.IndexOf(PropPaths.LocalPosition, propPath)) >= 0)
                {
                    currLocalPosition[axis] = float.Parse(propValue);
                }
                else if ((axis = Array.IndexOf(PropPaths.LocalRotation, propPath)) >= 0)
                {
                    currLocalRotation[axis] = float.Parse(propValue);
                }
                else if ((axis = Array.IndexOf(PropPaths.LocalScale, propPath)) >= 0)
                {
                    currLocalScale[axis] = float.Parse(propValue);
                }
            }

            return new BoneModification
            {
                BonePath = BlendShapeUtility.GetBonePath(rootBone, bone),
                Translation = currLocalPosition - prevLocalPosition,
                Rotation = (currLocalRotation * Quaternion.Inverse(prevLocalRotation)).eulerAngles,
                Scale = currLocalScale - prevLocalScale
            };
        }

        public static List<PropertyModification> ToPropertyModifications(this BoneTransformSnapshot snapshot)
        {
            List<PropertyModification> modifications = new();

            for (int i = 0; i < PropPaths.LocalPosition.Length; i++)
            {
                modifications.Add(new PropertyModification
                {
                    propertyPath = PropPaths.LocalPosition[i],
                    target = snapshot.Bone,
                    value = snapshot.LocalPosition[i].ToString()
                });
            }

            for (int i = 0; i < PropPaths.LocalRotation.Length; i++)
            {
                modifications.Add(new PropertyModification
                {
                    propertyPath = PropPaths.LocalRotation[i],
                    target = snapshot.Bone,
                    value = snapshot.LocalRotation[i].ToString()
                });
            }

            for (int i = 0; i < PropPaths.LocalScale.Length; i++)
            {
                modifications.Add(new PropertyModification
                {
                    propertyPath = PropPaths.LocalScale[i],
                    target = snapshot.Bone,
                    value = snapshot.LocalScale[i].ToString()
                });
            }

            return modifications;
        }

        public static void MirrorBoneModification(BoneModification modification, Transform rootBone)
        {
            string prevBonePath = modification.BonePath;

            if (prevBonePath == null)
            {
                return;
            }

            // Bone
            string[] paths = prevBonePath.Split('/');
            for (int i = 0; i < paths.Length; i++)
            {
                // 左变右，右变左
                if (paths[i].EndsWith("_L"))
                {
                    paths[i] = paths[i][..^1] + "R";
                }
                else if (paths[i].EndsWith("_R"))
                {
                    paths[i] = paths[i][..^1] + "L";
                }
            }
            modification.BonePath = string.Join('/', paths);

            Transform prevBone = BlendShapeUtility.FindBone(rootBone, prevBonePath);
            Transform currBone = BlendShapeUtility.FindBone(rootBone, modification.BonePath);

            // 模型本身有旋转，但是模型里的第一个子物体一般没有旋转。
            Quaternion modelRootRotationInv = Quaternion.Inverse(rootBone.root.localRotation);

            Quaternion prevRot = modelRootRotationInv * prevBone.parent.rotation;
            // Quaternion prevRotInv = Quaternion.Inverse(prevRot);
            Quaternion currRot = modelRootRotationInv * currBone.parent.rotation;
            Quaternion currRotInv = Quaternion.Inverse(currRot);

            // 镜像的时候，先从旧的骨骼父空间变换到模型的本地空间，再镜像 X 轴，然后变换回新的骨骼父空间

            // Translation
            Vector3 translationMS = prevRot * modification.Translation;
            translationMS.x *= -1;
            modification.Translation = currRotInv * translationMS;

            // Rotation
            Quaternion.Euler(modification.Rotation).ToAngleAxis(out float angle, out Vector3 axis);
            axis = currRotInv * Vector3.Scale(prevRot * axis, new Vector3(-1, 1, 1));
            angle *= -1;
            modification.Rotation = Quaternion.AngleAxis(angle, axis).eulerAngles;

            // Scale
            Vector3 scaleMS = prevRot * modification.Scale;
            scaleMS.x *= -1;
            modification.Scale = currRotInv * scaleMS;
        }

        public static int CompareBoneModifications(BoneModification x, BoneModification y)
        {
            string xName = x.BonePath?.Split('/')[^1];
            string yName = y.BonePath?.Split('/')[^1];
            return StringComparer.CurrentCulture.Compare(xName, yName);
        }

        public static int CompareBoneModificationsReverse(BoneModification x, BoneModification y)
        {
            return CompareBoneModifications(y, x);
        }
    }
}
