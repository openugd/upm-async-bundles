using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Presets
{
    [Serializable]
    public abstract class AssetGroupBuildCondition : ScriptableObject
    {
        public abstract bool Success { get; }
    }
}
