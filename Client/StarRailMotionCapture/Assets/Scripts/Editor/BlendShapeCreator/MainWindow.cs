using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private int? SelectedBlendShapeIndex
        {
            get
            {
                var selection = m_BlendShapeNameGUIList.selectedIndices;
                return selection.Count > 0 ? selection[0] : null;
            }
            set
            {
                if (value == null)
                {
                    m_BlendShapeNameGUIList.ClearSelection();
                    return;
                }

                m_BlendShapeNameGUIList.Select(value.Value, append: false);
            }
        }

        private BlendShapeData SelectedBlendShape
        {
            get
            {
                int? index = SelectedBlendShapeIndex;

                if (index == null)
                {
                    return null;
                }

                List<BlendShapeData> blendShapes = m_Asset.BlendShapes;

                if (index.Value < 0 || index.Value >= blendShapes.Count)
                {
                    SelectedBlendShapeIndex = null;
                    return null;
                }

                return blendShapes[index.Value];
            }
        }

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

        private void OnEnable()
        {
            Undo.undoRedoEvent += OnUndoRedoEvent;
        }

        private void OnDisable()
        {
            Undo.undoRedoEvent -= OnUndoRedoEvent;

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

        private void OnUndoRedoEvent(in UndoRedoInfo undo)
        {
            Repaint();
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

            Undo.RecordObject(m_Asset, $"Modify {m_Asset.name} (Asset)");

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

            if (blendShape == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();

            // ----------------------------------------------------------------------------------------
            // Begin GUI
            // ----------------------------------------------------------------------------------------

            // Name Field
            using (new EditorGUILayout.HorizontalScope())
            {
                blendShape.Name = EditorGUILayout.TextField("Name", blendShape.Name); // Delayed 会出问题
                List<BlendShapeData> blendShapeList = m_Asset.BlendShapes;

                using (new EditorGUI.DisabledScope(blendShapeList.Count <= 1))
                {
                    if (GUILayout.Button("Delete", GUILayout.MaxWidth(80), GUILayout.Height(18)))
                    {
                        blendShapeList.Remove(blendShape);
                        SelectedBlendShapeIndex = blendShapeList.Count - 1;
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
            if (!m_BoneModificationGUILists.TryGetValue(blendShape, out ReorderableList boneModificationGUIList))
            {
                boneModificationGUIList = CreateBoneModificationGUIList(blendShape.BoneModifications);
                m_BoneModificationGUILists.Add(blendShape, boneModificationGUIList);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Controlled Bones ({blendShape.BoneModifications.Count})", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                // Tools
                Rect toolsRect = EditorGUILayout.GetControlRect(false, 18, GUILayout.MaxWidth(80));
                GUIContent toolsLabel = EditorGUIUtility.TrTextContent("Tools");
                if (EditorGUI.DropdownButton(toolsRect, toolsLabel, FocusType.Keyboard, Styles.Dropdown.Value))
                {
                    ShowToolsMenu(toolsRect, blendShape, boneModificationGUIList);
                }
            }

            ScrollView(ref m_ScrollPosRight, () =>
            {
                boneModificationGUIList.DoLayoutList();
                GUILayout.Space(10);
            });

            // ----------------------------------------------------------------------------------------
            // End GUI
            // ----------------------------------------------------------------------------------------

            if (EditorGUI.EndChangeCheck())
            {
                MarkAssetDirty();
            }
        }

        private void ShowToolsMenu(Rect toolsRect, BlendShapeData blendShape, ReorderableList boneModificationGUIList)
        {
            const string copyJsonPrefix = "BoneModifications:";
            List<BoneModification> selectedBoneModifications = boneModificationGUIList.selectedIndices.Select(
                i => blendShape.BoneModifications[i]).ToList();

            GenericMenu menu = new();

            menu.AddItem(new GUIContent("Copy All"), false, () =>
            {
                var wrapper = new BoneModificationListWrapper { BoneModifications = blendShape.BoneModifications };
                string json = EditorJsonUtility.ToJson(wrapper, false);
                EditorGUIUtility.systemCopyBuffer = copyJsonPrefix + json;
            });

            if (selectedBoneModifications.Count > 0)
            {
                menu.AddItem(new GUIContent("Copy Selected"), false, () =>
                {
                    var wrapper = new BoneModificationListWrapper { BoneModifications = selectedBoneModifications };
                    string json = EditorJsonUtility.ToJson(wrapper, false);
                    EditorGUIUtility.systemCopyBuffer = copyJsonPrefix + json;
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Copy Selected"));
            }

            if (EditorGUIUtility.systemCopyBuffer.StartsWith(copyJsonPrefix))
            {
                // 提前处理好，然后在 lambda 里捕获。避免之后 systemCopyBuffer 变掉。
                string json = EditorGUIUtility.systemCopyBuffer[copyJsonPrefix.Length..];

                menu.AddItem(new GUIContent("Paste and Overwrite"), false, () =>
                {
                    var wrapper = new BoneModificationListWrapper();
                    EditorJsonUtility.FromJsonOverwrite(json, wrapper);

                    Undo.RecordObject(m_Asset, "Paste Bone Modification List (Overwrite)");
                    blendShape.BoneModifications.Clear();
                    blendShape.BoneModifications.AddRange(wrapper.BoneModifications);
                    MarkAssetDirty();
                });

                menu.AddItem(new GUIContent("Paste and Append"), false, () =>
                {
                    var wrapper = new BoneModificationListWrapper();
                    EditorJsonUtility.FromJsonOverwrite(json, wrapper);

                    Undo.RecordObject(m_Asset, "Paste Bone Modification List (Append)");
                    blendShape.BoneModifications.AddRange(wrapper.BoneModifications);
                    MarkAssetDirty();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste and Overwrite"));
                menu.AddDisabledItem(new GUIContent("Paste and Append"));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Mirror All"), false, () =>
            {
                Undo.RecordObject(m_Asset, "Mirror Bone Modification List (All)");

                foreach (BoneModification boneModification in blendShape.BoneModifications)
                {
                    EditorBlendShapeUtility.MirrorBoneModification(boneModification, m_Renderer.rootBone);
                }

                MarkAssetDirty();
            });

            if (selectedBoneModifications.Count > 0)
            {
                menu.AddItem(new GUIContent("Mirror Selected"), false, () =>
                {
                    Undo.RecordObject(m_Asset, "Mirror Bone Modification List (Selected)");

                    foreach (BoneModification boneModification in selectedBoneModifications)
                    {
                        EditorBlendShapeUtility.MirrorBoneModification(boneModification, m_Renderer.rootBone);
                    }

                    MarkAssetDirty();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Mirror Selected"));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Clear All"), false, () =>
            {
                Undo.RecordObject(m_Asset, "Clear Bone Modification List");
                blendShape.BoneModifications.Clear();
                MarkAssetDirty();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Sort Ascending"), false, () =>
            {
                Undo.RecordObject(m_Asset, "Sort Bone Modification List (Ascending)");
                blendShape.BoneModifications.Sort(EditorBlendShapeUtility.CompareBoneModifications);
                MarkAssetDirty();
            });

            menu.AddItem(new GUIContent("Sort Descending"), false, () =>
            {
                Undo.RecordObject(m_Asset, "Sort Bone Modification List (Descending)");
                blendShape.BoneModifications.Sort(EditorBlendShapeUtility.CompareBoneModificationsReverse);
                MarkAssetDirty();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Collapse All"), false, () =>
            {
                Undo.RecordObject(m_Asset, "Collapse Bone Modification List");
                foreach (BoneModification modification in blendShape.BoneModifications)
                {
                    modification.Editor_Foldout = false;
                }
                MarkAssetDirty();
                Repaint();
            });

            menu.AddItem(new GUIContent("Expand All"), false, () =>
            {
                Undo.RecordObject(m_Asset, "Expand Bone Modification List");
                foreach (BoneModification modification in blendShape.BoneModifications)
                {
                    modification.Editor_Foldout = true;
                }
                MarkAssetDirty();
                Repaint();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Select All Bones in Hierarchy"), false, () =>
            {
                Transform rootBone = m_Renderer.rootBone;
                Selection.objects = (
                    from modification in blendShape.BoneModifications
                    let bone = BlendShapeUtility.FindBone(rootBone, modification.BonePath)
                    where bone != null
                    select (Object)bone.gameObject
                ).ToArray();
            });

            if (selectedBoneModifications.Count > 0)
            {
                menu.AddItem(new GUIContent("Select Selected-Bones in Hierarchy"), false, () =>
                {
                    Transform rootBone = m_Renderer.rootBone;
                    Selection.objects = (
                        from modification in selectedBoneModifications
                        let bone = BlendShapeUtility.FindBone(rootBone, modification.BonePath)
                        where bone != null
                        select (Object)bone.gameObject
                    ).ToArray();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Select Selected-Bones in Hierarchy"));
            }

            menu.DropDown(toolsRect);
        }

        private ReorderableList CreateBoneModificationGUIList(List<BoneModification> list)
        {
            return new ReorderableList(list, typeof(BoneModification), true, false, true, true)
            {
                multiSelect = true,
                elementHeightCallback = (int index) =>
                {
                    int lineCount = list[index].Editor_Foldout ? 5 : 1;
                    return lineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    BoneModification mod = list[index];

                    Rect line1 = new Rect(rect.x, rect.y + EditorGUIUtility.standardVerticalSpacing, rect.width, EditorGUIUtility.singleLineHeight);
                    Rect line2 = new Rect(line1) { y = line1.yMax + EditorGUIUtility.standardVerticalSpacing };
                    Rect line3 = new Rect(line2) { y = line2.yMax + EditorGUIUtility.standardVerticalSpacing };
                    Rect line4 = new Rect(line3) { y = line3.yMax + EditorGUIUtility.standardVerticalSpacing };
                    Rect line5 = new Rect(line4) { y = line4.yMax + EditorGUIUtility.standardVerticalSpacing };

                    mod.BonePath = BonePathFieldGUI(line1, "Bone", mod.BonePath, ref mod.Editor_Foldout);

                    if (mod.Editor_Foldout)
                    {
                        bool wideMode = EditorGUIUtility.wideMode;
                        EditorGUIUtility.wideMode = true;
                        EditorGUI.indentLevel++;

                        mod.Translation = EditorGUI.Vector3Field(line2, "Translation", mod.Translation);
                        mod.Rotation = EditorGUI.Vector3Field(line3, "Rotation", mod.Rotation);
                        mod.Scale = EditorGUI.Vector3Field(line4, "Scale", mod.Scale);
                        mod.Curve = EditorGUI.CurveField(line5, "Curve", mod.Curve);

                        EditorGUI.indentLevel--;
                        EditorGUIUtility.wideMode = wideMode;
                    }
                }
            };
        }

        private string BonePathFieldGUI(Rect rect, string label, string bonePath, ref bool foldout)
        {
            float whitespaceWidth = EditorGUIUtility.labelWidth * 0.4f; // 留一部分空白，用来选择列表元素
            Rect foldoutRect = new(rect) { width = EditorGUIUtility.labelWidth - whitespaceWidth };
            Rect fieldRect = new(rect) { xMin = foldoutRect.xMax + whitespaceWidth };
            Transform bone = BlendShapeUtility.FindBone(m_Renderer.rootBone, bonePath);

            // Foldout
            if (bonePath is { Length: > 0 } && bone == null)
            {
                label += " <color=red>(Missing!)</color>";
            }
            GUIContent labelContent = EditorGUIUtility.TrTextContent(label, $"Path: {bonePath}");
            foldout = EditorGUI.Foldout(foldoutRect, foldout, labelContent, true, Styles.RichFoldout.Value);

            // Object Field
            EditorGUI.BeginChangeCheck();
            Transform newBone = (Transform)EditorGUI.ObjectField(fieldRect, bone, typeof(Transform), true);
            if (EditorGUI.EndChangeCheck())
            {
                bonePath = BlendShapeUtility.GetBonePath(m_Renderer.rootBone, newBone);
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

        private static class Styles
        {
            public static readonly Lazy<GUIStyle> RichFoldout = new(
                () => new GUIStyle(EditorStyles.foldout) { richText = true });

            public static readonly Lazy<GUIStyle> Dropdown = new(
                () => new GUIStyle(EditorStyles.popup) { alignment = TextAnchor.MiddleCenter });
        }
    }
}
