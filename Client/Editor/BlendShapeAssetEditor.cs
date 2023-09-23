using UnityEditor;

namespace HSR.MotionCapture.Editor
{
    [CustomEditor(typeof(BlendShapeAsset))]
    internal class BlendShapeAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BlendShapeAsset asset = target as BlendShapeAsset;
            EditorGUILayout.LabelField("Blend Shape Count", asset.BlendShapes.Count.ToString());
        }
    }
}
