using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Manifests
{
    [Serializable]
    public class AssetManifestFile : ScriptableObject
    {
        public AssetsManifest Manifest;
    }
}
