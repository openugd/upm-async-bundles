using System;
using OpenUGD.AsyncBundles.Presets;

namespace OpenUGD.AsyncBundles.Manifests
{
    [Serializable]
    public class Bundle
    {
        public string Name;
        public string Hash;
        public string[] Dependencies;
        public string Uri;
        public long FileSize;
        public UnloadType UnloadType;
        public uint Crc;
        public float DelayToUnload;

        [field: NonSerialized] public Bundle[] DependencyBundles { get; set; }
    }
}
