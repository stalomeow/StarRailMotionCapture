using System;
using System.Collections.Generic;
using UnityEngine;

namespace HSR.MotionCapture
{
    [CreateAssetMenu(menuName = "Honkai: StarRail/BlendShape")]
    public class BlendShapeAsset : ScriptableObject
    {
        [Serializable]
        public class BoneModification
        {
            public string BonePath = null;
            public Vector3 Translation = Vector3.zero;
            public Vector3 Rotation = Vector3.zero;
            public Vector3 Scale = Vector3.zero;
            public AnimationCurve Curve = AnimationCurve.Linear(0, 0, 1, 1);

            // Editor-Only States
#if UNITY_EDITOR
            public bool Editor_Foldout = false;
#endif
        }

        [Serializable]
        public class BlendShapeData
        {
            public string Name;
            public string Description;
            public List<BoneModification> BoneModifications = new();

            // Editor-Only States
#if UNITY_EDITOR
            public bool Editor_DescriptionFoldout = false;
#endif
        }

        public List<BlendShapeData> BlendShapes = new() { new BlendShapeData { Name = "Example" } };
    }
}
