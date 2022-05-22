using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Presets
{
    [Serializable]
    public class AssetGroupBuildProcessor : ScriptableObject
    {
        public virtual void OnPreBuild(AssetGroup group)
        {
        }

        public virtual void OnPostBuild(AssetGroup group)
        {
        }
    }
}
