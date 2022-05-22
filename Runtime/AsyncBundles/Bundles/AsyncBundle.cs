using System;
using System.Collections;
using System.Collections.Generic;
using OpenUGD.AsyncBundles.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace OpenUGD.AsyncBundles.Bundles
{
    public class AsyncBundle : IAsyncRefs
    {
        private enum State : byte
        {
            Retain,
            Release
        }

        private readonly HashSet<object> _refs;
        private readonly Signal<AsyncBundle> _onLoaded;
        private readonly Signal<AsyncBundle> _onUnloaded;
        private readonly Lifetime _lifetime;
        private readonly string _path;
        private readonly string _hash;
        private readonly float _delayToUnload;
        private readonly ICoroutineProvider _coroutineProvider;
        private readonly LoaderInfo _loaderInfo;
        private readonly AsyncBundleDependency _dependency;
        private readonly HashSet<object> _keepInstances;
        private AssetBundle _bundle;
        private State _state = State.Release;

        public AsyncBundle(Lifetime lifetime, ICoroutineProvider coroutineProvider, string path, string hash,
            float delayToUnload = 0f)
        {
            _lifetime = lifetime;
            _path = path;
            _hash = hash;
            _delayToUnload = delayToUnload;
            _coroutineProvider = coroutineProvider;
            _refs = new HashSet<object>();
            _keepInstances = new HashSet<object>();
            _onLoaded = new Signal<AsyncBundle>(lifetime);
            _onUnloaded = new Signal<AsyncBundle>(lifetime);
            _loaderInfo = new LoaderInfo(this);
            lifetime.AddAction(() => { _keepInstances.Clear(); });
        }

        public AsyncBundle(Lifetime lifetime, ICoroutineProvider coroutineProvider, string name, string path,
            string hash, AsyncBundleDependency dependency, float delayToUnload = 0f) : this(lifetime, coroutineProvider,
            path, hash, delayToUnload)
        {
            _dependency = dependency;
        }

        public AsyncBundleDependency Dependencies => _dependency;

        public string Name => _path;

        public IEnumerable<object> Refs => _refs;

        public ILoaderInfo Loader => _loaderInfo;

        public bool IsReady => _loaderInfo.IsLoaded && _bundle != null && (_dependency == null || _dependency.IsReady);

        public bool IsError => _loaderInfo.IsError || _dependency.IsError;

        public string Path => _path;

        public AssetBundle Bundle => _bundle;

        public bool UnloadInstances => _keepInstances.Count == 0;

        public IDisposable KeepInstance(object value) => new Keep(this, value);

        public void SubscribeOnLoaded(Lifetime lifetime, Action<AsyncBundle> listener) =>
            _onLoaded.Subscribe(lifetime, listener);

        public void SubscribeOnUnloaded(Lifetime lifetime, Action<AsyncBundle> listener) =>
            _onUnloaded.Subscribe(lifetime, listener);

        public void Release(object context)
        {
            Assert.IsNotNull(context);
            if (_refs.Remove(context) && _refs.Count == 0)
            {
                void UnloadBundlesWithDependency()
                {
                    if (_refs.Count == 0)
                    {
                        UnloadInternal();
                        if (_dependency != null) _dependency.Release(this);
                    }
                }

                if (_delayToUnload > float.Epsilon)
                {
                    var waitDef = _lifetime.DefineNested();
                    waitDef.Lifetime.AddAction(() => {
                        if (_lifetime.IsTerminated)
                        {
                            UnloadBundlesWithDependency();
                        }
                    });
                    _coroutineProvider.StartCoroutine(Wait(() => {
                        waitDef.Terminate();
                        UnloadBundlesWithDependency();
                    }, _delayToUnload), waitDef.Lifetime);
                }
                else
                {
                    UnloadBundlesWithDependency();
                }
            }
        }

        public void Retain(object context)
        {
            Assert.IsNotNull(context);
            if (_refs.Add(context))
            {
                if (_dependency != null) _dependency.Retain(this);

                LoadInternal();
            }
        }

        public void Unload() => UnloadInternal();

        private void LoadInternal()
        {
            if (_state == State.Release && !_loaderInfo.IsLoaded && !_loaderInfo.IsLoading && !_loaderInfo.IsError)
            {
                _state = State.Retain;
                _loaderInfo.Load(_path, _hash);
            }
        }

        private void UnloadInternal()
        {
            if (_state == State.Retain)
            {
                _state = State.Release;
                if (_loaderInfo.IsLoaded)
                {
                    _onUnloaded.Fire(this);
                }

                _loaderInfo.Unload();
            }
        }

        private IEnumerator Wait(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action();
        }

        public interface ILoaderInfo
        {
            float Progress { get; }
            bool IsLoading { get; }
            bool IsLoaded { get; }
            bool IsError { get; }
        }

        private class LoaderInfo : ILoaderInfo
        {
            private readonly AsyncBundle _bundle;
            private Lifetime.Definition _loaderDefinition;
            private Lifetime.Definition _completeDefinition;
            private AsyncBundleLoader _bundleLoader;

            public LoaderInfo(AsyncBundle bundle)
            {
                _bundle = bundle;
            }

            public float Progress {
                get { return _bundleLoader != null ? _bundleLoader.Progress : 0f; }
            }

            public bool IsLoading { get; private set; }
            public bool IsLoaded { get; private set; }
            public bool IsError { get; private set; }

            public void Load(string path, string hash)
            {
                if (_loaderDefinition == null)
                {
                    _loaderDefinition = Lifetime.Define(_bundle._lifetime);

                    var bundleLoader = AsyncBundleLoaderManager.Get(path, hash);
                    _bundleLoader = bundleLoader;
                    bundleLoader.ExecuteOnReady(_loaderDefinition.Lifetime, loader => {
                        IsLoading = false;
                        if (loader.Bundle != null)
                        {
                            IsLoaded = true;
                        }
                        else
                        {
                            IsError = true;
                        }

                        InvokeComplete(loader.Bundle, path, hash);
                    });
                    _loaderDefinition.Lifetime.AddAction(() => {
                        _bundle._onUnloaded.Fire(_bundle);
                        _loaderDefinition = null;
                        IsLoaded = false;
                        IsLoading = false;
                        IsError = false;

                        bundleLoader.Release(this, _bundle.UnloadInstances);

                        _bundleLoader = null;
                    });

                    IsLoading = true;
                    bundleLoader.Retain(this);
                }
            }

            public void Unload()
            {
                if (_loaderDefinition != null)
                {
                    _loaderDefinition.Terminate();
                }
            }

            private void InvokeComplete(AssetBundle bundle, string path, string hash)
            {
                if (IsError)
                {
                    if (Application.isEditor) Debug.LogError("bundle not load:" + path + ", hash:" + hash);
                    _bundle._onLoaded.Fire(_bundle);
                }
                else
                {
                    _bundle._bundle = bundle;
                    if (_bundle._dependency == null || _bundle._dependency.IsReady || _bundle._dependency.IsError)
                    {
                        _bundle._onLoaded.Fire(_bundle);
                    }
                    else
                    {
                        _bundle._dependency.SubscribeOnLoaded(_loaderDefinition.Lifetime, dependency => {
                            if ((_bundle._dependency == null || _bundle._dependency.IsReady) && IsLoaded &&
                                _bundle._bundle != null)
                            {
                                _bundle._onLoaded.Fire(_bundle);
                            }
                        });
                    }
                }
            }
        }

        private class Keep : IDisposable
        {
            private readonly AsyncBundle _bundle;
            private readonly object _value;
            private readonly IDisposable _disposable;

            public Keep(AsyncBundle bundle, object value)
            {
                _bundle = bundle;
                _value = value;
                _bundle._keepInstances.Add(value);
                if (_bundle._dependency != null)
                {
                    _disposable = _bundle._dependency.KeepInstance(value);
                }
            }

            public void Dispose()
            {
                _bundle._keepInstances.Remove(_value);
                if (_disposable != null)
                {
                    _disposable.Dispose();
                }
            }
        }
    }
}
