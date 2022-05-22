using System;
using System.Collections;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Bundles.Loaders
{
    public class AsyncWWWBundleLoader : IAsyncBundleLoader
    {
        private static int _instances;
        protected readonly Lifetime _lifetime;
        protected readonly ICoroutineProvider _coroutineProvider;
        private readonly int _retryCount;
        private readonly float _retryDelay;
        private readonly int _timeout;

        protected readonly Signal<IAsyncBundleLoader> _onComplete;
        protected Coroutine _coroutine;
        protected bool _bundleRequestDone;
        private int _instanceId;
        private WWWBundleLoader _bundleRequest;
        protected string _url;

        public AsyncWWWBundleLoader(Lifetime lifetime, ICoroutineProvider coroutineProvider, int retryCount,
            float retryDelay, int timeout)
        {
            _instanceId = _instances++;
            _lifetime = lifetime;
            _coroutineProvider = coroutineProvider;
            _retryCount = retryCount;
            _retryDelay = retryDelay;
            _timeout = timeout;
            _onComplete = new Signal<IAsyncBundleLoader>(lifetime);
        }

        public float Progress { get; protected set; }
        public bool IsLoading { get; protected set; }
        public bool IsLoaded { get; protected set; }
        public AssetBundle Bundle { get; protected set; }
        public Func<IAsyncBundleLoader> Next { get; set; }
        public bool Unloaded { get; protected set; }

        public void SubscribeOnComplete(Lifetime lifetime, Action<IAsyncBundleLoader> listener)
        {
            _onComplete.Subscribe(lifetime, listener);
        }

        public virtual void Load(string url, string hash)
        {
            if (AsyncBundleLoaderManager.Verbose)
            {
                AsyncBundleLoaderManager.Log($"{nameof(AsyncWWWBundleLoader)}.{nameof(Load)} path:{url}");
            }
            //Debug.Log($"AsyncWWWBundleLoader.Load {url}");

            _url = url;

            if (_coroutine != null)
            {
                if (!IsLoaded && !IsLoading)
                {
                    IsLoading = true;
                    Unloaded = false;
                }

                //Debug.LogWarning($"AsyncWWWBundleLoader.Load already loading {url}");

                return;
            }

            _coroutine = _coroutineProvider.StartCoroutine(LoadAsyncWebRequest(url, hash));
        }

        public void Unload(bool unloadAllLoadedObjects)
        {
            if (AsyncBundleLoaderManager.Verbose)
            {
                AsyncBundleLoaderManager.Log($"{nameof(AsyncWWWBundleLoader)}.{nameof(Unload)} path:{_url}");
            }
#if DEBUG_ASYNC_BUNDLE
      Debug.Log(">>> UNLOAD: " + Path.GetFileName(_url));
#endif
            if (!IsLoading)
            {
                if (_bundleRequestDone)
                {
                    UnloadBundle(unloadAllLoadedObjects);
                }

                Bundle = null;
                IsLoaded = false;
            }

            IsLoading = false;
            Unloaded = true;
        }

        private IEnumerator LoadAsyncWebRequest(string url, string hash)
        {
            //Debug.Log($"AsyncWWWBundleLoader.LoadAsyncWebRequest {url} {Bundle}");

            if (Bundle == null)
            {
                Unloaded = false;
                IsLoading = true;

                var retryDelay = _retryDelay > 0 ? _retryDelay : 0;
                var retryCount = _retryCount > 0 ? _retryCount : 0;
                if (_bundleRequest == null)
                {
                    //Debug.Log($"AsyncWWWBundleLoader.LoadAsyncWebRequest make new request, retries {retryCount}");

                    _bundleRequestDone = false;

                    WWWBundleLoader bundleRequest = null;
                    while (retryCount >= 0)
                    {
                        --retryCount;

                        bundleRequest = new WWWBundleLoader(url, hash, _timeout);
                        _bundleRequest = bundleRequest;

                        IsLoaded = false;
                        Bundle = null;

                        while (!bundleRequest.IsDone)
                        {
                            if (!IsLoading)
                            {
#if DEBUG_ASYNC_BUNDLE
              Debug.Log(">>> STOP: " + Path.GetFileName(url));
#endif

                                StopLoad();
                                yield break;
                            }

                            //Debug.Log($"AsyncWWWBundleLoader.LoadAsyncWebRequest progress {bundleRequest.Progress}");

                            Progress = bundleRequest.Progress;
                            yield return null;
                        }

                        if (bundleRequest.IsError)
                        {
                            AsyncBundleLoaderManager.LogError(
                                $"error on load bundle, isNetworkError:{bundleRequest.IsNetworkError}, isHttpError:{bundleRequest.IsHttpError}, error:{bundleRequest.Error}, responseCode:{bundleRequest.ResponseCode}, url:{url}");

                            DisposeRequest();
                            yield return new WaitForSeconds(retryDelay);
                            if (!IsLoading)
                            {
#if DEBUG_ASYNC_BUNDLE
              Debug.Log(">>> STOP: " + Path.GetFileName(url));
#endif

                                StopLoad();
                                yield break;
                            }

                            if (retryCount >= 0)
                            {
                                AsyncBundleLoaderManager.LogError($"retry to load bundle: {url}");
                            }
                        }
                        else
                        {
                            //Debug.Log($"AsyncWWWBundleLoader.LoadAsyncWebRequest break loading because done without error");
                            break;
                        }
                    }

                    //Debug.Log($"AsyncWWWBundleLoader.LoadAsyncWebRequest done loading");
                    Progress = 1;
                    _bundleRequestDone = true;

                    if (bundleRequest != null)
                    {
                        if (bundleRequest.IsDone && !bundleRequest.IsError)
                        {
                            Bundle = bundleRequest.Bundle;
                        }

                        if (Bundle == null)
                        {
                            AsyncBundleLoaderManager.LogError(
                                $"bundle not loaded, isNetworkError:{bundleRequest.IsNetworkError}, isHttpError:{bundleRequest.IsHttpError}, error:{bundleRequest.Error}, responseCode:{bundleRequest.ResponseCode}, url:{url}");
                            DisposeRequest();
                        }
                    }
                    else
                    {
                        AsyncBundleLoaderManager.LogError($"{nameof(AssetBundle)} can not load from url:{url}");
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
                    DisposeRequest();

#if DEBUG_ASYNC_BUNDLE
          Debug.Log(">>> END: " + Path.GetFileName(url));
#endif

                    //Debug.Log($"AsyncWWWBundleLoader.LoadAsyncWebRequest end of loading");

                    yield return null;
                    if (!_lifetime.IsTerminated)
                    {
                        if (AsyncBundleLoaderManager.Verbose)
                        {
                            AsyncBundleLoaderManager.Log($"{nameof(AsyncWWWBundleLoader)} Load complete path:{url}");
                        }

                        _onComplete.Fire(this);
                    }
                }
                else
                {
                    AsyncBundleLoaderManager.Log(
                        $"{nameof(AsyncWWWBundleLoader)}.{nameof(LoadAsyncWebRequest)}: already have bundle request");
                }
            }
            else if (Bundle != null)
            {
                yield return null;
                if (!_lifetime.IsTerminated)
                {
                    if (AsyncBundleLoaderManager.Verbose)
                    {
                        AsyncBundleLoaderManager.Log($"{nameof(AsyncWWWBundleLoader)} Load complete path:{url}");
                    }

                    _onComplete.Fire(this);
                }
            }
        }

        protected void StopLoad()
        {
            AsyncBundleLoaderManager.Log($"{nameof(AsyncWWWBundleLoader)}.{nameof(StopLoad)} {_url}");

            _coroutine = null;
            IsLoading = false;
            UnloadBundle(true);
        }

        private void DisposeRequest()
        {
            var bundleRequest = _bundleRequest;
            _bundleRequest = null;

            if (bundleRequest != null)
            {
                bundleRequest.Dispose();
            }
        }

        private void UnloadBundle(bool unloadAllLoadedObjects)
        {
            if (Bundle != null)
            {
                Bundle.Unload(unloadAllLoadedObjects);
                Bundle = null;
            }
            else
            {
                if (_bundleRequest != null && _bundleRequest.IsDone)
                {
                    Bundle = _bundleRequest.Bundle;
                    if (Bundle != null)
                    {
                        Bundle.Unload(unloadAllLoadedObjects);
                    }
                }

                Bundle = null;
            }

            DisposeRequest();
            _bundleRequestDone = false;
        }
    }
}
