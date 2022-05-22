using System;
using System.Linq;
using OpenUGD.AsyncBundles.Presets;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [CustomEditor(typeof(AssetGroup)), CanEditMultipleObjects]
    public class AssetGroupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AssetEditorUtils.PushColor();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var packToBundleProperty = serializedObject.FindProperty(nameof(AssetGroup.PackToBundle));
            var packTypeProperty = serializedObject.FindProperty(nameof(AssetGroup.PackType));
            var unloadTypeProperty = serializedObject.FindProperty(nameof(AssetGroup.UnloadType));
            var delayToUnloadProperty = serializedObject.FindProperty(nameof(AssetGroup.DelayToUnload));
            var pathProperty = serializedObject.FindProperty(nameof(AssetGroup.Path));
            var assetProperty = serializedObject.FindProperty(nameof(AssetGroup.Assets));
            var conditionsProperty = serializedObject.FindProperty(nameof(AssetGroup.Conditions));

            if (targets.Length == 1)
            {
                AssetEditorUtils.PushColor(
                    AssetsPresetUtils.GetAssetGroupColor(AssetsPresetUtils.Get(), (AssetGroup) target));
                GUILayout.Label(target.name, EditorStyles.boldLabel);
                AssetEditorUtils.PopColor();
            }

            AssetEditorUtils.PushColor();
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            var homeButton = EditorGUIUtility.IconContent("Assembly Icon");
            homeButton.text = nameof(AssetsPreset);
            if (GUILayout.Button(homeButton, GUILayout.Height(32)))
            {
                Selection.activeObject = AssetsPresetUtils.Get();
            }

            AssetEditorUtils.PopColor();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(packToBundleProperty);
            EditorGUILayout.PropertyField(packTypeProperty);
            EditorGUILayout.PropertyField(unloadTypeProperty);
            EditorGUILayout.PropertyField(delayToUnloadProperty);

            AssetEditorUtils.PushIndentLevel(EditorGUI.indentLevel + 1);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(conditionsProperty, new GUIContent("Conditions")
            {
                tooltip = "Condition to build"
            }, true);
            EditorGUILayout.EndVertical();
            AssetEditorUtils.PopIndentLevel();

            AssetEditorUtils.PushColor();
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Build And Load");
            GUILayout.Space(16f);

            var groupPath = pathProperty.objectReferenceValue as AssetGroupPath;
            var groupPaths = AssetsPresetUtils.GetGroupPath();

            EditorGUI.showMixedValue = pathProperty.hasMultipleDifferentValues;
            var index = Array.IndexOf(groupPaths, groupPath);
            if (EditorGUI.showMixedValue) index = -1;
            EditorGUILayout.BeginHorizontal();
            if (index == -1)
                GUILayout.Box(
                    new GUIContent(EditorGUIUtility.IconContent("console.erroricon.sml").image, "not selected"),
                    EditorStyles.label, GUILayout.Width(16));
            var newIndex =
                EditorGUILayout.Popup(nameof(AssetGroup.Path), index, groupPaths.Select(s => s.name).ToArray());
            if (index != -1)
            {
                if (GUILayout.Button("SELECT", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    Selection.activeObject = groupPaths[index];
                }
            }

            var addPathContent = new GUIContent();
            addPathContent.tooltip = "New";
            if (GUILayout.Button(addPathContent, "OL Plus", GUILayout.Width(16f), GUILayout.Height(16f)))
            {
                AssetsPresetUtils.NewPath(AssetsPresetUtils.Get());
            }

            EditorGUILayout.EndHorizontal();
            if (index != newIndex)
            {
                pathProperty.objectReferenceValue = groupPaths[newIndex];
            }

            EditorGUI.showMixedValue = false;

            if (!pathProperty.hasMultipleDifferentValues)
            {
                if (groupPath != null)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.Space();
                    EditorGUILayout.TextField(nameof(AssetGroupPath.LoadPath), groupPath.LoadPath);
                    EditorGUILayout.TextField("Preview", AssetBuilder.ProcessPreviewPath(groupPath.LoadPath),
                        EditorStyles.label);
                    EditorGUILayout.Space();
                    EditorGUILayout.TextField(nameof(AssetGroupPath.BuildPath), groupPath.BuildPath);
                    EditorGUILayout.TextField("Preview", AssetBuilder.ProcessPreviewPath(groupPath.BuildPath),
                        EditorStyles.label);
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
            AssetEditorUtils.PopColor();

            AssetEditorUtils.PushIndentLevel(EditorGUI.indentLevel + 1);
            EditorGUILayout.PropertyField(assetProperty, true);
            AssetEditorUtils.PopIndentLevel();


            EditorGUILayout.EndVertical();

            AssetEditorUtils.PopColor();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
