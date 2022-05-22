using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenUGD.AsyncBundles.Bundles;
using OpenUGD.AsyncBundles.Manifests;
using OpenUGD.AsyncBundles.Presets;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AsyncAssetImpl : IAsyncAssetImpl
    {
        private readonly AsyncBundleManager _bundleManager;
        private readonly Lifetime _lifetime;
        private readonly ICoroutineProvider _coroutineProvider;
        private readonly Signal<IManifestProvider> _onReady;
        private readonly AsyncAssetsCaching _caching;
        private AssetManifestProvider _manifestProvider;
        private bool _isReady;
#if !UNITY_WEBGL
        private Thread _startThread;
#endif

        public AsyncAssetImpl()
        {
            _lifetime = Lifetime.Eternal.DefineNested(nameof(AsyncAssets)).Lifetime;
            var readyDef = _lifetime.DefineNested();
            _onReady = new Signal<IManifestProvider>(readyDef.Lifetime);
            _coroutineProvider = CoroutineObject.Singleton().GetProvider();

            _caching = new AsyncAssetsCaching(_lifetime, _coroutineProvider);
            _bundleManager = new AsyncBundleManager(_lifetime, _coroutineProvider);
            var syncContext = SynchronizationContext.Current;
            var manifestRequest = Resources.LoadAsync<AssetManifestFile>(AsyncAssets.ResourcesManifestPath);
            manifestRequest.completed += operation =>
            {
                var keep = new KeepReference(_lifetime);
                keep.AddAction(() =>
                {
                    if (!_lifetime.IsTerminated)
                    {
                        _isReady = true;
                        _onReady.Fire(_manifestProvider);
                        readyDef.Terminate();
                    }
                });

                var assetProviderDef = keep.Keep();
                var cachingDef = keep.Keep();

                var assetManifestFile = (AssetManifestFile) manifestRequest.asset;
                var manifest = assetManifestFile.Manifest;

                AsyncBundleLoaderManager.MaxThreads = manifest.NumberOfParallelDownloads;
                AsyncBundleLoaderManager.RetryCount = manifest.RetryCount;
                AsyncBundleLoaderManager.RetryDelay = manifest.RetryDelay;
                AsyncBundleLoaderManager.Timeout = manifest.Timeout;

                var specialDirectories = AssetProviderSettings.Create();
#if UNITY_WEBGL
                _manifestProvider = new AssetManifestProvider(manifest, specialDirectories);
                assetProviderDef.Terminate();
#else
                _startThread = new Thread(() =>
                {
                    var provider = new AssetManifestProvider(manifest, specialDirectories);
                    syncContext.Post((syncState) =>
                    {
                        _manifestProvider = provider;
                        _startThread = null;
                        assetProviderDef.Terminate();
                    }, null);
                })
                {
                    IsBackground = true
                };
                _startThread.Start();
#endif


                _caching.ExecuteOnReady(_lifetime, () => cachingDef.Terminate());
            };
        }

        public IEnumerable<AsyncBundle> Bundles => _bundleManager.Bundles;
        public bool IsReady => _isReady;

        public void Release(AsyncReference reference)
        {
            reference.Dispose();
        }

        public AsyncReference<List<AsyncReference<T>>> RetainByFilter<T>(AssetFilterFunc filter, object context = null)
            where T : UnityEngine.Object
        {
            var progressProvider = new AsyncReference.GroupProgressProvider();
            var reference = new AsyncReference<List<AsyncReference<T>>>(progressProvider, context);
            ExecuteOnReady(reference.Lifetime, manifest =>
            {
                var references = new List<AsyncReference<T>>();
                var assetPaths = new List<AssetPath>();
                if (manifest.TryGetAssets(filter, assetPaths) && assetPaths.Count != 0)
                {
                    foreach (var assetPath in assetPaths)
                    {
                        references.Add(RetainByGuid<T>(assetPath.Guid, context));
                    }

                    progressProvider.Providers = references.Cast<IProgressProvider>().ToList();
                    reference.Lifetime.AddAction(() =>
                    {
                        foreach (var asyncReference in references)
                        {
                            Release(asyncReference);
                        }
                    });

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
                else
                {
                    Debug.LogError($"{nameof(AsyncAssets)} no results found by filter, with context:{context}");
                    AsyncReference<List<AsyncReference<T>>>.Fail(reference);
                }
            });
            return reference;
        }

        public AsyncReference<List<AsyncReference<T>>> RetainByTag<T>(string tag, object context = null)
            where T : UnityEngine.Object
        {
            var progressProvider = new AsyncReference.GroupProgressProvider();
            var reference = new AsyncReference<List<AsyncReference<T>>>(progressProvider, context);
            ExecuteOnReady(reference.Lifetime, manifest =>
            {
                var references = new List<AsyncReference<T>>();
                var assetPaths = new List<AssetPath>();
                if (manifest.TryGetByTag(tag, assetPaths) && assetPaths.Count != 0)
                {
                    foreach (var assetPath in assetPaths)
                    {
                        references.Add(RetainByGuid<T>(assetPath.Guid, context));
                    }

                    progressProvider.Providers = references.Cast<IProgressProvider>().ToList();
                    reference.Lifetime.AddAction(() =>
                    {
                        foreach (var asyncReference in references)
                        {
                            Release(asyncReference);
                        }
                    });

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
                else
                {
                    Debug.LogError($"{nameof(AsyncAssets)} no results found by tag: {tag}, with context:{context}");
                    AsyncReference<List<AsyncReference<T>>>.Fail(reference);
                }
            });
            return reference;
        }

        public AsyncReference<T> RetainByName<T>(string name, object context = null) where T : UnityEngine.Object
        {
            var progressProvider = new AsyncReference.LoaderInfoProgressProvider();
            var reference = new AsyncReference<T>(progressProvider, context);
            ExecuteOnReady(reference.Lifetime, manifest =>
            {
                if (_manifestProvider.TryGetByName(name, out var asset))
                {
                    Process(reference, progressProvider, asset, context);
                }
                else
                {
                    Debug.LogError($"{nameof(AsyncAssets)} Path to name:{name} not found, with context:{context}");
                    AsyncReference<T>.Fail(reference);
                }
            });
            return reference;
        }

        public AsyncReference<T> RetainByGuid<T>(string guid, object context = null) where T : UnityEngine.Object
        {
            var progressProvider = new AsyncReference.LoaderInfoProgressProvider();
            var reference = new AsyncReference<T>(progressProvider, context);
            ExecuteOnReady(reference.Lifetime, (manifest) =>
            {
                if (_manifestProvider.TryGetByGuid(guid, out var asset))
                {
                    Process(reference, progressProvider, asset, context);
                }
                else
                {
                    Debug.LogError($"{nameof(AsyncAssets)} Path to guid:{guid} not found, with context:{context}");
                    AsyncReference<T>.Fail(reference);
                }
            });
            return reference;
        }

        [Obsolete]
        public AsyncReference<T> Retain<T>(string guid, object context = null) where T : UnityEngine.Object
        {
            return RetainByGuid<T>(guid, context);
        }

        public void ExecuteOnReady(Lifetime lifetime, Action<IManifestProvider> action)
        {
            if (_isReady)
            {
                action(_manifestProvider);
            }
            else
            {
                _onReady.Subscribe(lifetime, action);
            }
        }

        private void Process<T>(AsyncReference<T> reference, AsyncReference.LoaderInfoProgressProvider progressProvider,
            AssetPath asset, object context = null) where T : UnityEngine.Object
        {
            if (!reference.Lifetime.IsTerminated)
            {
                if (asset.Bundle == null)
                {
                    var resourceHandler = Resources.LoadAsync<T>(asset.Name);
                    resourceHandler.completed += operation =>
                    {
                        if (!reference.Lifetime.IsTerminated)
                        {
                            if (resourceHandler.asset != null)
                            {
                                AsyncReference<T>.Resolve(reference, (T) resourceHandler.asset);
                            }
                            else
                            {
                                AsyncReference<T>.Fail(reference);
                            }
                        }
                    };
                }
                else
                {
                    var bundle = _bundleManager.Get(asset.Bundle);
                    progressProvider.LoaderInfo = bundle.Loader;
                    var isResolved = false;

                    void OnAsyncBundleReady(AsyncBundle asyncBundle)
                    {
                        var assetName = asset.Name;
                        if (asyncBundle.IsReady)
                        {
                            var bundleAssetHandler = asyncBundle.Bundle.LoadAssetAsync<T>(assetName);

                            async void OnComplete(AsyncOperation asyncOperation)
                            {
                                if (!isResolved && !reference.Lifetime.IsTerminated)
                                {
                                    isResolved = true;

                                    if (asset.Bundle.UnloadType == UnloadType.NeverUnload)
                                    {
                                        bundle.Retain(this);
                                    }

                                    var bundleAsset = (T) bundleAssetHandler.asset;

                                    AsyncReference<T>.Resolve(reference, bundleAsset);

                                    var result = await reference.Task;
                                    if (!reference.Lifetime.IsTerminated && result != null && result is T)
                                    {
                                        if (asset.Bundle.UnloadType == UnloadType.UnloadAfterResolve)
                                        {
                                            reference.Lifetime.AddAction(bundle.KeepInstance(reference.Context)
                                                .Dispose);
                                            bundle.Release(reference.Context);
                                        }
                                    }
                                }
                            }

                            bundleAssetHandler.completed += OnComplete;
                            reference.Lifetime.AddAction(() => bundleAssetHandler.completed -= OnComplete);
                        }
                        else
                        {
                            AsyncReference<T>.Fail(reference);
                        }
                    }

                    if (bundle.IsReady)
                    {
                        bundle.Retain(reference.Context);
                        reference.Lifetime.AddAction(() => { bundle.Release(reference.Context); });
                        OnAsyncBundleReady(bundle);
                    }
                    else if (bundle.IsError)
                    {
                        AsyncReference<T>.Fail(reference);
                    }
                    else
                    {
                        bundle.SubscribeOnLoaded(reference.Lifetime, OnAsyncBundleReady);
                        bundle.Retain(reference.Context);
                        reference.Lifetime.AddAction(() => { bundle.Release(reference.Context); });
                    }
                }
            }
        }
    }
}
