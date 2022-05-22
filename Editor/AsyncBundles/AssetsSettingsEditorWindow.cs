using System;
using OpenUGD.AsyncBundles.Manifests;
using OpenUGD.AsyncBundles.Presets;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AssetsSettingsEditorWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem(AssetsPresetUtils.MenuItem + "/Settings", priority = -2010)]
        public static void Open()
        {
            GetWindow<AssetsSettingsEditorWindow>("AsyncAssets Settings").Show(true);
        }

        [MenuItem(AssetsPresetUtils.MenuItem + "/Use AssetDatabase", priority = -3010)]
        public static void ToggleUseAssetDatabase()
        {
            AsyncAssets.Settings.SourceMode = AsyncAssets.SourceMode.AssetDatabase;
            Menu.SetChecked(AssetsPresetUtils.MenuItem + "/Use AssetDatabase", true);
        }

        [MenuItem(AssetsPresetUtils.MenuItem + "/Use AssetDatabase", true)]
        private static bool ToggleUseAssetDatabaseValidator()
        {
            Menu.SetChecked(AssetsPresetUtils.MenuItem + "/Use AssetDatabase",
                AsyncAssets.Settings.SourceMode == AsyncAssets.SourceMode.AssetDatabase);
            return true;
        }

        //
        [MenuItem(AssetsPresetUtils.MenuItem + "/Use LocalBundles", priority = -3010)]
        public static void ToggleUseLocalBundle()
        {
            AsyncAssets.Settings.SourceMode = AsyncAssets.SourceMode.LocalBundles;
            Menu.SetChecked(AssetsPresetUtils.MenuItem + "/Use LocalBundles", true);
        }

        [MenuItem(AssetsPresetUtils.MenuItem + "/Use LocalBundles", true)]
        private static bool ToggleUseLocalBundleValidator()
        {
            Menu.SetChecked(AssetsPresetUtils.MenuItem + "/Use LocalBundles",
                AsyncAssets.Settings.SourceMode == AsyncAssets.SourceMode.LocalBundles);
            return true;
        }

        [MenuItem(AssetsPresetUtils.MenuItem + "/Use ManifestBundles", priority = -3010)]
        public static void ToggleUseManifestBundles()
        {
            AsyncAssets.Settings.SourceMode = AsyncAssets.SourceMode.ManifestBundles;
            Menu.SetChecked(AssetsPresetUtils.MenuItem + "/Use ManifestBundles", true);
        }

        [MenuItem(AssetsPresetUtils.MenuItem + "/Use ManifestBundles", true)]
        private static bool ToggleUseManifestBundlesValidator()
        {
            Menu.SetChecked(AssetsPresetUtils.MenuItem + "/Use ManifestBundles",
                AsyncAssets.Settings.SourceMode == AsyncAssets.SourceMode.ManifestBundles);
            return true; //AsyncAssets.Settings.SourceMode != AsyncAssets.SourceMode.ManifestBundles;
        }

        void OnEnable()
        {
        }

        void OnDisable()
        {
        }

        string GetFileSize(long byteCount, bool bits = true)
        {
            int div = bits ? 8 : 1;
            string size = "0 Bytes";
            if (byteCount >= (1073741824 / div))
                size = $"{byteCount / (1073741824.0 / div):##.##}" + (bits ? " GBit" : " GByte");
            else if (byteCount >= (1048576.0 / div))
                size = $"{byteCount / (1048576.0 / div):##.##}" + (bits ? " MBit" : " MByte");
            else if (byteCount >= (1024.0 / div))
                size = $"{byteCount / (1024.0 / div):##.##}" + (bits ? " KBit" : " KByte");
            else if (byteCount > 0 && byteCount < (1024.0 / div))
                size = byteCount + (bits ? " Bits" : " Byte");

            return size;
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            OnGUISourceMode();

            EditorGUILayout.Separator();

            OnGUIAssetDatabaseTools();

            EditorGUILayout.Separator();

            OnGUIGroups();

            EditorGUILayout.Separator();

            OnGUIDebug();

            EditorGUILayout.Separator();

            OnGUITools();

            EditorGUILayout.EndScrollView();
        }

        void OnGUIAssetDatabaseTools()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("AssetDatabase", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            AsyncAssets.Settings.UseAssetDatabaseNetworkEmulation = EditorGUILayout.ToggleLeft("Network Emulation",
                AsyncAssets.Settings.UseAssetDatabaseNetworkEmulation);

            if (AsyncAssets.Settings.UseAssetDatabaseNetworkEmulation)
            {
                EditorGUILayout.Separator();
                GUILayout.Label(GetFileSize(AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond, true));
                GUILayout.Label(GetFileSize(AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond, false));
                AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond = EditorGUILayout.IntSlider(
                    AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond, 1024, 100 * 1024 * 1024);
                EditorGUILayout.Separator();
                var index = GUILayout.Toolbar(-1, new[] { "dial-up", "GPRS", "EDGE", "3G", "HSPA", "4G", "4G+" },
                    EditorStyles.miniButton);
                switch (index)
                {
                    //DialUp
                    case 0: AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond = 56 * 1024; break;
                    //GPRS
                    case 1:
                        AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond =
                            (int)(0.1 * 1024 * 1024) / 8; break;
                    //EDGE
                    case 2:
                        AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond =
                            (int)(0.3 * 1024 * 1024) / 8; break;
                    //3G
                    case 3:
                        AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond =
                            (int)(1.5 * 1024 * 1024) / 8; break;
                    //HSPA
                    case 4:
                        AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond =
                            (int)(4 * 1024 * 1024) / 8; break;
                    //4G
                    case 5:
                        AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond =
                            (int)(15 * 1024 * 1024) / 8; break;
                    //4G+
                    case 6:
                        AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond =
                            (int)(30 * 1024 * 1024) / 8; break;
                }
            }


            EditorGUILayout.EndVertical();
        }

        void OnGUITools()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Tools", EditorStyles.boldLabel);
            EditorGUILayout.Separator();
            if (GUILayout.Button("Fix Assets Name", EditorStyles.miniButton))
            {
                var preset = AssetsPresetUtils.Get();
                AssetsPresetUtils.FixAssetsName(preset);
            }

            if (GUILayout.Button("Check Recursion", EditorStyles.miniButton))
            {
                var manifest = Resources.Load<AssetManifestFile>(AsyncAssets.ResourcesManifestPath);
                if (manifest == null)
                {
                    EditorUtility.DisplayDialog("Error",
                        $"Manifest not found on path:{AsyncAssets.ResourcesManifestPath}",
                        "close");
                }
                else
                {
                    AssetManifestUtils.HasRecursion(manifest.Manifest, new Progress<AssetManifestUtils.Progress>(
                        (progress => {
                            EditorUtility.DisplayProgressBar("Check recursion", progress.Message, progress.Percent);
                            if (progress.IsRecursion)
                            {
                                EditorUtility.ClearProgressBar();
                                Debug.LogError(
                                    $"Recursion Found! {progress.Message}\n -> {string.Join(" \n -> ", progress.Bundles)}");
                                EditorUtility.DisplayDialog("Recursion Found!", progress.Message, "Close");
                            }
                        })));
                    EditorUtility.ClearProgressBar();
                }
            }

            EditorGUILayout.EndVertical();
        }

        void OnGUISourceMode()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Source Mode", EditorStyles.boldLabel);
            EditorGUILayout.Separator();
            AsyncAssets.Settings.SourceMode =
                (AsyncAssets.SourceMode)EditorGUILayout.EnumPopup("Source Mode", AsyncAssets.Settings.SourceMode);
            EditorGUILayout.HelpBox("\"Use AssetDatabase\" and \"Use Local Bundles\" take effect on start play",
                MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        void OnGUIGroups()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Groups", EditorStyles.boldLabel);
            GUILayout.Space(16f);
            AsyncAssets.Settings.DependencyResolverUseDisableGroups = EditorGUILayout.ToggleLeft(
                $"Dependency Resolver Use Do Not \"{nameof(AssetGroup.PackToBundle)}\" Groups",
                AsyncAssets.Settings.DependencyResolverUseDisableGroups);
            EditorGUILayout.EndVertical();
        }

        void OnGUIDebug()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Debug", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            var preset = AssetsPresetUtils.Get();
            if (GUILayout.Button("apply to bundle", EditorStyles.miniButtonLeft))
            {
                AssetsPresetUtils.UnCheckToBundles(preset);
                AssetsPresetUtils.CheckToBundles(preset);
            }

            if (GUILayout.Button("clear from bundles", EditorStyles.miniButtonRight))
            {
                AssetsPresetUtils.UnCheckToBundles(preset);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        class Progress<T> : IProgress<T>
        {
            private readonly Action<T> _action;

            public Progress(Action<T> action)
            {
                _action = action;
            }

            public void Report(T value)
            {
                _action(value);
            }
        }
    }
}
