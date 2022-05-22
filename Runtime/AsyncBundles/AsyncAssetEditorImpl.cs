using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUGD.AsyncBundles.Manifests;
using OpenUGD.AsyncBundles.Presets;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUGD.AsyncBundles
{
    public class AsyncAssetEditorImpl : IAsyncAssetImpl
    {
        private IManifestProvider _manifestProvider;

        public void Release(AsyncReference reference)
        {
            reference.Dispose();
        }

        public AsyncReference<List<AsyncReference<T>>> RetainByFilter<T>(AssetFilterFunc filter, object context = null)
            where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            var assets = new List<AssetPath>();
            if (GetManifest().TryGetAssets(filter, assets) && assets.Count != 0)
            {
                var progress = new AsyncReference.LoaderInfoProgressProvider();
                var reference = new AsyncReference<List<AsyncReference<T>>>(progress, context);
                var references = new List<AsyncReference<T>>();
                foreach (var assetPath in assets)
                {
                    references.Add(RetainByGuid<T>(assetPath.Guid, context));
                }

                foreach (var asyncReference in references)
                {
                    reference.Lifetime.AddAction(() => Release(asyncReference));
                }

                async void WaitToComplete()
                {
                    foreach (var asyncReference in references)
                    {
                        await asyncReference.Task;
                    }

                    if (!reference.Lifetime.IsTerminated)
                    {
                        AsyncReference<List<AsyncReference<T>>>.Resolve(reference, references);
                    }
                }

                WaitToComplete();
            }

            Debug.LogError($"{nameof(AsyncAssets)} no results found by filter, with context:{context}");
            return AsyncReference<List<AsyncReference<T>>>.Fail(
                new AsyncReference<List<AsyncReference<T>>>(new AsyncReference.LoaderInfoProgressProvider(), context));
#else
      return null;
#endif
        }

        public AsyncReference<List<AsyncReference<T>>> RetainByTag<T>(string tag, object context = null)
            where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            var assets = new List<AssetPath>();
            if (GetManifest().TryGetByTag(tag, assets) && assets.Count != 0)
            {
                var progress = new AsyncReference.LoaderInfoProgressProvider();
                var reference = new AsyncReference<List<AsyncReference<T>>>(progress, context);
                var references = new List<AsyncReference<T>>();
                foreach (var assetPath in assets)
                {
                    references.Add(RetainByGuid<T>(assetPath.Guid, context));
                }

                foreach (var asyncReference in references)
                {
                    reference.Lifetime.AddAction(() => Release(asyncReference));
                }

                async void WaitToComplete()
                {
                    foreach (var asyncReference in references)
                    {
                        await asyncReference.Task;
                    }

                    if (!reference.Lifetime.IsTerminated)
                    {
                        AsyncReference<List<AsyncReference<T>>>.Resolve(reference, references);
                    }
                }

                WaitToComplete();
            }

            Debug.LogError($"{nameof(AsyncAssets)} no results found by tag: {tag}, with context:{context}");
            return AsyncReference<List<AsyncReference<T>>>.Fail(
                new AsyncReference<List<AsyncReference<T>>>(new AsyncReference.LoaderInfoProgressProvider(), context));
#else
      return null;
#endif
        }

        public AsyncReference<T> RetainByName<T>(string name, object context = null) where T : Object
        {
#if UNITY_EDITOR

            if (GetManifest().TryGetByName(name, out var asset))
            {
                return RetainByGuid<T>(asset.Guid, context);
            }

            var progress = new AsyncReference.LoaderInfoProgressProvider();
            var reference = new AsyncReference<T>(progress, context);
            AsyncReference<T>.Fail(reference);
            Debug.LogError($"{nameof(AsyncAssets)} no results found by name: {name}, with context:{context}");
            return reference;
#else
      return null;
#endif
        }

        public AsyncReference<T> RetainByGuid<T>(string guid, object context = null) where T : Object
        {
#if UNITY_EDITOR
            var progress = new AsyncReference.ProgressProvider();
            var reference = new AsyncReference<T>(progress, context);
            T result = default;
            var isEmulation = false;
            if (GetManifest().TryGetByGuid(guid, out var path))
            {
                if (AsyncAssets.Settings.UseAssetDatabaseNetworkEmulation && Application.isPlaying)
                {
                    result = Load<T>(guid);

                    if (result != default)
                    {
                        IEnumerator Emulator()
                        {
                            var startTick = Environment.TickCount;
                            var speed = AsyncAssets.Settings.AssetDatabaseNetworkEmulationBytesPerSecond;
                            var totalBytes = CalculateSize(guid);
                            var totalSeconds = totalBytes / (double)speed;
                            progress.Progress = 0;
                            while (true)
                            {
                                yield return null;
                                var now = Environment.TickCount;
                                var passedSeconds = (now - startTick) / 1000.0;
                                if (passedSeconds >= totalSeconds)
                                {
                                    progress.Progress = 1f;
                                    AsyncReference<T>.Resolve(reference, result);
                                    yield break;
                                }

                                var ratio = passedSeconds / totalSeconds;
                                progress.Progress = (float)ratio;
                            }
                        }

                        CoroutineObject.Singleton().GetProvider().StartCoroutine(Emulator(), reference.Lifetime);
                    }
                }
                else
                {
                    result = Load<T>(guid);
                    if (result != default)
                    {
                        AsyncReference<T>.Resolve(reference, result);
                    }
                }
            }

            if (!isEmulation && result == default)
            {
                Debug.LogError($"{nameof(AsyncAssets)} no results found by guid: {guid}, with context:{context}");
                AsyncReference<T>.Fail(reference);
            }

            return reference;
#else
      return null;
#endif
        }

#if UNITY_EDITOR

        private static long CalculateSize(string guid)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                var totalSize = 0L;
                totalSize += new FileInfo(path).Length;
                foreach (var dependency in UnityEditor.AssetDatabase.GetDependencies(path, true))
                {
                    totalSize += new FileInfo(dependency).Length;
                }

                return totalSize;
            }

            return 0;
        }

        private static T Load<T>(string guid) where T : UnityEngine.Object
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var result = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            return result;
        }
#endif

        private IManifestProvider GetManifest()
        {
#if UNITY_EDITOR
            if (_manifestProvider == null)
            {
                var manifestRequest = Resources.Load<AssetManifestFile>(AsyncAssets.ResourcesManifestPath);
                _manifestProvider = new ManifestProvider(manifestRequest.Manifest);
            }
#else
            if (_manifestProvider == null)
            {
                var specialDirectories = AssetProviderSettings.Create();
                var manifestRequest = Resources.Load<AssetManifestFile>(AsyncAssets.ResourcesManifestPath);
                _manifestProvider = new AssetManifestProvider(manifestRequest.Manifest, specialDirectories);
            }
#endif
            return _manifestProvider;
        }

        public void ExecuteOnReady(Lifetime lifetime, Action<IManifestProvider> listener)
        {
            listener(GetManifest());
        }

        public bool IsReady => true;

#if UNITY_EDITOR
        class ManifestProvider : IManifestProvider
        {
            private AssetsPreset _preset;
            public long TotalSize { get; } = 0;
            public AssetsManifest Manifest { get; }

            public ManifestProvider(AssetsManifest manifest)
            {
                Manifest = manifest;
            }

            public bool TryGetByGuid(string guid, out AssetPath path)
            {
                foreach (var assetGroup in GetPreset().Groups)
                {
                    foreach (var asset in assetGroup.Assets)
                    {
                        if (asset.Guid == guid)
                        {
                            path = new AssetPath
                            {
                                Name = asset.Name,
                                Guid = asset.Guid,
                            };
                            return true;
                        }
                    }
                }

                path = default;
                return false;
            }

            public bool TryGetBundle(string bundleName, out Bundle bundle)
            {
                bundle = default;
                return false;
            }

            public bool TryGetByName(string name, out AssetPath path)
            {
                foreach (var assetGroup in GetPreset().Groups)
                {
                    foreach (var asset in assetGroup.Assets)
                    {
                        if (asset.Name == name)
                        {
                            path = new AssetPath
                            {
                                Name = asset.Name,
                                Guid = asset.Guid
                            };
                            return true;
                        }
                    }
                }

                path = default;
                return false;
            }

            public bool TryGetByTag(string tag, List<AssetPath> result)
            {
                var found = false;
                foreach (var assetGroup in GetPreset().Groups)
                {
                    foreach (var asset in assetGroup.Assets)
                    {
                        if (asset.Tags != null && asset.Tags.Contains(tag))
                        {
                            found = true;
                            result.Add(new AssetPath
                            {
                                Name = asset.Name,
                                Guid = asset.Guid,
                            });
                        }
                    }
                }

                return found;
            }

            public bool TryGetAssets(AssetFilterFunc filter, List<AssetPath> result)
            {
                var found = false;
                foreach (var assetGroup in GetPreset().Groups)
                {
                    foreach (var asset in assetGroup.Assets)
                    {
                        if (filter(new AssetFilter
                        {
                            Name = asset.Name,
                            Guid = asset.Guid,
                            Tags = asset.Tags
                        }))
                        {
                            found = true;
                            result.Add(new AssetPath
                            {
                                Name = asset.Name,
                                Guid = asset.Guid,
                            });
                        }
                    }
                }

                return found;
            }

            private AssetsPreset GetPreset()
            {
                if (_preset == null)
                {
                    var presets = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(AssetsPreset)}");
                    if (presets.Length == 0)
                    {
                        return null;
                    }

                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(presets[0]);
                    _preset = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetsPreset>(path);
                }

                return _preset;
            }
        }
#endif
    }
}
