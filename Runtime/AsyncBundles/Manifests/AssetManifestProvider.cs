using System.Collections.Generic;
using System.Text;

namespace OpenUGD.AsyncBundles.Manifests
{
    public class AssetManifestProvider : IManifestProvider
    {
        private readonly AssetsManifest _manifest;
        private readonly AssetProviderSettings _settings;
        private readonly Dictionary<string, Asset> _mapByGuid = new Dictionary<string, Asset>();
        private readonly Dictionary<string, Asset> _mapByName = new Dictionary<string, Asset>();
        private readonly Dictionary<string, List<Asset>> _mapByTags = new Dictionary<string, List<Asset>>();
        private readonly Dictionary<string, Bundle> _mapBundle = new Dictionary<string, Bundle>();
        private readonly Dictionary<string, AssetPathKey> _mapKeyPath = new Dictionary<string, AssetPathKey>();
        private readonly long _totalSize;

        public AssetManifestProvider(AssetsManifest manifest, AssetProviderSettings settings)
        {
            _manifest = manifest;
            _settings = settings;

            var stringBuilder = new StringBuilder(128);

            foreach (var key in manifest.KeyPaths)
            {
                _mapKeyPath[key.Key] = key;
            }

            foreach (var asset in manifest.Assets)
            {
                _mapByGuid[asset.Guid] = asset;
                _mapByName[asset.Name] = asset;
                if (asset.Tags != null)
                {
                    foreach (var assetTag in asset.Tags)
                    {
                        if (!_mapByTags.TryGetValue(assetTag, out var list))
                        {
                            _mapByTags[assetTag] = list = new List<Asset>();
                        }

                        list.Add(asset);
                    }
                }
            }

            long totalSize = 0;
            foreach (var bundle in manifest.Bundles)
            {
                ProcessBundle(bundle, stringBuilder);
                _mapBundle[bundle.Name] = bundle;
                totalSize += bundle.FileSize;
            }

            _totalSize = totalSize;

            foreach (var bundle in _mapBundle.Values)
            {
                var dependencies = bundle.Dependencies;
                if (dependencies != null && dependencies.Length != 0)
                {
                    var count = bundle.Dependencies.Length;
                    var bundles = new Bundle[count];
                    for (var i = 0; i < count; i++)
                    {
                        bundles[i] = _mapBundle[dependencies[i]];
                    }

                    bundle.DependencyBundles = bundles;
                }
            }
        }

        public long TotalSize => _totalSize;
        public AssetsManifest Manifest => _manifest;

        public bool TryGetBundle(string bundleName, out Bundle bundle)
        {
            return _mapBundle.TryGetValue(bundleName, out bundle);
        }

        public bool TryGetByName(string name, out AssetPath path)
        {
            if (_mapByName.TryGetValue(name, out var asset))
            {
                var bundle = _mapBundle[asset.Bundle];

                path = new AssetPath
                {
                    Name = asset.Name,
                    Guid = asset.Guid,
                    Bundle = bundle
                };
                return true;
            }

            path = default;
            return false;
        }

        public bool TryGetByGuid(string guid, out AssetPath path)
        {
            if (_mapByGuid.TryGetValue(guid, out var asset))
            {
                var bundle = _mapBundle[asset.Bundle];

                path = new AssetPath
                {
                    Guid = asset.Guid,
                    Name = asset.Name,
                    Bundle = bundle
                };
                return true;
            }

            path = default;
            return false;
        }

        public bool TryGetByTag(string tag, List<AssetPath> result)
        {
            if (_mapByTags.TryGetValue(tag, out var assets))
            {
                foreach (var asset in assets)
                {
                    var bundle = _mapBundle[asset.Bundle];
                    result.Add(new AssetPath
                    {
                        Name = asset.Name,
                        Guid = asset.Guid,
                        Bundle = bundle
                    });
                }

                return true;
            }

            return false;
        }

        public bool TryGetAssets(AssetFilterFunc filter, List<AssetPath> result)
        {
            var found = false;
            foreach (var asset in _manifest.Assets)
            {
                if (filter(new AssetFilter
                {
                    Name = asset.Name,
                    Bundle = asset.Bundle,
                    Tags = asset.Tags,
                    Guid = asset.Guid
                }))
                {
                    found = true;
                    result.Add(new AssetPath
                    {
                        Name = asset.Name,
                        Guid = asset.Guid,
                        Bundle = _mapBundle[asset.Bundle]
                    });
                }
            }

            return found;
        }

        private void ProcessBundle(Bundle bundle, StringBuilder path)
        {
            if (_settings.UseLocalBundles)
            {
                bundle.Uri = $"{_settings.LocalBundlePath}/{bundle.Name}".Replace('\\', '/');
            }
            else
            {
                bundle.Uri = ProcessUri(bundle.Uri, path);
            }
        }

        private string ProcessUri(string uri, StringBuilder path)
        {
            path.Length = 0;
            path.Append(uri);
            foreach (var pair in _mapKeyPath)
            {
                string value = pair.Value.Value;
                if (pair.Value.Type != AssetPathKeyType.Embed)
                {
                    value = _settings[pair.Value.Type];
                }

                path.Replace(pair.Key, value);
            }

            return path.ToString();
        }
    }

    public struct AssetPath
    {
        public string Name;
        public string Guid;
        public Bundle Bundle;
    }
}
