using System;
using System.Collections.Generic;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace OpenUGD.AsyncBundles.Bundles
{
    public class AsyncBundleLoaderManager
    {
        private class CoroutineBehaviour : MonoBehaviour
        {
        }

        private static readonly Dictionary<string, AsyncBundleLoader>
            _map = new Dictionary<string, AsyncBundleLoader>();

        private static AsyncBundleQueue _queue;
        private static Lifetime.Definition _lifetime;
        private static ICoroutineProvider _coroutineProvider;

        public static bool Verbose = false;

        public static void Log(string message)
        {
            Debug.Log($"[ABL] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[ABL] {message}");
        }

        private static void Initialize()
        {
            if (_lifetime == null)
            {
                _lifetime = Lifetime.Define(Lifetime.Eternal);

                _coroutineProvider = CoroutineObject.Singleton().GetProvider();
                _queue = new AsyncBundleQueue(_lifetime.Lifetime, _coroutineProvider);
            }
        }

        public static IEnumerable<IAsyncLoaderItemProgress> Current
        {
            get
            {
                Initialize();
                return _queue.Current;
            }
        }

        public static IEnumerable<IAsyncLoaderItemProgress> Queue
        {
            get
            {
                Initialize();
                return _queue.Queue;
            }
        }

        public static int RetryCount
        {
            get
            {
                Initialize();
                return _queue.RetryCount;
            }
            set
            {
                Assert.IsTrue(value >= 0);
                Initialize();
                _queue.RetryCount = value;
            }
        }

        public static float RetryDelay
        {
            get
            {
                Initialize();
                return _queue.RetryDelay;
            }
            set
            {
                Assert.IsTrue(value >= 0f);
                Initialize();
                _queue.RetryDelay = value;
            }
        }

        public static int MaxThreads
        {
            get
            {
                Initialize();
                return _queue.MaxThreads;
            }
            set
            {
                Assert.IsTrue(value > 0);
                Initialize();
                _queue.MaxThreads = value;
            }
        }

        public static int Timeout
        {
            get
            {
                Initialize();
                return _queue.Timeout;
            }
            set
            {
                Assert.IsTrue(value > 0);
                Initialize();
                _queue.Timeout = value;
            }
        }


        public static AsyncBundleLoader Get(string path, string hash)
        {
            Initialize();

            AsyncBundleLoader loader;
            if (!_map.TryGetValue(path, out loader))
            {
                _map[path] = loader = new AsyncBundleLoader(_lifetime.Lifetime, path, _queue);
            }

            loader.Hash = hash;

            return loader;
        }
    }

    public class AsyncBundleLoader
    {
        private enum Status
        {
            None,
            NeedUnload,
        }

        private readonly string _path;
        private readonly AsyncBundleQueue _queue;
        private readonly HashSet<object> _refs = new HashSet<object>();
        private bool _doNotUnloadInstances;

        private Signal<AsyncBundleLoader> _onReady;
        private IAsyncLoaderItem _loader;
        private Status _status;

        public AsyncBundleLoader(Lifetime lifetime, string path, AsyncBundleQueue queue)
        {
            _path = path;
            _queue = queue;

            _onReady = new Signal<AsyncBundleLoader>(lifetime);
        }

        public string Path => _path;
        public AssetBundle Bundle { get; private set; }

        public float Progress
        {
            get { return _loader != null ? _loader.Progress : 0f; }
        }

        public string Hash { get; set; }

        public void Retain(object value)
        {
            if (_refs.Add(value))
            {
                Load();
            }
            else
            {
                AsyncBundleLoaderManager.Log("AsyncBundleLoader.Load: already retained");
            }
        }

        public void Release(object value, bool unloadInstances)
        {
            _doNotUnloadInstances = _doNotUnloadInstances || !unloadInstances;
            _refs.Remove(value);
            if (_refs.Count == 0)
            {
                Unload();
            }
        }

        public void ExecuteOnReady(Lifetime lifetime, Action<AsyncBundleLoader> listener)
        {
            if (Bundle != null)
            {
                listener(this);
            }
            else
            {
                _onReady.Subscribe(lifetime, listener);
            }
        }

        private void Load()
        {
            _status = Status.None;

            if (_loader == null)
            {
                _loader = _queue.Enqueue(_path, Hash, OnComplete);
            }
            else
            {
                AsyncBundleLoaderManager.Log("AsyncBundleLoader.Load: already has loader");
            }
        }

        private void OnComplete(IAsyncLoaderItem loader)
        {
            InvokeComplete(loader.Bundle);
        }

        private void InvokeComplete(AssetBundle bundle)
        {
            if (_status == Status.NeedUnload)
            {
                Unload();
            }
            else
            {
                Bundle = bundle;
                _onReady.Fire(this);
            }
        }

        private void Unload()
        {
            if (_loader != null)
            {
                _loader.Unload(!_doNotUnloadInstances);
                _loader = null;
            }

            _status = Status.NeedUnload;
        }
    }
}
