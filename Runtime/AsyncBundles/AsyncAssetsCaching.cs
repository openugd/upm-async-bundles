using System;
using System.IO;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AsyncAssetsCaching
    {
        private const string DirName = "AsyncAssetsBundleCache";

        public static string CacheDir =>
            _cacheDir ?? (_cacheDir = Path.Combine(Application.persistentDataPath, DirName));

        private static string _cacheDir;
        private readonly Signal _onReady;
        private bool _isReady;

        public AsyncAssetsCaching(Lifetime lifetime, ICoroutineProvider provider)
        {
            _onReady = new Signal(lifetime);

#if UNITY_WEBGL
            _isReady = true;
#else

            var cache = Caching.GetCacheByPath(CacheDir);
            if (!cache.valid)
            {
                Directory.CreateDirectory(CacheDir);
                cache = Caching.AddCache(CacheDir);
            }

            provider.StartCoroutine(WaitCacheReady(cache), lifetime);
#endif
        }

        public void ExecuteOnReady(Lifetime lifetime, Action listener)
        {
            if (_isReady) listener();
            else _onReady.Subscribe(lifetime, listener);
        }

#if !UNITY_WEBGL
        private IEnumerator WaitCacheReady(Cache cache)
        {
            while (true)
            {
                yield return null;
                if (cache.ready)
                {
                    Caching.MoveCacheBefore(cache, Caching.GetCacheAt(0));

                    _isReady = true;
                    _onReady.Fire();
                    break;
                }
            }
        }
#endif
    }
}
