using System;

namespace OpenUGD.AsyncBundles.ReplaceProperties
{
    [Serializable]
    public enum BuildPathReplacePropertyType
    {
        StreamingAssetsPath = 0,
        DataPath = 1,
        PersistentDataPath = 2,
        EmbedToBuild = 3,
        EditorOnly = 4,
    }
}
