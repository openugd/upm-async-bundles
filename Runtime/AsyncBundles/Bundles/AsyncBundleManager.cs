using System.Collections.Generic;
using OpenUGD.AsyncBundles.Manifests;
using OpenUGD.AsyncBundles.Utils;

namespace OpenUGD.AsyncBundles.Bundles
{
    public class AsyncBundleManager
    {
        private readonly Lifetime _lifetime;
        private readonly ICoroutineProvider _coroutineProvider;
        private readonly Dictionary<string, AsyncBundle> _map;

        public AsyncBundleManager(Lifetime lifetime, ICoroutineProvider coroutineProvider)
        {
            _lifetime = lifetime;
            _coroutineProvider = coroutineProvider;
            _map = new Dictionary<string, AsyncBundle>();
            _lifetime.AddAction(() => { _map.Clear(); });
        }

        public IEnumerable<AsyncBundle> Bundles => _map.Values;

        public AsyncBundle Get(string path, string hash)
        {
            AsyncBundle result;
            if (!_map.TryGetValue(path, out result))
            {
                result = new AsyncBundle(_lifetime, _coroutineProvider, path, hash);
                _map[path] = result;
            }

            return result;
        }

        public AsyncBundle Get(Bundle bundle)
        {
            AsyncBundle result;
            if (!_map.TryGetValue(bundle.Uri, out result))
            {
                var dependencies = new List<AsyncBundle>();
                result = new AsyncBundle(_lifetime, _coroutineProvider, bundle.Name, bundle.Uri, bundle.Hash,
                    new AsyncBundleDependency(_lifetime, bundle, dependencies), bundle.DelayToUnload);
                _map[bundle.Uri] = result;
                if (bundle.DependencyBundles != null)
                {
                    foreach (var dependency in bundle.DependencyBundles)
                    {
                        if (!IsAncestor(bundle, dependency))
                        {
                            var async = Get(dependency);
                            dependencies.Add(async);
                        }
                    }
                }
            }

            return result;
        }

        private bool IsAncestor(Bundle bundle, Bundle dependency)
        {
            return bundle.Uri == dependency.Uri;
        }
    }
}
