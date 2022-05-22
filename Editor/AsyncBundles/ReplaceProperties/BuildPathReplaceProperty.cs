using System;

namespace OpenUGD.AsyncBundles.ReplaceProperties
{
    [Serializable]
    public struct BuildPathReplaceProperty
    {
        public string Key;
        public Func<string> Value;
        public BuildPathReplacePropertyType Type;
    }
}
