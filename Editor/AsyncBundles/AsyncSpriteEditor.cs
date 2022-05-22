using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [CustomEditor(typeof(AsyncImageSprite), true), CanEditMultipleObjects]
    public class AsyncImageSpriteEditor : AsyncSpriteEditor
    {
        protected override void OnInspectorGUIHeader()
        {
            var rendererProperty = serializedObject.FindProperty(nameof(AsyncImageSprite.Image));
            EditorGUILayout.PropertyField(rendererProperty);

            var setNativeSizeProperty = serializedObject.FindProperty(nameof(AsyncImageSprite.SetNativeSize));
            EditorGUILayout.PropertyField(setNativeSizeProperty);

            EditorGUILayout.Separator();
        }
    }

    [CustomEditor(typeof(AsyncSpriteRendererSprite), true), CanEditMultipleObjects]
    public class AsyncSpriteRendererSpriteEditor : AsyncSpriteEditor
    {
        protected override void OnInspectorGUIHeader()
        {
            var rendererProperty = serializedObject.FindProperty(nameof(AsyncSpriteRendererSprite.Renderer));
            EditorGUILayout.PropertyField(rendererProperty);
            EditorGUILayout.Separator();
        }
    }


    [CustomEditor(typeof(AsyncSprite), true), CanEditMultipleObjects]
    public class AsyncSpriteEditor : Editor
    {
        protected virtual void OnInspectorGUIHeader()
        {
        }

        protected virtual void OnInspectorGUIFooter()
        {
        }

        public override void OnInspectorGUI()
        {
            var typeProperty = serializedObject.FindProperty(nameof(AsyncSprite.Type));
            var referenceProperty = serializedObject.FindProperty(nameof(AsyncSprite.Reference));
            var nameProperty = serializedObject.FindProperty(nameof(AsyncSprite.Name));
            var restoreProperty = serializedObject.FindProperty(nameof(AsyncSprite.RestoreSprite));

            EditorGUI.BeginChangeCheck();
            OnInspectorGUIHeader();
            EditorGUILayout.PropertyField(restoreProperty);
            EditorGUILayout.PropertyField(typeProperty);
            EditorGUILayout.Separator();
            if (typeProperty.enumValueIndex == (int) AsyncSprite.LinkType.Reference)
            {
                EditorGUILayout.PropertyField(referenceProperty);
            }
            else
            {
                EditorGUILayout.PropertyField(nameProperty);
            }

            OnInspectorGUIFooter();
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
                        var aPrefab = t as AsyncSprite;
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
                        var aPrefab = t as AsyncSprite;
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
