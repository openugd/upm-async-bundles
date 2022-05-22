using UnityEngine;

namespace OpenUGD.AsyncBundles.Manifests
{
    public class AssetProviderSettings
    {
        private readonly string[] _path = new string[typeof(AssetPathKeyType).GetEnumValues().Length];

        public string this[AssetPathKeyType type]
        {
            get => _path[(int) type];
            set => _path[(int) type] = value;
        }

        public bool UseLocalBundles { get; set; }
        public string LocalBundlePath { get; set; }

        public static AssetProviderSettings Create()
        {
            var result = new AssetProviderSettings
            {
                [AssetPathKeyType.DataPath] = Application.dataPath,
                [AssetPathKeyType.StreamingAssets] = Application.streamingAssetsPath,
                [AssetPathKeyType.PersistentDataPath] = Application.persistentDataPath,
                UseLocalBundles = AsyncAssets.Settings.SourceMode == AsyncAssets.SourceMode.LocalBundles,
                LocalBundlePath = AsyncAssets.Settings.BuildPath
            };
            return result;
        }
    }
}
