using OpenUGD.AsyncBundles.Presets;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [CustomEditor(typeof(AssetGroupPath))]
    public class AssetGroupPathEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var buildProperty = serializedObject.FindProperty(nameof(AssetGroupPath.BuildPath));
            var localProperty = serializedObject.FindProperty(nameof(AssetGroupPath.LoadPath));

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Path");

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(localProperty);
            GUILayout.Label(AssetBuilder.ProcessPreviewPath(localProperty.stringValue));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(buildProperty);
            GUILayout.Label(AssetBuilder.ProcessPreviewPath(buildProperty.stringValue));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("replaces");
            EditorGUILayout.Space();
            AssetEditorUtils.PushIndentLevel(EditorGUI.indentLevel + 1);
            foreach (var property in AssetsPresetUtils.ReplaceProperties())
            {
                EditorGUILayout.TextField(property.Type.ToString(), property.Key);
            }

            AssetEditorUtils.PopIndentLevel();
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
