using System;
using UnityEngine;

namespace OpenUGD.AsyncBundles.Presets
{
    [Serializable]
    [CreateAssetMenu(menuName = "Assets/Preset", fileName = "AssetsPreset.asset")]
    public class AssetsPreset : ScriptableObject
    {
        public BundleCompression BundleCompression;
        [Range(1, 100)] public int NumberOfParallelDownloads = 10;
        public AssetGroup[] Groups;

        [Range(0, 120), Tooltip("WWW RetryCount")]
        public int RetryCount;

        [Range(0, 120), Tooltip("WWW Retry Delay In Seconds")]
        public float RetryDelay = 1;

        [Range(1, 600),
         Tooltip("Sets UnityWebRequest to attempt to abort after the number of seconds in timeout have passed.")]
        public int Timeout = 5;
    }
}
