using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Presets
{
    [Serializable]
    [CreateAssetMenu(fileName = nameof(AssetGroupPath), menuName = "Assets/GroupPath")]
    public class AssetGroupPath : ScriptableObject
    {
        public string BuildPath;
        public string LoadPath;

#if UNITY_EDITOR
        public static AssetGroupPath[] EditorCache;

        void OnEnable()
        {
            EditorCache = null;
        }

        void OnDestroy()
        {
            EditorCache = null;
        }
#endif
    }
}
