using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [CustomEditor(typeof(AsyncPrefab)), CanEditMultipleObjects]
    public class AsyncPrefabEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var typeProperty = serializedObject.FindProperty(nameof(AsyncPrefab.Type));
            var referenceProperty = serializedObject.FindProperty(nameof(AsyncPrefab.Reference));
            var nameProperty = serializedObject.FindProperty(nameof(AsyncPrefab.Name));

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(typeProperty);
            EditorGUILayout.Separator();
            if (typeProperty.enumValueIndex == (int) AsyncPrefab.LinkType.Reference)
            {
                EditorGUILayout.PropertyField(referenceProperty);
            }
            else
            {
                EditorGUILayout.PropertyField(nameProperty);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.Separator();
                AssetEditorUtils.PushColor();
                GUI.color = Color.gray;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Editor Tools");
                EditorGUILayout.Separator();

                EditorGUILayout.BeginHorizontal();

                var loadContent = EditorGUIUtility.IconContent("Prefab Icon");
                loadContent.tooltip = "load/reload";
                loadContent.text = "load/reload";
                if (GUILayout.Button(loadContent, EditorStyles.miniButton, GUILayout.Width(80f), GUILayout.Height(16f)))
                {
                    foreach (var t in targets)
                    {
                        var aPrefab = t as AsyncPrefab;
                        if (aPrefab != null)
                        {
                            aPrefab.Load();
                        }
                    }
                }

                var unloadContent = EditorGUIUtility.IconContent("TreeEditor.Trash");
                unloadContent.tooltip = "unload";
                unloadContent.text = "unload";
                if (GUILayout.Button(unloadContent, EditorStyles.miniButton, GUILayout.Width(80f),
                    GUILayout.Height(16f)))
                {
                    foreach (var t in targets)
                    {
                        var aPrefab = t as AsyncPrefab;
                        if (aPrefab != null)
                        {
                            aPrefab.Unload();
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox("In Editor mode, you can not edit or save content", MessageType.Info);

                EditorGUILayout.EndVertical();
                AssetEditorUtils.PopColor();
            }
        }
    }
}
