using System;

namespace OpenUGD.AsyncBundles.Manifests
{
    [Serializable]
    public class AssetsManifest
    {
        public int NumberOfParallelDownloads;
        public int RetryCount;
        public float RetryDelay;
        public int Timeout;
        public Asset[] Assets;
        public Bundle[] Bundles;
        public AssetPathKey[] KeyPaths;
    }
}
