using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenUGD.AsyncBundles.Bundles.Loaders;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace OpenUGD.AsyncBundles.Bundles
{
    public class AsyncBundleQueue
    {
        private readonly LinkedQueue<LoaderItem> _queue = new LinkedQueue<LoaderItem>();
        private readonly Lifetime _lifetime;
        private readonly ICoroutineProvider _coroutineProvider;
        private readonly List<LoaderItem> _current = new List<LoaderItem>();
        private int _retryCount;
        private float _retryDelay;
#if UNITY_IOS
        private int _maxThreads = 10;
        private int _timeout = 7;
#else
        private int _maxThreads = 1;
        private int _timeout = 10;
#endif

        public int RetryCount
        {
            get => _retryCount;
            set => _retryCount = value;
        }

        public float RetryDelay
        {
            get => _retryDelay;
            set => _retryDelay = value;
        }

        public int Timeout
        {
            get => _timeout;
            set => _timeout = value;
        }

        public int MaxThreads
        {
            get => _maxThreads;
            set
            {
                Assert.IsTrue(value > 0);
                _maxThreads = value;
            }
        }

        public IEnumerable<IAsyncLoaderItemProgress> Current => _current;

        public IEnumerable<IAsyncLoaderItemProgress> Queue => _queue;

        public AsyncBundleQueue(Lifetime lifetime, ICoroutineProvider coroutineProvider)
        {
            _lifetime = lifetime;
            _coroutineProvider = coroutineProvider;
        }

        public IAsyncLoaderItem Enqueue(string path, string hash, Action<IAsyncLoaderItem> onLoaded)
        {
            if (AsyncBundleLoaderManager.Verbose)
            {
                AsyncBundleLoaderManager.Log($"Enqueue path: {path}");
            }

            var lifetime = Lifetime.Define(_lifetime);
            IAsyncBundleLoader loader;
            if (AsyncBundleUtils.IsWWW(path))
            {
                loader = new AsyncWWWBundleLoader(lifetime.Lifetime, _coroutineProvider, _retryCount, _retryDelay,
                    _timeout);
            }
            else if (AsyncBundleUtils.IsStreamingAssets(path))
            {
                loader = new AsyncStreamingAssetBundleLoader(lifetime.Lifetime, _coroutineProvider, _retryCount,
                    _retryDelay, _timeout);
            }
            else
            {
                loader = new AsyncResourceBundleLoader(lifetime.Lifetime, _coroutineProvider)
                {
                    Next = () => new AsyncStreamingAssetBundleLoader(lifetime.Lifetime, _coroutineProvider, _retryCount,
                        _retryDelay, _timeout)
                };
            }

            var item = new LoaderItem(lifetime)
            {
                Loader = loader,
                OnReady = onLoaded,
                Path = path,
                Hash = hash
            };

            _queue.Enqueue(item);

            item.Lifetime.AddAction(() =>
            {
                if (!item.IsProcessed)
                {
                    _queue.Remove(item);
                }
            });

            ProcessNext();

            return item;
        }

        private void ProcessNext()
        {
            while (MaxThreads > _current.Count && _queue.Count != 0)
            {
                var item = _queue.Dequeue();
                if (!item.Lifetime.IsTerminated)
                {
                    item.IsProcessed = true;
                    var lt = Lifetime.Define(item.Lifetime);
                    _current.Add(item);

                    item.IsCurrent = true;
                    item.Lifetime.AddAction(() =>
                    {
                        lt.Terminate();
                        if (item.IsCurrent)
                        {
                            var current = _current;
                            current.Remove(item);
                            item.IsCurrent = false;
                        }

                        _coroutineProvider.StartCoroutine(DoNext(ProcessNext));
                    });
                    item.Loader.SubscribeOnComplete(lt.Lifetime, loader =>
                    {
                        lt.Terminate();
                        if (item.IsCurrent)
                        {
                            var current = _current;
                            current.Remove(item);
                            item.IsCurrent = false;
                        }

                        if (loader.Bundle == null && loader.Next != null)
                        {
                            item.Loader = item.Loader.Next();
                            _queue.EnqueueFirst(item);
                        }
                        else
                        {
                            item.OnReady(item);
                        }

                        ProcessNext();
                        //_coroutineProvider.StartCoroutine(WaitAndDo(1f, ProcessNext));
                    });

                    item.Load();
                }
            }
        }

        private IEnumerator WaitAndDo(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action();
        }

        private IEnumerator DoNext(Action action)
        {
            yield return null;
            action();
        }

        private class LoaderItem : IAsyncLoaderItem
        {
            private readonly Lifetime.Definition _lifetime;
            public Action<IAsyncLoaderItem> OnReady;
            public IAsyncBundleLoader Loader;
            public string Path { get; set; }
            public string Hash;
            public bool IsCurrent;
            public bool IsProcessed;

            public LoaderItem(Lifetime.Definition lifetime)
            {
                _lifetime = lifetime;
            }

            public Lifetime.Definition Definition => _lifetime;

            public Lifetime Lifetime => _lifetime.Lifetime;

            public float Progress => Loader.Progress;

            public AssetBundle Bundle => Loader.Bundle;

            public void Unload(bool unloadInstances)
            {
                if (AsyncBundleLoaderManager.Verbose)
                {
                    AsyncBundleLoaderManager.Log($"Unload path: {Path}");
                }

                Definition.Terminate();
                Loader.Unload(unloadInstances);
            }

            public void Load()
            {
                if (AsyncBundleLoaderManager.Verbose)
                {
                    AsyncBundleLoaderManager.Log($"Load path: {Path}");
                }

                Loader.Load(Path, Hash);
            }
        }
    }
}
