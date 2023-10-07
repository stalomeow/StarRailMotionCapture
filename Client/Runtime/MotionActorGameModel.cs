using System;
using System.Collections.Generic;
using UnityEngine;

namespace HSR.MotionCapture
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Honkai Star Rail/Motion Capture/Motion Actor (Game Model)")]
    public class MotionActorGameModel : MonoBehaviour
    {
        [SerializeField] private Avatar m_Avatar;
        [SerializeField] private SkinnedMeshRenderer m_FaceRenderer;
        [SerializeField] private bool m_FlipHorizontally = false;

        [Header("Face")]

        [SerializeField] private BlendShapeAsset m_BlendShapeData;
        [SerializeField, Range(0, 1)] private float m_BlendShapeSmooth = 0.1f;
        [SerializeField, Range(0, 1)] private float m_HeadRotationSmooth = 0.1f;

        [NonSerialized] private Transform m_FaceRootBone;
        [NonSerialized] private List<BoneTransformSnapshot> m_FaceBoneInitialStates;
        [NonSerialized] private Dictionary<string, BlendShapeAsset.BlendShapeData> m_BlendShapeMap;
        [NonSerialized] private Dictionary<string, BlendShapeAsset.BlendShapeData> m_BlendShapeMapHFlip;
        [NonSerialized] private Dictionary<string, float> m_LastBlendShapeWeights;
        [NonSerialized] private Quaternion m_LastHeadRotation;

        [NonSerialized] private Animator m_Animator;

        private void Start()
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

            m_Animator = GetComponent<Animator>();
        }

        public void ResetHeadAndFace()
        {
            BoneTransformSnapshot.ApplyMany(m_FaceBoneInitialStates);
        }

        public void RotateHead(Quaternion rotation)
        {
            if (m_FlipHorizontally)
            {
                rotation.y *= -1;
                rotation.z *= -1;
            }

            rotation = Quaternion.Slerp(rotation, m_LastHeadRotation, m_HeadRotationSmooth);

            // 1. 用世界空间的旋转。
            // 2. 四元数乘法不满足交换律。
            m_FaceRootBone.rotation = rotation * m_FaceRootBone.rotation;
            m_LastHeadRotation = rotation;
        }

        public void SetBlendShapeWeight(string blendShapeName, float value)
        {
            Dictionary<string, BlendShapeAsset.BlendShapeData> blendShapeMap =
                m_FlipHorizontally ? m_BlendShapeMapHFlip : m_BlendShapeMap;

            if (!blendShapeMap.TryGetValue(blendShapeName, out BlendShapeAsset.BlendShapeData blendShapeData))
            {
                return;
            }

            float lastWeight = m_LastBlendShapeWeights.GetValueOrDefault(blendShapeName, 0);
            float weight = Mathf.Lerp(value, lastWeight, m_BlendShapeSmooth);
            m_LastBlendShapeWeights[blendShapeName] = weight;

            BlendShapeUtility.ApplyBlendShape(blendShapeData, m_FaceRootBone, weight);
        }

        public Transform GetHumanBodyBone(HumanBodyBones boneId)
        {
            return m_Animator.GetBoneTransform(boneId);
        }
    }
}
