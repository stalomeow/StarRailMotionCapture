using System;
using System.Collections.Generic;
using UnityEngine;

namespace HSR.MotionCapture
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Honkai Star Rail/Motion Capture/Motion Actor (Game Model)")]
    public class MotionActorGameModel : RemoteMotionDataReceiver
    {
        [Header("Renderers")]

        [SerializeField] private SkinnedMeshRenderer m_FaceRenderer;

        [Header("Motion Settings")]

        [SerializeField] private BlendShapeAsset m_BlendShapeData;

        [SerializeField, Range(0, 1)] private float m_HeadRotationSmooth = 0.1f;
        [SerializeField, Range(0, 1)] private float m_BlendShapeSmooth = 0.1f;
        [SerializeField] private bool m_FlipHorizontally = false;

        [NonSerialized] private Transform m_FaceRootBone;
        [NonSerialized] private List<BoneTransformSnapshot> m_FaceBoneInitialStates;
        [NonSerialized] private Dictionary<string, BlendShapeAsset.BlendShapeData> m_BlendShapeMap;
        [NonSerialized] private Dictionary<string, BlendShapeAsset.BlendShapeData> m_BlendShapeMapHFlip;
        [NonSerialized] private Dictionary<string, float> m_LastBlendShapeWeights;
        [NonSerialized] private Quaternion m_LastHeadRotation;

        protected override void Start()
        {
            m_FaceRootBone = m_FaceRenderer.rootBone;
            m_FaceBoneInitialStates = new List<BoneTransformSnapshot>();
            BoneTransformSnapshot.TakeMany(m_FaceRenderer.bones, m_FaceBoneInitialStates);

            m_BlendShapeMap = new Dictionary<string, BlendShapeAsset.BlendShapeData>();
            m_BlendShapeMapHFlip = new Dictionary<string, BlendShapeAsset.BlendShapeData>();

            foreach (var blendShape in m_BlendShapeData.BlendShapes)
            {
                string blendShapeName = blendShape.Name;
                m_BlendShapeMap.Add(blendShapeName, blendShape);

                if (blendShapeName.EndsWith("Left"))
                {
                    m_BlendShapeMapHFlip.Add(blendShapeName[..^4] + "Right", blendShape);
                }
                else if (blendShapeName.EndsWith("Right"))
                {
                    m_BlendShapeMapHFlip.Add(blendShapeName[..^5] + "Left", blendShape);
                }
                else
                {
                    m_BlendShapeMapHFlip.Add(blendShapeName, blendShape);
                }
            }

            m_LastBlendShapeWeights = new Dictionary<string, float>();
            m_LastHeadRotation = Quaternion.identity;

            base.Start();
        }

        protected override void UpdateFaceMotion(FaceData data)
        {
            BoneTransformSnapshot.ApplyMany(m_FaceBoneInitialStates);
            RotateHead(data);

            Dictionary<string, BlendShapeAsset.BlendShapeData> blendShapeMap =
                m_FlipHorizontally ? m_BlendShapeMapHFlip : m_BlendShapeMap;

            foreach (var blendShape in data.BlendShapes)
            {
                if (!blendShapeMap.TryGetValue(blendShape.Name, out BlendShapeAsset.BlendShapeData blendShapeData))
                {
                    continue;
                }

                float lastWeight = m_LastBlendShapeWeights.GetValueOrDefault(blendShape.Name, 0);
                float weight = Mathf.Lerp(blendShape.Value, lastWeight, m_BlendShapeSmooth);
                m_LastBlendShapeWeights[blendShape.Name] = weight;

                BlendShapeUtility.ApplyBlendShape(blendShapeData, m_FaceRootBone, weight);
            }
        }

        private void RotateHead(FaceData data)
        {
            Quaternion rotation = ConvertRotation(data.HeadRotation, m_FlipHorizontally);
            rotation = Quaternion.Slerp(rotation, m_LastHeadRotation, m_HeadRotationSmooth);

            // 1. 用世界空间的旋转。
            // 2. 四元数乘法不满足交换律。
            m_FaceRootBone.rotation = rotation * m_FaceRootBone.rotation;
            m_LastHeadRotation = rotation;
        }

        private static Quaternion ConvertRotation(FaceData.Types.Quaternion rotation, bool mirrorHorizontally)
        {
            // Unity 是左手系。Y 轴向上，Z 轴向前，X 轴向右
            // mediapipe 是右手系。Y 轴向上，Z 轴向前，X 轴向左
            // rotation.w == cos(theta/2) 偶函数，不用管
            Quaternion result = new(rotation.X, -rotation.Y, -rotation.Z, rotation.W);

            if (mirrorHorizontally)
            {
                result.y *= -1;
                result.z *= -1;
            }

            return result;
        }
    }
}
