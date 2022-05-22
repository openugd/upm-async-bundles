using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUGD.AsyncBundles.Manifests;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles.ReplaceProperties
{
    public static class AssetBuildPathReplacePropertyUtils
    {
        private static BuildPathReplaceProperty[] _cache;

        private static readonly BuildPathReplaceProperty[] _replaceProperties = new BuildPathReplaceProperty[]
        {
            new BuildPathReplaceProperty
            {
                Key = "{streamingAssetsPath}",
                Value = () => Application.streamingAssetsPath,
                Type = BuildPathReplacePropertyType.StreamingAssetsPath
            },
            new BuildPathReplaceProperty
            {
                Key = "{dataPath}",
                Value = () => Application.dataPath,
                Type = BuildPathReplacePropertyType.DataPath
            },
            new BuildPathReplaceProperty
            {
                Key = "{persistentDataPath}",
                Value = () => Application.persistentDataPath,
                Type = BuildPathReplacePropertyType.PersistentDataPath
            },
            new BuildPathReplaceProperty
            {
                Key = "{buildTarget}",
                Value = () => EditorUserBuildSettings.activeBuildTarget.ToString(),
                Type = BuildPathReplacePropertyType.EditorOnly
            },
            new BuildPathReplaceProperty
            {
                Key = "{subTarget}",
                Value = () => EditorUserBuildSettings.androidBuildSubtarget.ToString(),
                Type = BuildPathReplacePropertyType.EditorOnly
            },
            new BuildPathReplaceProperty
            {
                Key = "{library}",
                Value = () =>
                {
                    var dir = new DirectoryInfo(Application.dataPath).Parent;
                    return $"{dir.FullName}/Library";
                },
                Type = BuildPathReplacePropertyType.EditorOnly
            },
            new BuildPathReplaceProperty
            {
                Key = "{buildPath}",
                Value = () => AsyncAssets.Settings.BuildPath,
                Type = BuildPathReplacePropertyType.EditorOnly
            },
            new BuildPathReplaceProperty
            {
                Key = "{root}",
                Value = () =>
                {
                    var dir = new DirectoryInfo(Application.dataPath).Parent;
                    return $"{dir.FullName}";
                },
                Type = BuildPathReplacePropertyType.EditorOnly
            },
            new BuildPathReplaceProperty
            {
                Key = "{build}",
                Value = () =>
                {
#if DEBUG
                    return "debug";
#else
          return "release";
#endif
                },
                Type = BuildPathReplacePropertyType.EditorOnly
            }
        };

        public static T[] Find<T>() where T : AssetBuildReplaceProperty
        {
            var properties = new List<AssetBuildReplaceProperty>();
            var assetsGuid = AssetDatabase.FindAssets($"t:{nameof(AssetBuildReplaceProperty)}");
            foreach (string guid in assetsGuid)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var property =
                        AssetDatabase.LoadAssetAtPath(path, typeof(AssetBuildReplaceProperty)) as
                            AssetBuildReplaceProperty;
                    if (property != null)
                    {
                        properties.Add(property);
                    }
                }
            }

            return properties.OfType<T>().ToArray();
        }

        public static void Invalidate()
        {
            AssetBuildReplaceProperty.Cache.Properties = null;
            _cache = null;
        }

        public static BuildPathReplaceProperty[] Replaces()
        {
            if (AssetBuildReplaceProperty.Cache.Properties == null)
            {
                _cache = null;
                AssetBuildReplaceProperty.Cache.Properties = Find<AssetBuildReplaceProperty>();
            }

            if (_cache == null)
            {
                var properties = new List<BuildPathReplaceProperty>(_replaceProperties);
                foreach (AssetBuildReplaceProperty property in AssetBuildReplaceProperty.Cache.Properties)
                {
                    properties.Add(new BuildPathReplaceProperty
                    {
                        Key = property.Key,
                        Value = () => property.Value,
                        Type = BuildPathReplacePropertyType.EditorOnly
                    });
                }

                _cache = properties.ToArray();
            }

            return _cache;
        }
    }
}
