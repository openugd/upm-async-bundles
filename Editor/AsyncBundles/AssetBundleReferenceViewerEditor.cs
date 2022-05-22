using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AssetBundleReferenceViewerEditor : EditorWindow
    {
        private readonly string DisplayUnusedKey = $"{nameof(AssetBundleReferenceViewerEditor)}.displayUnused";

        [MenuItem(AssetsPresetUtils.MenuItem + "/AssetBundle References")]
        public static void Open()
        {
            GetWindow<AssetBundleReferenceViewerEditor>("References").Show(true);
        }

        private static bool _showEmpty = false;
        private Vector2 _scroll;

        void OnEnable()
        {
            _showEmpty = EditorPrefs.GetBool(DisplayUnusedKey, false);
            EditorApplication.update += Repaint;
        }

        void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        void OnGUI()
        {
            if (!Application.isPlaying) return;

            EditorGUI.BeginChangeCheck();

            AssetEditorUtils.PushColor();
            GUI.color = Color.gray;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            GUILayout.Space(6f);
            _showEmpty = EditorGUILayout.Toggle("Display Unused", _showEmpty);
            EditorGUILayout.EndVertical();
            AssetEditorUtils.PopColor();
            EditorGUILayout.Separator();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var bundle in AsyncAssets.Bundles)
            {
                if (bundle.Refs.Count() != 0 || _showEmpty)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label($"Bundle: {bundle.Name}", EditorStyles.boldLabel);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    foreach (var bundleRef in bundle.Refs)
                    {
                        GUILayout.Label(bundleRef.ToString());
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Separator();
                }
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(DisplayUnusedKey, _showEmpty);
            }
        }
    }
}
