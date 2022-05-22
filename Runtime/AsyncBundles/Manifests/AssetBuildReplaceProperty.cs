using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Manifests
{
    [Serializable]
    public abstract class AssetBuildReplaceProperty : ScriptableObject
    {
        public abstract string Key { get; }
        public abstract string Value { get; }

#if UNITY_EDITOR

        void OnEnable()
        {
            Cache.Properties = null;
        }

        void OnDestroy()
        {
            Cache.Properties = null;
        }

        public static class Cache
        {
            public static AssetBuildReplaceProperty[] Properties;
        }
#endif
    }
}
