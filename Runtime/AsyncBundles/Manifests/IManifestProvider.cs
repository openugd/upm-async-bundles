using System.Collections.Generic;

namespace OpenUGD.AsyncBundles.Manifests
{
    public interface IManifestProvider
    {
        long TotalSize { get; }
        AssetsManifest Manifest { get; }
        bool TryGetByGuid(string guid, out AssetPath path);
        bool TryGetBundle(string bundleName, out Bundle bundle);
        bool TryGetByName(string name, out AssetPath path);
        bool TryGetByTag(string tag, List<AssetPath> result);
        bool TryGetAssets(AssetFilterFunc filter, List<AssetPath> result);
    }

    public delegate bool AssetFilterFunc(AssetFilter filter);

    public struct AssetFilter
    {
        public string Name;
        public string Bundle;
        public string[] Tags;
        public string Guid;
    }
}
