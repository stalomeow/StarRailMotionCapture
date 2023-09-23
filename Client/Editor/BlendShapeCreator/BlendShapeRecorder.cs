using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using static HSR.MotionCapture.BlendShapeAsset;
using Object = UnityEngine.Object;

namespace HSR.MotionCapture.Editor.BlendShapeCreator
{
    internal sealed class BlendShapeRecorder : IDisposable
    {
        private SkinnedMeshRenderer m_Renderer;
        private BlendShapeData m_TargetBlendShape;

        private bool m_IsRecording = false;
        private readonly Dictionary<Transform, Dictionary<string, string>> m_RecordedInitialPropValues = new();
        private readonly Dictionary<Transform, Dictionary<string, string>> m_RecordedModifiedPropValues = new();
        private readonly Dictionary<int, List<UndoPropertyModification>> m_RecordedModificationHistory = new();

        private bool m_IsPreviewing = false;
        private float m_PreviewValue = 0;
        private readonly List<BoneTransformSnapshot> m_PreviewInitialBoneStates = new();

        public BlendShapeRecorder()
        {
            UnityInternalAPI.PrefabUtility.allowRecordingPrefabPropertyOverridesFor += AllowRecordingPrefabPropertyOverridesFor;
            Undo.postprocessModifications += PostProcessUndo;
            Undo.undoRedoEvent += UndoRedoEvent;
        }

        public void Dispose()
        {
            UnityInternalAPI.PrefabUtility.allowRecordingPrefabPropertyOverridesFor -= AllowRecordingPrefabPropertyOverridesFor;
            Undo.postprocessModifications -= PostProcessUndo;
            Undo.undoRedoEvent -= UndoRedoEvent;

            if (m_IsRecording)
            {
                EndRecording(save: false);
            }

            if (m_IsPreviewing)
            {
                EndPreviewing();
            }
        }

        #region Record

        public bool IsRecording => m_IsRecording;

        public void StartRecording(SkinnedMeshRenderer renderer, BlendShapeData targetBlendShape)
        {
            Assert.IsFalse(m_IsRecording);
            Assert.IsFalse(m_IsPreviewing);

            m_Renderer = renderer;
            m_TargetBlendShape = targetBlendShape;

            AnimationMode.StartAnimationMode();
            UnityInternalAPI.AnimationMode.StartAnimationRecording();

            m_IsRecording = true;

            // 还原之前的记录
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Restore Recordings");
            BlendShapeUtility.ApplyBlendShape(m_TargetBlendShape, m_Renderer.rootBone, 1,
                onWillModifyBoneCallback: (Transform bone) => Undo.RecordObject(bone, bone.name));
            Undo.IncrementCurrentGroup();
        }

        public bool EndRecording(bool save = true)
        {
            Assert.IsTrue(m_IsRecording);
            Assert.IsFalse(m_IsPreviewing);

            UnityInternalAPI.AnimationMode.StopAnimationRecording();
            AnimationMode.StopAnimationMode();

            bool isSaved;

            if (save && EditorUtility.DisplayDialog("Recorder", "Save Recordings?", "Yes", "No"))
            {
                m_TargetBlendShape.BoneModifications.Clear();
                m_TargetBlendShape.BoneModifications.AddRange(GetRecordedBoneModifications());
                isSaved = true;
            }
            else
            {
                isSaved = false;
            }

            m_Renderer = null;
            m_TargetBlendShape = null;
            m_RecordedInitialPropValues.Clear();
            m_RecordedModifiedPropValues.Clear();
            m_RecordedModificationHistory.Clear();
            m_IsRecording = false;
            return isSaved;
        }

        private bool AllowRecordingPrefabPropertyOverridesFor(Object componentOrGameObject)
        {
            if (!m_IsRecording)
            {
                return true;
            }

            GameObject go = componentOrGameObject as GameObject;

            if (go == null)
            {
                if (componentOrGameObject is not Component component)
                {
                    return true;

                }

                go = component.gameObject;
            }

            // If the input object is a child of the current root of animation then disallow recording of prefab property overrides
            // since the input object is currently being setup for animation recording
            return !go.transform.IsChildOf(m_Renderer.rootBone);
        }

        private UndoPropertyModification[] PostProcessUndo(UndoPropertyModification[] modifications)
        {
            if (!m_IsRecording)
            {
                return modifications;
            }

            ProcessPropertyModification(modifications,
                out UndoPropertyModification[] processedModifications,
                out UndoPropertyModification[] discardedModifications);

            // 记录 Undo 历史
            int undoGroup = Undo.GetCurrentGroup();
            if (!m_RecordedModificationHistory.TryGetValue(undoGroup, out var modificationList))
            {
                modificationList = new List<UndoPropertyModification>();
                m_RecordedModificationHistory.Add(undoGroup, modificationList);
            }
            modificationList.AddRange(processedModifications);

            return discardedModifications;
        }

        private void UndoRedoEvent(in UndoRedoInfo undo)
        {
            if (!m_IsRecording || !m_RecordedModificationHistory.TryGetValue(undo.undoGroup, out var modificationList))
            {
                return;
            }

            UndoPropertyModification[] modifications = modificationList.ToArray();

            // Undo 需要倒过来把 modifications 执行一遍
            if (!undo.isRedo)
            {
                // 交换 previousValue 和 currentValue
                for (int i = 0; i < modifications.Length; i++)
                {
                    ref UndoPropertyModification m = ref modifications[i];
                    (m.previousValue, m.currentValue) = (m.currentValue, m.previousValue);
                }

                Array.Reverse(modifications);
            }

            ProcessPropertyModification(modifications,
                out UndoPropertyModification[] processedModifications, out _);

            // ? 是否需要先 Apply 再 Process
            EditorBlendShapeUtility.ApplyBonePropertyModifications(processedModifications);
        }

        private void ProcessPropertyModification(
            UndoPropertyModification[] modifications,
            out UndoPropertyModification[] processedModifications,
            out UndoPropertyModification[] discardedModifications)
        {
            List<UndoPropertyModification> processedModificationsList = new();
            List<UndoPropertyModification> discardedModificationsList = new();

            foreach (UndoPropertyModification modification in modifications)
            {
                Transform rootBone = m_Renderer.rootBone;

                if (!EditorBlendShapeUtility.IsValidBonePropertyModification(modification, rootBone))
                {
                    discardedModificationsList.Add(modification);
                    continue;
                }
                processedModificationsList.Add(modification);

                PropertyModification prevValue = modification.previousValue;
                PropertyModification currValue = modification.currentValue;

                // 记录属性值
                Transform bone = (Transform)prevValue.target;
                AnimationUtility.PropertyModificationToEditorCurveBinding(prevValue,
                    bone.gameObject, out EditorCurveBinding binding); // Returns property type
                // 下面一行，不能用 AnimationMode.AddEditorCurveBinding(bone.gameObject, binding);
                AnimationMode.AddPropertyModification(binding, prevValue, modification.keepPrefabOverride);
                RecordPropertyModification(bone, prevValue.propertyPath, prevValue.value, currValue.value);
            }

            processedModifications = processedModificationsList.ToArray();
            discardedModifications = discardedModificationsList.ToArray();
        }

        private void RecordPropertyModification(Transform bone, string propertyPath, string prevValue, string currValue)
        {
            if (!m_RecordedInitialPropValues.TryGetValue(bone, out Dictionary<string, string> initialValueMap))
            {
                initialValueMap = new Dictionary<string, string>();
                m_RecordedInitialPropValues.Add(bone, initialValueMap);
            }

            // 记录属性的初始值
            if (!initialValueMap.TryGetValue(propertyPath, out string initialValue))
            {
                initialValue = prevValue;
                initialValueMap.Add(propertyPath, initialValue);
            }

            if (!m_RecordedModifiedPropValues.TryGetValue(bone, out Dictionary<string, string> modifiedValueMap))
            {
                modifiedValueMap = new Dictionary<string, string>();
                m_RecordedModifiedPropValues.Add(bone, modifiedValueMap);
            }

            if (initialValue == currValue)
            {
                // 如果当前值和初始值一样，就删掉记录
                modifiedValueMap.Remove(propertyPath);
            }
            else
            {
                modifiedValueMap[propertyPath] = currValue;
            }
        }

        private IEnumerable<BoneModification> GetRecordedBoneModifications()
        {
            Transform rootBone = m_Renderer.rootBone;

            foreach ((Transform bone, Dictionary<string, string> propValues) in m_RecordedModifiedPropValues)
            {
                if (propValues.Count == 0)
                {
                    continue;
                }

                yield return EditorBlendShapeUtility.CreateBoneModification(rootBone, bone, propValues);
            }
        }

        #endregion

        #region Preview

        public bool IsPreviewing => m_IsPreviewing;

        public float PreviewValue
        {
            get => m_PreviewValue;
            set
            {
                Assert.IsFalse(m_IsRecording);
                Assert.IsTrue(m_IsPreviewing);

                m_PreviewValue = Mathf.Clamp01(value);
                UpdatePreview();
            }
        }

        public void StartPreviewing(SkinnedMeshRenderer renderer, BlendShapeData targetBlendShape)
        {
            Assert.IsFalse(m_IsRecording);
            Assert.IsFalse(m_IsPreviewing);

            m_Renderer = renderer;
            m_TargetBlendShape = targetBlendShape;
            BoneTransformSnapshot.TakeMany(m_Renderer.bones, m_PreviewInitialBoneStates);

            AnimationMode.StartAnimationMode();
            m_IsPreviewing = true;
            UpdatePreview();
        }

        public void EndPreviewing()
        {
            Assert.IsFalse(m_IsRecording);
            Assert.IsTrue(m_IsPreviewing);

            AnimationMode.StopAnimationMode();

            m_Renderer = null;
            m_TargetBlendShape = null;
            m_PreviewInitialBoneStates.Clear();
            m_IsPreviewing = false;
        }

        private void UpdatePreview()
        {
            BoneTransformSnapshot.ApplyMany(m_PreviewInitialBoneStates);

            BlendShapeUtility.ApplyBlendShape(m_TargetBlendShape, m_Renderer.rootBone, m_PreviewValue,
                onWillModifyBoneCallback: (Transform bone) =>
                {
                    GameObject go = bone.gameObject;
                    BoneTransformSnapshot snapshot = new(bone);
                    foreach (PropertyModification modification in snapshot.ToPropertyModifications())
                    {
                        AnimationUtility.PropertyModificationToEditorCurveBinding(modification, go, out var binding);
                        AnimationMode.AddEditorCurveBinding(go, binding);
                    }
                });

            SceneView.RepaintAll();
        }

        #endregion
    }
}
