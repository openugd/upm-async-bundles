using System;
using System.Collections;
using System.IO;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Bundles.Loaders
{
    public class AsyncStreamingAssetBundleLoader : AsyncWWWBundleLoader
    {
        public AsyncStreamingAssetBundleLoader(Lifetime lifetime, ICoroutineProvider coroutineProvider, int retryCount,
            float retryDelay, int timeout) : base(lifetime, coroutineProvider, retryCount, retryDelay, timeout)
        {
        }

        public override void Load(string url, string hash)
        {
            if (AsyncBundleLoaderManager.Verbose)
            {
                AsyncBundleLoaderManager.Log($"{nameof(AsyncStreamingAssetBundleLoader)} Loaded path:{url}");
            }

            if (_coroutine != null)
            {
                if (!IsLoaded && !IsLoading)
                {
                    IsLoading = true;
                    Unloaded = false;
                }

                return;
            }

            _url = url;
#if DEBUG_ASYNC_BUNDLE
      Debug.Log(">>> BEGIN: " + Path.GetFileName(url));
#endif

            var path = AsyncBundleUtils.IsStreamingAssets(url)
                ? url
                : Path.Combine(Application.streamingAssetsPath, url);
            if (path.Contains("://")) // is www
            {
                base.Load(path, hash);
            }
            else
            {
                _coroutine = _coroutineProvider.StartCoroutine(LoadAsync(path, hash));
            }
        }

        public IEnumerator LoadAsync(string url, string hash)
        {
            if (Bundle == null)
            {
                Unloaded = false;
                IsLoading = true;

                _bundleRequestDone = false;

                if (File.Exists(url) == false)
                {
                    IsLoaded = true;
                    IsLoading = false;
                    Bundle = null;
                    _bundleRequestDone = true;

                    yield return null;
                    if (!_lifetime.IsTerminated)
                    {
                        _onComplete.Fire(this);
                    }

                    yield break;
                }

                var bundleRequest = AssetBundle.LoadFromFileAsync(url);
                while (!bundleRequest.isDone)
                {
                    Progress = bundleRequest.progress;
                    yield return null;
                }

                _bundleRequestDone = true;

                try
                {
                    Bundle = bundleRequest.assetBundle;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (Bundle == null)
                {
                    AsyncBundleLoaderManager.LogError("bundle not load from: " + url);
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
                    if (AsyncBundleLoaderManager.Verbose)
                    {
                        AsyncBundleLoaderManager.Log(
                            $"{nameof(AsyncStreamingAssetBundleLoader)} Load complete path:{url}");
                    }

                    _onComplete.Fire(this);
                }
            }
            else
            {
                yield return null;

                IsLoading = false;
                IsLoaded = true;
                if (!_lifetime.IsTerminated)
                {
                    if (AsyncBundleLoaderManager.Verbose)
                    {
                        AsyncBundleLoaderManager.Log(
                            $"{nameof(AsyncStreamingAssetBundleLoader)} Load complete path:{url}");
                    }

                    _onComplete.Fire(this);
                }
            }
        }
    }
}
