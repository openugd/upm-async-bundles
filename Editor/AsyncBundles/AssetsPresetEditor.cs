using System.Collections.Generic;
using OpenUGD.AsyncBundles.Presets;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [CustomEditor(typeof(AssetsPreset))]
    public class AssetsPresetEditor : UnityEditor.Editor
    {
        private bool _needToBuild = false;
        private bool _needToClear = false;

        private HashSet<AssetGroup> _isExpand = new HashSet<AssetGroup>();

        void OnEnable()
        {
            _isExpand.Clear();
        }

        void OnDisable()
        {
            _isExpand.Clear();
        }

        public override void OnInspectorGUI()
        {
            _needToBuild = false;
            _needToClear = false;

            DrawHeaderTools();
            EditorGUILayout.Space();
            DrawItems();
            EditorGUILayout.Space();
            DrawNew();

            serializedObject.ApplyModifiedProperties();

            if (_needToBuild)
            {
                AssetEditorUtils.ExecuteInEditorUpdate(() => { AssetBuilder.Build((AssetsPreset) target); });
            }

            if (_needToClear)
            {
                AssetEditorUtils.ExecuteInEditorUpdate(AssetBuilder.Clear);
            }
        }

        private void DrawHeaderTools()
        {
            AssetEditorUtils.PushColor(new Color(0.7f, 0.7f, 0.7f));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Assets");
            EditorGUILayout.BeginHorizontal();
            AssetEditorUtils.PushColor(new Color(0.8f, 0.8f, 0.8f));
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("UnityEditor.FindDependencies"))
            {
                text = " Dependencies",
                tooltip = "Dependencies"
            }, EditorStyles.miniButtonLeft, GUILayout.Height(16f)))
            {
                AssetGroupDependencyEditorWindow.Open();
            }

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("EditCollider"))
            {
                text = " References",
                tooltip = "References"
            }, EditorStyles.miniButtonMid, GUILayout.Height(16f)))
            {
                AssetGroupReferencesEditorWindow.Open((AssetsPreset) target);
            }

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("SceneViewTools"))
            {
                text = "Settings",
                tooltip = "Settings"
            }, EditorStyles.miniButtonRight, GUILayout.Height(16f)))
            {
                AssetsSettingsEditorWindow.Open();
            }

            AssetEditorUtils.PopColor();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            var bundleCompressionProperty = serializedObject.FindProperty(nameof(AssetsPreset.BundleCompression));
            EditorGUILayout.PropertyField(bundleCompressionProperty);

            var numberOfParallelDownloadsProperty =
                serializedObject.FindProperty(nameof(AssetsPreset.NumberOfParallelDownloads));
            EditorGUILayout.PropertyField(numberOfParallelDownloadsProperty);

            var retryCountProperty = serializedObject.FindProperty(nameof(AssetsPreset.RetryCount));
            EditorGUILayout.PropertyField(retryCountProperty);

            var retryDelayProperty = serializedObject.FindProperty(nameof(AssetsPreset.RetryDelay));
            EditorGUILayout.PropertyField(retryDelayProperty);

            var timeoutProperty = serializedObject.FindProperty(nameof(AssetsPreset.Timeout));
            EditorGUILayout.PropertyField(timeoutProperty);

            EditorGUILayout.Space();

            var cleanButton = EditorGUIUtility.IconContent("TreeEditor.Trash");
            cleanButton.text = "Clear";

            var buildButton = EditorGUIUtility.IconContent("Assembly Icon");
            buildButton.text = "Build";

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(cleanButton, new GUIStyle(EditorStyles.miniButtonLeft) {fontStyle = FontStyle.Bold},
                GUILayout.Height(26)))
            {
                _needToClear = true;
            }

            if (GUILayout.Button(buildButton, new GUIStyle(EditorStyles.miniButtonRight) {fontStyle = FontStyle.Bold},
                GUILayout.Height(26)))
            {
                _needToBuild = true;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
            AssetEditorUtils.PopColor();
            EditorGUILayout.EndVertical();
        }

        private void DrawNew()
        {
            AssetEditorUtils.PushColor();

            var preset = (AssetsPreset) target;
            GUI.color = Color.gray;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("New Group");
            EditorGUILayout.Space();
            if (GUILayout.Button(EditorGUIUtility.IconContent("Toolbar Plus"), EditorStyles.miniButton))
            {
                AssetsPresetUtils.NewGroup(preset);
            }

            EditorGUILayout.EndVertical();

            AssetEditorUtils.PopColor();
        }

        private void DrawItems()
        {
            AssetEditorUtils.PushColor();
            var preset = (AssetsPreset) target;
            var indexToDelete = -1;
            for (var index = 0; index < preset.Groups.Length; index++)
            {
                var isExpand = false;
                var group = preset.Groups[index];
                if (group == null)
                {
                    AssetEditorUtils.PushColor();
                    GUI.color = Color.red;
                    if (GUILayout.Button("NULL --- Remove"))
                    {
                        indexToDelete = index;
                    }

                    AssetEditorUtils.PopColor();
                }
                else
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    AssetEditorUtils.PushColor();
                    //GUI.color = Color.gray;
                    if (GUILayout.Button("SELECT", EditorStyles.miniButtonLeft, GUILayout.Width(55)))
                    {
                        Selection.activeObject = group;
                    }

                    AssetEditorUtils.PopColor();
                    isExpand = _isExpand.Contains(group);
                    var expandContent = EditorGUIUtility.IconContent("animationvisibilitytoggleon");
                    expandContent.tooltip = "show/hide";
                    isExpand = GUILayout.Toggle(isExpand, expandContent, EditorStyles.miniButtonRight,
                        GUILayout.Width(24f), GUILayout.Height(16f));
                    if (isExpand)
                    {
                        _isExpand.Add(group);
                    }
                    else
                    {
                        _isExpand.Remove(group);
                    }

                    GUILayout.Box(
                        new GUIContent(
                            EditorGUIUtility.IconContent(group.PackToBundle ? "TestPassed" : "TestFailed").image,
                            "build to bundle"), EditorStyles.label, GUILayout.Height(16), GUILayout.Width(16));

                    var label = EditorGUIUtility.IconContent("Assembly Icon");
                    label.text = group.name;
                    AssetEditorUtils.PushColor(AssetsPresetUtils.GetAssetGroupColor(preset, group));
                    EditorGUILayout.ObjectField(label, group, typeof(AssetGroup), false);
                    AssetEditorUtils.PopColor();
                    EditorGUILayout.EndHorizontal();

                    if (isExpand)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        if (group != null)
                        {
                            AssetEditorUtils.PushColor(new Color(0.8f, 0.8f, 0.8f));
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            GUILayout.Space(2);
                            GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent("console.infoicon"))
                                {text = group.Path != null ? group.Path.name : " - - - "});
                            GUILayout.Space(2);
                            EditorGUILayout.EndVertical();
                            AssetEditorUtils.PopColor();
                            GUILayout.Space(4);
                            if (group.Assets == null || group.Assets.Length == 0)
                            {
                                GUILayout.Label("Empty");
                            }
                            else
                            {
                                foreach (var assetInfo in group.Assets)
                                {
                                    var path = AssetDatabase.GUIDToAssetPath(assetInfo.Guid);
                                    var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                                    var rect = EditorGUILayout.GetControlRect();
                                    AssetEditorUtils.RectLeft(ref rect, 10);
                                    var depRect = AssetEditorUtils.RectLeft(ref rect, 26);
                                    if (GUI.Button(depRect, new GUIContent("D") {tooltip = "Dependency"},
                                        EditorStyles.miniButtonLeft))
                                    {
                                        AssetDependencyEditorWindow.Open(new Object[] {asset});
                                    }

                                    EditorGUI.ObjectField(rect, asset, typeof(UnityEngine.Object), false);
                                }
                            }
                        }

                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            if (indexToDelete != -1)
            {
                Undo.RecordObject(preset, $"Delete NUll: {nameof(AssetGroup)}");

                var groups = preset.Groups;
                ArrayUtility.RemoveAt(ref groups, indexToDelete);
                preset.Groups = groups;
                AssetsPresetUtils.Save(preset);
            }

            AssetEditorUtils.PopColor();
        }
    }
}
