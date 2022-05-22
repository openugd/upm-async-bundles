using System;
using System.Collections.Generic;
using System.IO;
using OpenUGD.AsyncBundles.Bundles;
using OpenUGD.AsyncBundles.Manifests;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AsyncAssets
    {
        public enum SourceMode
        {
            AssetDatabase = 0,
            LocalBundles = 1,
            ManifestBundles = 2,
        }

        public static string ResourcesManifestPath => nameof(AssetManifestFile);

        private static AsyncAssetImpl _impl;
        private static AsyncAssetEditorImpl _editorImpl;

        private static AsyncAssetImpl RuntimeImpl => _impl ?? (_impl = new AsyncAssetImpl());
        private static AsyncAssetEditorImpl EditorImpl => _editorImpl ?? (_editorImpl = new AsyncAssetEditorImpl());
        private static bool IsRuntime => Application.isPlaying && Settings.SourceMode != SourceMode.AssetDatabase;

        private static IAsyncAssetImpl Impl {
            get {
                if (IsRuntime)
                {
                    return RuntimeImpl;
                }

                return EditorImpl;
            }
        }

        public static IEnumerable<AsyncBundle> Bundles => RuntimeImpl.Bundles;

        public static bool IsReady => Impl.IsReady;

        public static void ExecuteOnReady(Lifetime lifetime, Action<IManifestProvider> listener)
        {
            Impl.ExecuteOnReady(lifetime, listener);
        }

        public static void Release(AsyncReference reference)
        {
            Impl.Release(reference);
        }

        public static AsyncReference<List<AsyncReference<T>>> RetainByFilter<T>(AssetFilterFunc filter,
            object context = null) where T : UnityEngine.Object
        {
            return Impl.RetainByFilter<T>(filter, context);
        }

        public static AsyncReference<List<AsyncReference<T>>> RetainByTag<T>(string tag, object context = null)
            where T : UnityEngine.Object
        {
            return Impl.RetainByTag<T>(tag, context);
        }

        public static AsyncReference<T> RetainByName<T>(string name, object context = null) where T : UnityEngine.Object
        {
            return Impl.RetainByName<T>(name, context);
        }

        public static AsyncReference<T> RetainByGuid<T>(string guid, object context = null) where T : UnityEngine.Object
        {
            return Impl.RetainByGuid<T>(guid, context);
        }

        public static class Settings
        {
            private const string SourceModePrefName = nameof(AsyncAssets) + ".sourceMode";
            private const string UseAssetDatabasePrefName = nameof(AsyncAssets) + ".useAssetDatabase";

            private const string UseAssetDatabaseNetworkEmulationPrefName =
                nameof(AsyncAssets) + ".useAssetDatabaseNetworkEmulation";

            private const string AssetDatabaseNetworkEmulationSpeedPrefName =
                nameof(AsyncAssets) + ".assetDatabaseNetworkEmulationSpeed";

            private const string UseLocalBundlesPrefName = nameof(AsyncAssets) + ".useLocalBundles";

            private const string DependencyResolverUseDisableGroupsPrefName =
                nameof(AsyncAssets) + ".dependencyResolverUseDisableGroups";

            private static string _buildPath;

            public static string BuildPath {
                get {
                    if (_buildPath == null)
                    {
                        var dir = new DirectoryInfo(Application.dataPath).Parent;
                        _buildPath = $"{dir.FullName}/Library/AssetBuilder/Bundles";
                    }

                    return _buildPath;
                }
            }

            public static SourceMode SourceMode {
                get {
#if UNITY_EDITOR
                    return (SourceMode)UnityEditor.EditorPrefs.GetInt(SourceModePrefName);
#else
                    return SourceMode.ManifestBundles;
#endif
                }
                set {
#if UNITY_EDITOR
                    UnityEditor.EditorPrefs.SetInt(SourceModePrefName, (int)value);
#endif
                }
            }

            public static bool UseAssetDatabaseNetworkEmulation {
                set {
#if UNITY_EDITOR
                    UnityEditor.EditorPrefs.SetBool(UseAssetDatabaseNetworkEmulationPrefName, value);
#endif
                }
                get {
#if UNITY_EDITOR
                    return UnityEditor.EditorPrefs.GetBool(UseAssetDatabaseNetworkEmulationPrefName);
#else
                    return false;
#endif
                }
            }

            public static int AssetDatabaseNetworkEmulationBytesPerSecond {
                set {
#if UNITY_EDITOR
                    UnityEditor.EditorPrefs.SetInt(AssetDatabaseNetworkEmulationSpeedPrefName, value);
#endif
                }
                get {
#if UNITY_EDITOR
                    return UnityEditor.EditorPrefs.GetInt(AssetDatabaseNetworkEmulationSpeedPrefName, 0);
#else
          return 0;
#endif
                }
            }

            public static bool DependencyResolverUseDisableGroups {
                set {
#if UNITY_EDITOR
                    UnityEditor.EditorPrefs.SetBool(DependencyResolverUseDisableGroupsPrefName, value);
#endif
                }
                get {
#if UNITY_EDITOR
                    return UnityEditor.EditorPrefs.GetBool(DependencyResolverUseDisableGroupsPrefName);
#else
          return false;
#endif
                }
            }
        }
    }
}
