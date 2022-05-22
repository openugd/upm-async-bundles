using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUGD.AsyncBundles.Manifests;
using OpenUGD.AsyncBundles.Presets;
using OpenUGD.AsyncBundles.ReplaceProperties;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUGD.AsyncBundles
{
    public class AssetsPresetUtils
    {
        public const string GroupDir = "Groups";
        public const string GroupPathDir = "GroupsPath";
        public const string MenuItem = "Window/AsyncBundles";

        public static readonly string[] AvailableAssetExtensions = new[]
        {
            ".prefab",
            ".png",
            ".jpg",
            ".psd",
            ".asset",
            ".tga",
            ".txt",
            ".json",
            ".bytes",
            ".mat",
            ".shader",
            ".unity",
            ".fbx",
            ".obj",
            ".controller",
            ".anim",
            ".ttf",
            ".FBX",
            ".wav",
            ".ogg",
            ".mp3",
            ".WAV",
            ".playable",
            ".spriteatlas"
        };

        private static AssetsPreset _preset;


        [MenuItem(MenuItem + "/ClearCache")]
        public static void ClearCache()
        {
            DirectoryUtility.DeleteDirectory(AsyncAssetsCaching.CacheDir);
            Caching.ClearCache();
        }

        [MenuItem(MenuItem + "/Select Preset", priority = -1002)]
        public static void SelectPreset()
        {
            var preset = Get();
            if (preset != null)
            {
                Selection.activeObject = preset;
            }
        }

        public static bool AvailableAssetGuid(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AvailableAssetPath(path);
        }

        public static bool AvailableAssetPath(string assetPath)
        {
            var path = assetPath.Replace("\\", "/");
            var ext = Path.GetExtension(path);
            var result = Array.IndexOf(AvailableAssetExtensions, ext) != -1;
            var preset = Get();
            if (preset != null)
            {
                var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(preset));
                if (!string.IsNullOrEmpty(dir))
                {
                    if (assetPath.Contains(dir.Replace("\\", "/"))) return false;
                }
            }

            return result;
        }

        public static bool AvailableAsset(UnityEngine.Object target)
        {
            var path = AssetDatabase.GetAssetPath(target);
            return AvailableAssetPath(path);
        }

        public static void Save(AssetsPreset preset)
        {
            EditorUtility.SetDirty(preset);
            AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(preset));
            importer.SaveAndReimport();

            foreach (var presetGroup in preset.Groups)
            {
                if (presetGroup != null)
                {
                    EditorUtility.SetDirty(presetGroup);
                    AssetImporter groupImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(presetGroup));
                    groupImporter.SaveAndReimport();
                }
            }
        }

        public static AssetGroupPath[] GetGroupPath(bool reload = false)
        {
            if (AssetGroupPath.EditorCache == null || reload ||
                AssetGroupPath.EditorCache.Any(groupPath => groupPath == null))
            {
                var presets = AssetDatabase.FindAssets($"t:{nameof(AssetGroupPath)}");
                var list = ListPool<string>.Pop();
                foreach (var preset in presets)
                {
                    list.Add(AssetDatabase.GUIDToAssetPath(preset));
                }

                var paths = ListPool<AssetGroupPath>.Pop();
                foreach (var path in list)
                {
                    paths.Add(AssetDatabase.LoadAssetAtPath<AssetGroupPath>(path));
                }

                var result = paths.ToArray();

                ListPool<string>.Push(list);
                ListPool<AssetGroupPath>.Push(paths);
                AssetGroupPath.EditorCache = result;
            }

            return AssetGroupPath.EditorCache;
        }

        public static AssetsPreset Get()
        {
            if (_preset == null)
            {
                var presets = AssetDatabase.FindAssets($"t:{nameof(AssetsPreset)}");
                if (presets.Length == 0)
                {
                    if (EditorUtility.DisplayDialog($"{nameof(AssetsPreset)} not found", "Please create", "ok"))
                    {
                        var preset = ScriptableObject.CreateInstance<AssetsPreset>();
                        preset.Groups = new AssetGroup[0];
                        AssetDatabase.CreateAsset(preset, $"Assets/{nameof(AssetsPreset)}.asset");
                        AssetDatabase.SaveAssets();
                        presets = new[]
                        {
                            AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(preset))
                        };
                    }

                    return null;
                }

                var path = AssetDatabase.GUIDToAssetPath(presets[0]);
                _preset = AssetDatabase.LoadAssetAtPath<AssetsPreset>(path);
            }

            return _preset;
        }


        public static void Fill(AssetsPreset preset, Dictionary<string, AssetGroup> map)
        {
            if (preset.Groups == null) return;

            foreach (var group in preset.Groups)
            {
                if (group != null && group.Assets != null)
                {
                    foreach (var asset in group.Assets)
                    {
                        map[asset.Guid] = group;
                    }
                }
            }
        }

        public static int ContainsGuid(AssetsPreset preset, IEnumerable<string> guid)
        {
            if (preset.Groups == null) return 0;
            var count = 0;
            foreach (var assetGroup in preset.Groups)
            {
                if (assetGroup == null) continue;
                if (assetGroup.Assets == null) assetGroup.Assets = new AssetInfo[0];

                foreach (var assetInfo in assetGroup.Assets)
                {
                    if (guid.Contains(assetInfo.Guid))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public static AssetInfo GetAssetInfo(AssetsPreset preset, string guid)
        {
            if (preset.Groups == null) return null;
            foreach (var assetGroup in preset.Groups)
            {
                if (assetGroup != null && assetGroup.Assets != null)
                {
                    foreach (var assetInfo in assetGroup.Assets)
                    {
                        if (assetInfo.Guid == guid)
                        {
                            return assetInfo;
                        }
                    }
                }
            }

            return null;
        }

        public static AssetInfo GetAndRemove(AssetsPreset preset, Object asset, bool useHistory = true)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            return GetAndRemove(preset, assetGuid, useHistory);
        }

        public static AssetInfo GetAndRemove(AssetsPreset preset, string guid, bool useHistory = true)
        {
            if (preset.Groups == null) return null;
            foreach (var assetGroup in preset.Groups)
            {
                if (assetGroup != null && assetGroup.Assets != null)
                {
                    foreach (var assetInfo in assetGroup.Assets)
                    {
                        if (assetInfo.Guid == guid)
                        {
                            if (useHistory)
                            {
                                Undo.RecordObject(assetGroup, $"{nameof(AssetsPreset)}.{nameof(GetAndRemove)}");
                            }

                            RemoveFromGroupInternal(assetGroup, assetInfo.Guid);

                            if (useHistory)
                            {
                                Save(preset);
                            }

                            return assetInfo;
                        }
                    }
                }
            }

            return null;
        }

        public static AssetGroup GetGroup(AssetsPreset preset, Object asset)
        {
            if (asset == null) return null;
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            return GetGroup(preset, assetGuid);
        }

        public static AssetGroup GetGroup(AssetsPreset preset, string guid)
        {
            if (preset.Groups == null) return null;
            foreach (var assetGroup in preset.Groups)
            {
                if (assetGroup != null && assetGroup.Assets != null)
                {
                    foreach (var assetInfo in assetGroup.Assets)
                    {
                        if (assetInfo.Guid == guid)
                        {
                            return assetGroup;
                        }
                    }
                }
            }

            return null;
        }

        public static void Add(UnityEngine.Object obj, AssetGroup assetGroup, AssetsPreset preset)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var path = AssetDatabase.GetAssetPath(obj);
            if (!AvailableAssetPath(path)) throw new ArgumentException($"{nameof(obj)} asset not valid to bundle");
            Add(AssetDatabase.AssetPathToGUID(path), assetGroup, preset);
        }

        public static void Add(string guid, AssetGroup assetGroup, AssetsPreset preset)
        {
            if (!AvailableAssetGuid(guid)) throw new ArgumentException($"{nameof(guid)} asset not valid to bundle");
            Add(new AssetInfo
            {
                Guid = guid,
                Name = GetAssetNameByGuid(guid)
            }, assetGroup, preset);
        }

        public static void Add(AssetInfo assetInfo, AssetGroup assetGroup, AssetsPreset preset)
        {
            var lastGroup = GetGroup(preset, assetInfo.Guid);
            if (lastGroup != assetGroup)
            {
                if (lastGroup != null)
                {
                    RemoveFromGroupInternal(lastGroup, assetInfo.Guid);
                }

                var list = assetGroup.Assets.ToList();
                list.Add(assetInfo);
                assetGroup.Assets = list.ToArray();
            }
        }

        public static AssetGroupPath NewPath(AssetsPreset preset)
        {
            var path = AssetDatabase.GetAssetPath(preset);
            var dir = Path.GetDirectoryName(path);
            var groupPath = Path.Combine(dir, AssetsPresetUtils.GroupPathDir);
            if (!Directory.Exists(groupPath))
            {
                var guid = AssetDatabase.CreateFolder(dir, AssetsPresetUtils.GroupPathDir);
                groupPath = AssetDatabase.GUIDToAssetPath(guid);
            }

            var unique = Hash128.Compute(GUID.Generate().ToString()).ToString().Substring(0, 6);
            var asset = ScriptableObject.CreateInstance<AssetGroupPath>();
            asset.BuildPath = string.Empty;
            asset.LoadPath = string.Empty;
            AssetDatabase.CreateAsset(asset, Path.Combine(groupPath, $"GroupPath_{unique}.asset"));

            EditorUtility.SetDirty(asset);

            return asset;
        }

        public static AssetGroup NewGroup(AssetsPreset preset)
        {
            var path = AssetDatabase.GetAssetPath(preset);
            var dir = Path.GetDirectoryName(path);
            var groupPath = Path.Combine(dir, AssetsPresetUtils.GroupDir);
            if (!Directory.Exists(groupPath))
            {
                var guid = AssetDatabase.CreateFolder(dir, AssetsPresetUtils.GroupDir);
                groupPath = AssetDatabase.GUIDToAssetPath(guid);
            }

            var unique = Hash128.Compute(GUID.Generate().ToString()).ToString().Substring(0, 6);
            var asset = ScriptableObject.CreateInstance<AssetGroup>();
            asset.Assets = new AssetInfo[0];
            asset.Processors = new AssetGroupBuildProcessor[0];
            asset.PackToBundle = true;
            AssetDatabase.CreateAsset(asset, Path.Combine(groupPath, $"Group_{unique}.asset"));

            var groups = preset.Groups.ToList();
            groups.Add(asset);
            preset.Groups = groups.ToArray();
            EditorUtility.SetDirty(preset);
            return asset;
        }

        public static T[] FindAssetBuildReplaceProperty<T>() where T : AssetBuildReplaceProperty
        {
            return AssetBuildPathReplacePropertyUtils.Find<T>();
        }

        public static BuildPathReplaceProperty[] ReplaceProperties()
        {
            return AssetBuildPathReplacePropertyUtils.Replaces();
        }

        public static void CheckToBundles(AssetsPreset preset)
        {
            foreach (AssetGroup presetGroup in preset.Groups)
            {
                if (presetGroup != null && presetGroup.Assets != null)
                {
                    foreach (AssetInfo assetInfo in presetGroup.Assets)
                    {
                        if (assetInfo != null)
                        {
                            var assetPath = AssetDatabase.GUIDToAssetPath(assetInfo.Guid);
                            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                            if (presetGroup.PackType == PackType.Together)
                            {
                                importer.assetBundleName = presetGroup.name;
                            }
                            else
                            {
                                importer.assetBundleName = assetInfo.Name;
                            }
                        }
                    }
                }
            }
        }

        public static void UnCheckToBundles(AssetsPreset preset)
        {
            foreach (string assetPath in AssetDatabase.GetAllAssetPaths())
            {
                if (Path.GetExtension(assetPath) != ".cs")
                {
                    AssetImporter importer = AssetImporter.GetAtPath(assetPath);

                    if (importer.assetBundleName != null)
                    {
                        importer.assetBundleName = null;
                    }
                }
            }
        }

        public static string GetAssetNameByGuid(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return FixAssetName(path);
        }

        public static string FixAssetName(string assetName)
        {
            return AssetEditorUtils.RenameToUnderscore(assetName).Replace('.', '_');
        }

        public static void FixAssetsName(AssetsPreset preset)
        {
            UndoPreset(preset);
            foreach (AssetGroup presetGroup in preset.Groups)
            {
                if (presetGroup != null && presetGroup.Assets != null)
                {
                    foreach (AssetInfo assetInfo in presetGroup.Assets)
                    {
                        if (assetInfo != null)
                        {
                            assetInfo.Name = FixAssetName(assetInfo.Name);
                        }
                    }
                }
            }

            Save(preset);
        }

        public static Color GetAssetGroupColor(AssetsPreset preset, AssetGroup group)
        {
            var index = Array.IndexOf(preset.Groups, group);
            var hue = (float) index / preset.Groups.Length;
            var saturation = ((uint) group.name.GetHashCode() % 360) / 360f;
            var color = Color.HSVToRGB(hue, 0.7f + (saturation * 0.2f), 1.5f);
            color.a = 1;
            return color;
        }

        private static void RemoveFromGroupInternal(AssetGroup assetGroup, string guid)
        {
            var list = assetGroup.Assets.ToList();
            list.RemoveAll(f => f.Guid == guid);
            assetGroup.Assets = list.ToArray();
        }

        public static void UndoPreset(AssetsPreset preset)
        {
            Undo.RecordObjects(preset.Groups.Where(g => g != null).Cast<Object>().ToArray(), nameof(UndoPreset));
        }
    }
}
