using System;
using System.Collections;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Bundles.Loaders
{
    public class AsyncResourceBundleLoader : IAsyncBundleLoader
    {
        private readonly Lifetime _lifetime;
        private readonly ICoroutineProvider _coroutineProvider;
        private readonly Signal<IAsyncBundleLoader> _onComplete;
        private Coroutine _coroutine;

        public AsyncResourceBundleLoader(Lifetime lifetime, ICoroutineProvider coroutineProvider)
        {
            _lifetime = lifetime;
            _coroutineProvider = coroutineProvider;
            _onComplete = new Signal<IAsyncBundleLoader>(lifetime);
        }

        public void SubscribeOnComplete(Lifetime lifetime, Action<IAsyncBundleLoader> listener)
        {
            _onComplete.Subscribe(lifetime, listener);
        }

        public void Load(string url, string hash)
        {
            if (_coroutine != null)
            {
                if (!IsLoaded && !IsLoading)
                {
                    IsLoading = true;
                    Unloaded = false;
                }

                return;
            }

            if (_coroutine == null)
            {
                _coroutine = _coroutineProvider.StartCoroutine(LoadAsync(url, hash));
            }
        }

        public float Progress { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsLoaded { get; private set; }
        public AssetBundle Bundle { get; private set; }
        public Func<IAsyncBundleLoader> Next { get; set; }
        public bool Unloaded { get; private set; }

        private IEnumerator LoadAsync(string url, string hash)
        {
            if (Bundle == null)
            {
                Unloaded = false;
                IsLoading = true;

                var request = Resources.LoadAsync(url);

                while (!request.isDone)
                {
                    Progress = request.progress / 2f;
                    if (!IsLoading)
                    {
#if DEBUG_ASYNC_BUNDLE
          Debug.Log(">>> STOP: " + Path.GetFileName(url));
#endif
                        StopLoad();
                        yield break;
                    }

                    yield return null;
                }

                var textAsset = request.asset as TextAsset;
                if (textAsset != null)
                {
                    var bundleRequest = AssetBundle.LoadFromMemoryAsync(textAsset.bytes);
                    while (!bundleRequest.isDone)
                    {
                        Progress = bundleRequest.progress;
                        yield return null;
                    }

                    Catch(() => { Bundle = bundleRequest.assetBundle; });

                    if (Bundle == null)
                    {
                        Debug.LogError("bundle not load from: " + url);
                    }

                    _coroutine = null;
                    if (!IsLoading)
                    {
#if DEBUG_ASYNC_BUNDLE
          Debug.Log(">>> STOP: " + Path.GetFileName(url));
#endif
                        StopLoad();
                        yield break;
                    }

                    IsLoading = false;
                    IsLoaded = true;
#if DEBUG_ASYNC_BUNDLE
        Debug.Log(">>> END: " + Path.GetFileName(url));
#endif
                    yield return null;
                    if (!_lifetime.IsTerminated)
                    {
                        _onComplete.Fire(this);
                    }
                }
                else
                {
                    yield return null;
                    IsLoaded = true;
                    IsLoading = false;
                    if (!_lifetime.IsTerminated)
                    {
                        _onComplete.Fire(this);
                    }
                }
            }
        }

        public void Unload(bool unloadAllLoadedObjects)
        {
            if (_coroutine != null && _coroutineProvider != null)
            {
                _coroutineProvider.StopCoroutine(_coroutine);
                _coroutine = null;
            }

            if (Bundle != null)
            {
                Bundle.Unload(unloadAllLoadedObjects);
                Bundle = null;
            }

            IsLoading = false;
            IsLoaded = false;
            Unloaded = true;
        }

        private void StopLoad()
        {
            _coroutine = null;
            IsLoading = false;
            UnloadBundle(true);
        }

        private void Catch(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void UnloadBundle(bool unloadAllLoadedObjects)
        {
            if (Bundle != null)
            {
                Bundle.Unload(unloadAllLoadedObjects);
                Bundle = null;
            }
        }
    }
}
