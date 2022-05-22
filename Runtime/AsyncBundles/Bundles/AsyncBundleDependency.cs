using System;
using System.Collections.Generic;
using System.Linq;
using OpenUGD.AsyncBundles.Manifests;

namespace OpenUGD.AsyncBundles.Bundles
{
    public class AsyncBundleDependency : IAsyncRefs
    {
        private readonly HashSet<object> _refs;
        private readonly Lifetime _lifetime;
        private readonly Bundle _bundle;
        private readonly List<AsyncBundle> _dependencies;
        private readonly Signal<AsyncBundleDependency> _onLoaded;
        private Lifetime.Definition _dependencyLifetime;
        private bool _resolved = false;

        public AsyncBundleDependency(Lifetime lifetime, Bundle bundle, List<AsyncBundle> dependencies)
        {
            _lifetime = lifetime;
            _bundle = bundle;
            _dependencies = dependencies;
            _refs = new HashSet<object>();
            _onLoaded = new Signal<AsyncBundleDependency>(lifetime);
        }

        public override string ToString()
        {
            return $"Dependency To: {_bundle.Name}";
        }

        public void Release(AsyncBundle value)
        {
            if (_refs.Remove(value) && _refs.Count == 0)
            {
                foreach (var bundle in _dependencies)
                {
                    bundle.Release(this);
                }

                if (_dependencyLifetime != null)
                {
                    var dep = _dependencyLifetime;
                    _dependencyLifetime = null;
                    dep.Terminate();
                }
            }
        }

        public void Retain(AsyncBundle value)
        {
            if (_refs.Add(value))
            {
                foreach (var bundle in _dependencies)
                {
                    bundle.Retain(this);
                }

                if (IsReady || IsError)
                {
                    _onLoaded.Fire(this);
                }
                else
                {
                    if (_dependencyLifetime == null)
                    {
                        _dependencyLifetime = Lifetime.Define(_lifetime, Name);
                        _dependencyLifetime.Lifetime.AddAction(() => { _resolved = false; });
                    }

                    foreach (var bundle in _dependencies)
                    {
                        bundle.SubscribeOnLoaded(_dependencyLifetime.Lifetime, DependencyLoadedHandler);
                    }
                }
            }
        }

        public int Count => _dependencies.Count;

        public IEnumerable<object> Refs => _refs;

        public string Name => _bundle.Name;

        public bool IsReady
        {
            get { return _dependencies.Count == 0 || _dependencies.TrueForAll(a => a.IsReady); }
        }

        public bool IsError
        {
            get { return _dependencies.Count != 0 && _dependencies.Any(a => a.IsError); }
        }

        public bool IsResolved => _resolved;

        public void SubscribeOnLoaded(Lifetime lifetime, Action<AsyncBundleDependency> listener)
        {
            _onLoaded.Subscribe(lifetime, listener);
        }

        private void DependencyLoadedHandler(AsyncBundle asyncBundle)
        {
            if (!_resolved)
            {
                if (IsReady || IsError)
                {
                    _resolved = true;
                    _onLoaded.Fire(this);
                }
            }
        }

        public IDisposable KeepInstance(object value)
        {
            var result = new Keep();
            foreach (var bundle in _dependencies)
            {
                result.Add(bundle.KeepInstance(value));
            }

            return result;
        }

        private class Keep : IDisposable
        {
            private readonly List<IDisposable> _disposables = new List<IDisposable>();

            public void Dispose()
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }

                _disposables.Clear();
            }

            public void Add(IDisposable disposable)
            {
                _disposables.Add(disposable);
            }
        }
    }
}
