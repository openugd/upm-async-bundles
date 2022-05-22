using System.IO;
using OpenUGD.AsyncBundles.Bundles;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AssetBundleLoaderManagerEditor : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem(AssetsPresetUtils.MenuItem + "/AssetBundle Loading")]
        public static void Open()
        {
            GetWindow<AssetBundleLoaderManagerEditor>("AsyncBundleLoader").Show(true);
        }

        void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        void OnGUI()
        {
            if (!Application.isPlaying)
            {
                GUILayout.Label("Only in Play Mode");
                return;
            }


            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            AsyncBundleLoaderManager.MaxThreads =
                EditorGUILayout.IntSlider("MaxThreads", AsyncBundleLoaderManager.MaxThreads, 1, 1000);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var progress in AsyncBundleLoaderManager.Current)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(Path.GetFileNameWithoutExtension(progress.Path), GUILayout.MaxHeight(16f));
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 16f), progress.Progress,
                    (progress.Progress).ToString("P"));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var progress in AsyncBundleLoaderManager.Queue)
            {
                GUILayout.Label(Path.GetFileNameWithoutExtension(progress.Path));
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
}
