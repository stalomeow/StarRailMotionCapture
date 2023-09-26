using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using static HSR.MotionCapture.BlendShapeAsset;

namespace HSR.MotionCapture.Editor.BlendShapeCreator
{
    public sealed class MainWindow : EditorWindow
    {
        [OnOpenAsset]
        public static bool Open(int instanceID)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceID);

            if (asset is BlendShapeAsset blendShapeAsset)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                MainWindow window = GetWindow<MainWindow>($"BlendShapeEditor - {assetPath}");
                window.m_Asset = blendShapeAsset;
                return true;
            }

            return false;
        }

        [SerializeField] private BlendShapeAsset m_Asset;
        [SerializeField] private SkinnedMeshRenderer m_Renderer;
        [SerializeField] private Vector2 m_ScrollPosLeft;
        [SerializeField] private Vector2 m_ScrollPosRight;
        [SerializeField] private float m_LeftPanelWidth = 220;
        [SerializeField] private int m_SerializeOnly_SelectedBlendShapeIndex;

        [NonSerialized] private BlendShapeRecorder m_Recorder;
        [NonSerialized] private ReorderableList m_BlendShapeNameGUIList;
        [NonSerialized] private Dictionary<BlendShapeData, ReorderableList> m_BoneModificationGUILists;

        private int SelectedBlendShapeIndex
        {
            get => m_BlendShapeNameGUIList.index;
            set => m_BlendShapeNameGUIList.index = value;
        }

        private BlendShapeData SelectedBlendShape => m_Asset.BlendShapes[SelectedBlendShapeIndex];

        private void InitIfNot()
        {
            m_Recorder ??= new BlendShapeRecorder();
            m_BlendShapeNameGUIList ??= new ReorderableList(m_Asset.BlendShapes, typeof(BlendShapeData),
                true, false, false, false)
            {
                showDefaultBackground = false,
                index = m_SerializeOnly_SelectedBlendShapeIndex,
                elementHeight = EditorGUIUtility.singleLineHeight,
                drawElementCallback = (rect, index, active, focused) =>
                {
                    EditorGUI.LabelField(rect, m_Asset.BlendShapes[index].Name);
                }
            };
            m_BoneModificationGUILists ??= new Dictionary<BlendShapeData, ReorderableList>();
        }

        private void OnDisable()
        {
            if (m_Recorder != null)
            {
                m_Recorder.Dispose();
                m_Recorder = null;
            }

            if (m_BlendShapeNameGUIList != null)
            {
                m_SerializeOnly_SelectedBlendShapeIndex = m_BlendShapeNameGUIList.index;
            }
        }

        private void OnGUI()
        {
            InitIfNot();

            ToolbarGUI();

            if (m_Renderer == null)
            {
                EditorGUILayout.HelpBox("Please assign the 'Debug Skinned Mesh Renderer' field.", MessageType.Error);
                return;
            }

            using (new EditorGUI.DisabledScope(m_Recorder.IsRecording || m_Recorder.IsPreviewing))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(m_LeftPanelWidth)))
                    {
                        BlendShapeListGUI();
                    }

                    HorizontalSplitterGUI();

                    using (new EditorGUILayout.VerticalScope())
                    {
                        BlendShapeDataGUI();
                    }
                }
            }
        }

        private void ToolbarGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(m_Recorder.IsPreviewing))
                {
                    RecordButtonGUI();
                }

                using (new EditorGUI.DisabledScope(m_Recorder.IsRecording))
                {
                    PreviewButtonAndSliderGUI();
                }

                GUILayout.FlexibleSpace();
                ShowBlendShapeAssetButtonGUI();
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(m_Recorder.IsRecording || m_Recorder.IsPreviewing))
                {
                    FaceRendererFieldGUI();
                }
            }
        }

        private void RecordButtonGUI()
        {
            Color backupColor = GUI.color;
            GUI.color = m_Recorder.IsRecording ? AnimationMode.recordedPropertyColor : backupColor;

            try
            {
                EditorGUI.BeginChangeCheck();
                GUIContent icon = EditorGUIUtility.TrIconContent("Animation.Record");
                bool isRecording = GUILayout.Toggle(m_Recorder.IsRecording, icon, EditorStyles.toolbarButton);

                if (EditorGUI.EndChangeCheck())
                {
                    if (isRecording)
                    {
                        m_Recorder.StartRecording(m_Renderer, SelectedBlendShape);
                    }
                    else
                    {
                        if (m_Recorder.EndRecording())
                        {
                            MarkAssetDirty();
                        }
                    }
                }
            }
            finally
            {
                GUI.color = backupColor;
            }
        }

        private void PreviewButtonAndSliderGUI()
        {
            Color backupColor = GUI.color;
            GUI.color = m_Recorder.IsPreviewing ? AnimationMode.animatedPropertyColor : backupColor;

            try
            {
                EditorGUI.BeginChangeCheck();
                bool isPreviewing = GUILayout.Toggle(m_Recorder.IsPreviewing, "Preview", EditorStyles.toolbarButton);

                if (EditorGUI.EndChangeCheck())
                {
                    if (isPreviewing)
                    {
                        m_Recorder.StartPreviewing(m_Renderer, SelectedBlendShape);
                    }
                    else
                    {
                        m_Recorder.EndPreviewing();
                    }
                }

                using (new EditorGUI.DisabledScope(!m_Recorder.IsPreviewing))
                {
                    EditorGUI.BeginChangeCheck();
                    float previewValue = EditorGUILayout.Slider(m_Recorder.PreviewValue, 0, 1, GUILayout.MaxWidth(120), GUILayout.Height(16));

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_Recorder.PreviewValue = previewValue;
                    }
                }
            }
            finally
            {
                GUI.color = backupColor;
            }
        }

        private void ShowBlendShapeAssetButtonGUI()
        {
            if (GUILayout.Button("Show Asset", EditorStyles.toolbarButton))
            {
                PingAsset(true);
            }
        }

        private void FaceRendererFieldGUI()
        {
            EditorGUILayout.LabelField("Debug Skinned Mesh Renderer", EditorStyles.boldLabel, GUILayout.MaxWidth(190), GUILayout.Height(16));
            m_Renderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(m_Renderer, typeof(SkinnedMeshRenderer), true, GUILayout.MaxWidth(220), GUILayout.Height(16));
        }

        private void BlendShapeListGUI()
        {
            EditorGUILayout.LabelField($"Blend Shapes ({m_Asset.BlendShapes.Count})", EditorStyles.boldLabel);

            if (GUILayout.Button("Create New..."))
            {
                m_Asset.BlendShapes.Add(new BlendShapeData { Name = "New Blend Shape" });
                SelectedBlendShapeIndex =  m_Asset.BlendShapes.Count - 1;
                MarkAssetDirty();
            }

            EditorGUILayout.Space();

            ScrollView(ref m_ScrollPosLeft, () =>
            {
                EditorGUI.BeginChangeCheck();
                m_BlendShapeNameGUIList.DoLayoutList();

                if (EditorGUI.EndChangeCheck())
                {
                    MarkAssetDirty();
                }
            });
        }

        private void HorizontalSplitterGUI()
        {
            Rect dragRect = GetHorizontalSplitterDragRect();
            dragRect = UnityInternalAPI.EditorGUIUtility.HandleHorizontalSplitter(dragRect, position.width, 220, 400);
            UnityInternalAPI.EditorGUIUtility.DrawHorizontalSplitter(dragRect);
            SetHorizontalSplitterDragRect(dragRect);
        }

        private Rect GetHorizontalSplitterDragRect()
        {
            float toolbarHeight = EditorStyles.toolbar.fixedHeight * 2;
            return new Rect(m_LeftPanelWidth, toolbarHeight, 5, position.height - toolbarHeight);
        }

        private void SetHorizontalSplitterDragRect(Rect value)
        {
            m_LeftPanelWidth = value.x;
        }

        private void BlendShapeDataGUI()
        {
            BlendShapeData blendShape = SelectedBlendShape;
            int blendShapeIndex = SelectedBlendShapeIndex;

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();

            // ----------------------------------------------------------------------------------------
            // Begin GUI
            // ----------------------------------------------------------------------------------------

            // Name Field
            using (new EditorGUILayout.HorizontalScope())
            {
                blendShape.Name = EditorGUILayout.DelayedTextField("Name", blendShape.Name);
                List<BlendShapeData> blendShapeList = m_Asset.BlendShapes;

                using (new EditorGUI.DisabledScope(blendShapeList.Count <= 1))
                {
                    if (GUILayout.Button("Delete", GUILayout.MaxWidth(80), GUILayout.Height(18)))
                    {
                        blendShapeList.RemoveAt(blendShapeIndex);
                        SelectedBlendShapeIndex = Mathf.Clamp(blendShapeIndex, 0, blendShapeList.Count - 1);
                        MarkAssetDirty();

                        m_BoneModificationGUILists.Remove(blendShape);
                        return;
                    }
                }
            }

            // Description Field
            using (new EditorGUILayout.HorizontalScope())
            {
                bool foldout = blendShape.Editor_DescriptionFoldout;

                if (foldout)
                {
                    EditorGUILayout.LabelField("Description");
                }
                else
                {
                    string[] descLines = blendShape.Description?.Split('\n', '\r');
                    string descPreview = descLines is { Length: > 0 } ? descLines[0] : string.Empty;
                    GUIContent label = EditorGUIUtility.TrTextContent("Description");
                    GUIContent content = EditorGUIUtility.TrTextContent(descPreview + "...", blendShape.Description);
                    EditorGUILayout.LabelField(label, content);
                }

                if (GUILayout.Button(foldout ? "Complete" : "Edit", GUILayout.MaxWidth(80), GUILayout.Height(18)))
                {
                    blendShape.Editor_DescriptionFoldout = !foldout;
                }
            }

            if (blendShape.Editor_DescriptionFoldout)
            {
                ScrollView(ref m_ScrollPosRight, () =>
                {
                    blendShape.Description = EditorGUILayout.TextArea(blendShape.Description);
                });
                return;
            }

            GUILayout.Space(10);

            // BoneModifications List
            if (m_Renderer != null)
            {
                if (!m_BoneModificationGUILists.TryGetValue(blendShape, out ReorderableList boneModificationGUIList))
                {
                    boneModificationGUIList = CreateBoneModificationGUIList(blendShape.BoneModifications);
                    m_BoneModificationGUILists.Add(blendShape, boneModificationGUIList);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Controlled Bones ({blendShape.BoneModifications.Count})", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    const string jsonPrefix = "BoneModifications:";

                    if (GUILayout.Button("Copy", GUILayout.MaxWidth(60), GUILayout.Height(18)))
                    {
                        BoneModificationListWrapper wrapper = new() { BoneModifications = blendShape.BoneModifications };
                        string json = EditorJsonUtility.ToJson(wrapper, false);
                        EditorGUIUtility.systemCopyBuffer = jsonPrefix + json;
                    }

                    if (GUILayout.Button("Paste", GUILayout.MaxWidth(60), GUILayout.Height(18)))
                    {
                        string json = EditorGUIUtility.systemCopyBuffer;

                        if (json.StartsWith(jsonPrefix))
                        {
                            json = json[jsonPrefix.Length..];
                            BoneModificationListWrapper wrapper = new();
                            EditorJsonUtility.FromJsonOverwrite(json, wrapper);

                            Undo.RecordObject(m_Asset, "Paste Bone Modification List");
                            blendShape.BoneModifications.Clear();
                            blendShape.BoneModifications.AddRange(wrapper.BoneModifications);
                            MarkAssetDirty();
                        }
                        else
                        {
                            Debug.LogError("Failed to paste.");
                        }
                    }

                    if (GUILayout.Button("Clear", GUILayout.MaxWidth(60), GUILayout.Height(18)))
                    {
                        Undo.RecordObject(m_Asset, "Clear Bone Modification List");
                        blendShape.BoneModifications.Clear();
                        MarkAssetDirty();
                    }

                    if (GUILayout.Button("Mirror", GUILayout.MaxWidth(60), GUILayout.Height(18)))
                    {
                        Transform rootBone = m_Renderer.rootBone;
                        Quaternion rootRotInv = Quaternion.Inverse(rootBone.root.localRotation);

                        Undo.RecordObject(m_Asset, "Mirror Bone Modification List");

                        foreach (BoneModification boneModification in SelectedBlendShape.BoneModifications)
                        {
                            string prevBonePath = boneModification.BonePath;
                            if (prevBonePath == null)
                            {
                                continue;
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
                            boneModification.BonePath = string.Join('/', paths);

                            Transform prevBone = BlendShapeUtility.FindBone(rootBone, prevBonePath);
                            Transform currBone = BlendShapeUtility.FindBone(rootBone, boneModification.BonePath);

                            Quaternion prevRot = rootRotInv * prevBone.parent.rotation;
                            // Quaternion prevRotInv = Quaternion.Inverse(prevRot);
                            Quaternion currRot = rootRotInv * currBone.parent.rotation;
                            Quaternion currRotInv = Quaternion.Inverse(currRot);

                            // 镜像的时候，先从旧的骨骼父空间变换到模型的本地空间，再镜像 X 轴，然后变换回新的骨骼父空间

                            // Translation
                            Vector3 translationMS = prevRot * boneModification.Translation;
                            translationMS.x *= -1;
                            boneModification.Translation = currRotInv * translationMS;

                            // Rotation
                            Quaternion.Euler(boneModification.Rotation).ToAngleAxis(out float angle, out Vector3 axis);
                            axis = currRotInv * Vector3.Scale(prevRot * axis, new Vector3(-1, 1, 1));
                            angle *= -1;
                            boneModification.Rotation = Quaternion.AngleAxis(angle, axis).eulerAngles;

                            // Scale
                            Vector3 scaleMS = prevRot * boneModification.Scale;
                            scaleMS.x *= -1;
                            boneModification.Scale = currRotInv * scaleMS;
                        }

                        MarkAssetDirty();
                    }
                }

                ScrollView(ref m_ScrollPosRight, () =>
                {
                    boneModificationGUIList.DoLayoutList();
                    GUILayout.Space(10);
                });
            }

            // ----------------------------------------------------------------------------------------
            // End GUI
            // ----------------------------------------------------------------------------------------

            if (EditorGUI.EndChangeCheck())
            {
                MarkAssetDirty();
            }
        }

        private ReorderableList CreateBoneModificationGUIList(List<BlendShapeAsset.BoneModification> list)
        {
            return new ReorderableList(list, typeof(BlendShapeAsset.BoneModification), true, false, true, true)
            {
                elementHeightCallback = (int index) =>
                {
                    int lineCount = list[index].Editor_Foldout ? 6 : 2;
                    return lineCount * EditorGUIUtility.singleLineHeight
                           + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    BoneModification mod = list[index];

                    Rect line1 = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                    Rect line2 = new Rect(line1) { y = line1.yMax + EditorGUIUtility.standardVerticalSpacing };
                    Rect line3 = new Rect(line2) { y = line2.yMax + EditorGUIUtility.standardVerticalSpacing };
                    Rect line4 = new Rect(line3) { y = line3.yMax + EditorGUIUtility.standardVerticalSpacing };
                    Rect line5 = new Rect(line4) { y = line4.yMax + EditorGUIUtility.standardVerticalSpacing };
                    Rect line6 = new Rect(line5) { y = line5.yMax + EditorGUIUtility.standardVerticalSpacing };

                    mod.BonePath = BonePathFieldGUI(line1, "Bone", mod.BonePath);
                    bool foldout = EditorGUI.Foldout(line2, list[index].Editor_Foldout, "Modifications", true);
                    list[index].Editor_Foldout = foldout;

                    if (foldout)
                    {
                        bool wideMode = EditorGUIUtility.wideMode;
                        EditorGUIUtility.wideMode = true;

                        using (new EditorGUI.IndentLevelScope())
                        {
                            mod.Translation = EditorGUI.Vector3Field(line3, "Translation", mod.Translation);
                            mod.Rotation = EditorGUI.Vector3Field(line4, "Rotation", mod.Rotation);
                            mod.Scale = EditorGUI.Vector3Field(line5, "Scale", mod.Scale);
                            mod.Curve = EditorGUI.CurveField(line6, "Curve", mod.Curve);
                        }

                        EditorGUIUtility.wideMode = wideMode;
                    }
                }
            };
        }

        private string BonePathFieldGUI(Rect rect, string label, string bonePath)
        {
            EditorGUI.BeginChangeCheck();
            Transform bone = BlendShapeUtility.FindBone(m_Renderer.rootBone, bonePath);
            Transform newBone = (Transform)EditorGUI.ObjectField(rect, label, bone, typeof(Transform), true);

            if (EditorGUI.EndChangeCheck())
            {
                return BlendShapeUtility.GetBonePath(m_Renderer.rootBone, newBone);
            }

            return bonePath;
        }

        private void MarkAssetDirty()
        {
            // Debug.Log("Dirty");
            EditorUtility.SetDirty(m_Asset);
        }

        private void PingAsset(bool select)
        {
            EditorGUIUtility.PingObject(m_Asset);

            if (select)
            {
                Selection.activeObject = m_Asset;
            }
        }

        private static void ScrollView(ref Vector2 scrollPos, [NotNull] Action guiAction)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            guiAction();
            EditorGUILayout.EndScrollView();
        }

        [Serializable]
        private class BoneModificationListWrapper
        {
            public List<BoneModification> BoneModifications;
        }
    }
}
