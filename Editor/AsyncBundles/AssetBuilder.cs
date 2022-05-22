using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OpenUGD.AsyncBundles.Manifests;
using OpenUGD.AsyncBundles.Presets;
using OpenUGD.AsyncBundles.ReplaceProperties;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AssetBuilder
    {
        public const string ResourcesDir = "/Resources/";
        public const string ResourcesDirName = "Resources";
        private const string EmbedPath = "{streamingAssetsPath}";
        public static string BuildPath => AsyncAssets.Settings.BuildPath;

        public static void Clear()
        {
            Exception exception = null;
            var count = 10;
            while (count-- > 0)
            {
                try
                {
                    if (Directory.Exists(BuildPath))
                    {
                        Directory.Delete(BuildPath, true);
                    }

                    return;
                }
                catch (Exception e)
                {
                    exception = e;
                    Thread.Sleep(10);
                }
            }

            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Exception", exception.Message, "close");
        }

        public static void Build(AssetsPreset preset, bool forceEmbedBundles = false)
        {
            var options = BuildAssetBundleOptions.None;
            if (preset.BundleCompression == BundleCompression.Uncompressed)
            {
                options |= BuildAssetBundleOptions.UncompressedAssetBundle;
            }
            else
            {
                options |= BuildAssetBundleOptions.ChunkBasedCompression;
            }

            var output = BuildPath;
            var assetMap = new Dictionary<string, List<AssetFile>>();
            var groupMap = new Dictionary<string, AssetGroup>();

            foreach (var group in preset.Groups)
            {
                if (group.CanBuild)
                {
                    foreach (var asset in group.Assets)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(asset.Guid);
                        if (string.IsNullOrEmpty(path) ||
                            AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object)) == null)
                        {
                            Debug.LogWarning(
                                $"Asset is null: '{asset.Guid}' with name:'{asset.Name}' in group:{group.name}");
                            continue;
                        }

                        AssetFileSource source = AssetFileSource.Bundle;
                        if (path.Contains(ResourcesDir))
                        {
                            source = AssetFileSource.Resources;
                        }

                        string bundleName = group.name;
                        if (group.PackType == PackType.Separately)
                        {
                            var assetName = AssetsPresetUtils.FixAssetName(asset.Name);
                            //bundleName = $"{assetName}_{asset.Guid}";
                            bundleName = $"{assetName}";
                        }

                        bundleName = AssetsPresetUtils.FixAssetName(bundleName);

                        List<AssetFile> assetFiles;
                        if (!assetMap.TryGetValue(bundleName, out assetFiles))
                        {
                            assetMap[bundleName] = assetFiles = new List<AssetFile>();
                        }

                        groupMap[bundleName] = group;
                        assetFiles.Add(new AssetFile
                        {
                            Guid = asset.Guid,
                            Name = asset.Name,
                            Tags = asset.Tags,
                            BundleName = bundleName,
                            FileName = path,
                            Group = group,
                            Source = source
                        });
                    }
                }
            }

            var assetBundles = new List<AssetBundleBuild>();
            foreach (var pair in assetMap)
            {
                var files = pair.Value.Where(f => f.Source == AssetFileSource.Bundle).Select(f => f.FileName).ToArray();
                var names = pair.Value.Where(f => f.Source == AssetFileSource.Bundle).Select(f => f.Name).ToArray();
                if (files.Length != 0)
                {
                    assetBundles.Add(new AssetBundleBuild
                    {
                        assetBundleName = pair.Key,
                        assetNames = files,
                        addressableNames = names
                    });
                }
            }

            if (!Directory.Exists(output))
            {
                Directory.CreateDirectory(output);
                Thread.Sleep(10);
            }

            if (assetBundles.Count != 0)
            {
                foreach (AssetGroup assetGroup in groupMap.Values)
                {
                    if (assetGroup.Processors != null && assetGroup.Processors.Length != 0)
                    {
                        foreach (AssetGroupBuildProcessor processor in assetGroup.Processors)
                        {
                            if (processor != null)
                            {
                                processor.OnPreBuild(assetGroup);
                            }
                        }
                    }
                }

                var manifest = BuildPipeline.BuildAssetBundles(output, assetBundles.ToArray(), options,
                    EditorUserBuildSettings.activeBuildTarget);

                foreach (AssetGroup assetGroup in groupMap.Values)
                {
                    if (assetGroup.Processors != null && assetGroup.Processors.Length != 0)
                    {
                        foreach (AssetGroupBuildProcessor processor in assetGroup.Processors)
                        {
                            if (processor != null)
                            {
                                processor.OnPostBuild(assetGroup);
                            }
                        }
                    }
                }

                var bundleSize = new Dictionary<string, long>();
                var bundleCrc = new Dictionary<string, uint>();
                foreach (var bundle in manifest.GetAllAssetBundles())
                {
                    var source = Path.Combine(output, bundle);
                    var destination =
                        Path.Combine(ProcessBuildPath(forceEmbedBundles ? EmbedPath : groupMap[bundle].Path.BuildPath),
                            bundle);
                    var destinationDir = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(destinationDir))
                    {
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }
                    }

                    var hash = manifest.GetAssetBundleHash(bundle).ToString();
                    File.Copy(source, BundleUri(destination, hash), true);
                    var fileSize = new FileInfo(source).Length;
                    bundleSize[bundle] = fileSize;

                    uint crc;
                    if (BuildPipeline.GetCRCForAssetBundle(source, out crc))
                    {
                        bundleCrc[bundle] = crc;
                    }

                    groupMap[bundle].OnBundleCopied(destination, bundle);
                }

                var manifestBundles = new Dictionary<string, Bundle>();
                foreach (var bundle in manifest.GetAllAssetBundles())
                {
                    var group = groupMap[bundle];
                    var hash = manifest.GetAssetBundleHash(bundle).ToString();
                    var delayToUnload = group.DelayToUnload;
                    manifestBundles.Add(bundle, new Bundle
                    {
                        Name = bundle,
                        UnloadType = group.UnloadType,
                        Uri = ProcessLoadPath(Path.Combine(forceEmbedBundles ? EmbedPath : group.Path.LoadPath,
                            BundleUri(bundle, hash)).Replace('\\', '/')),
                        DelayToUnload = delayToUnload,
                        Hash = hash,
                        Crc = bundleCrc[bundle],
                        FileSize = bundleSize[bundle]
                    });
                }

                foreach (var bundle in manifest.GetAllAssetBundles())
                {
                    var dependencies = manifest.GetDirectDependencies(bundle);
                    var list = new List<Bundle>();
                    foreach (var dep in dependencies)
                    {
                        list.Add(manifestBundles[dep]);
                    }

                    manifestBundles[bundle].Dependencies = list.Select(b => b.Name).ToArray();
                }

                var bundles = manifestBundles.Values.ToArray();

                var manifestAssets = new List<Asset>();
                foreach (var pair in assetMap)
                {
                    foreach (var assetFile in pair.Value)
                    {
                        if (assetFile.Source == AssetFileSource.Resources)
                        {
                            var name = assetFile.FileName;
                            var index = name.LastIndexOf(ResourcesDir);
                            name = name.Remove(index + ResourcesDir.Length,
                                name.Length - (index + ResourcesDir.Length));
                            var ext = Path.GetExtension(name);
                            if (!string.IsNullOrEmpty(ext))
                            {
                                var i = name.LastIndexOf(ext);
                                if (i == name.Length - ext.Length)
                                {
                                    name = name.Substring(0, name.Length - ext.Length);
                                }
                            }

                            manifestAssets.Add(new Asset
                            {
                                Name = name,
                                Guid = assetFile.Guid,
                                Tags = assetFile.Tags
                            });
                        }
                        else
                        {
                            manifestAssets.Add(new Asset
                            {
                                Name = assetFile.Name,
                                Guid = assetFile.Guid,
                                Bundle = pair.Key,
                                Tags = assetFile.Tags
                            });
                        }
                    }
                }

                var assetPathKeys = new List<AssetPathKey>();
                foreach (var property in AssetsPresetUtils.ReplaceProperties())
                {
                    if (property.Type != BuildPathReplacePropertyType.EditorOnly)
                    {
                        AssetPathKeyType keyType = property.Type.ToKeyType();
                        string value = property.Type == BuildPathReplacePropertyType.EmbedToBuild
                            ? property.Value()
                            : null;

                        assetPathKeys.Add(new AssetPathKey
                        {
                            Key = property.Key,
                            Type = keyType,
                            Value = value
                        });
                    }
                }

                var assetsManifest = new AssetsManifest
                {
                    NumberOfParallelDownloads = preset.NumberOfParallelDownloads,
                    RetryCount = preset.RetryCount,
                    RetryDelay = preset.RetryDelay,
                    Timeout = preset.Timeout,
                    Bundles = bundles,
                    Assets = manifestAssets.ToArray(),
                    KeyPaths = assetPathKeys.ToArray()
                };

                var presetPath = AssetDatabase.GetAssetPath(preset);
                var presetDir = Path.GetDirectoryName(presetPath);
                var presetResourcesDir = Path.Combine(presetDir, ResourcesDirName);
                if (!Directory.Exists(presetResourcesDir))
                {
                    Directory.CreateDirectory(presetResourcesDir);
                    Thread.Sleep(10);
                    AssetDatabase.Refresh();
                }

                var manifestFile = ScriptableObject.CreateInstance<AssetManifestFile>();
                manifestFile.Manifest = assetsManifest;
                var filePath = Path.Combine(presetResourcesDir, $"{nameof(AssetManifestFile)}.asset");
                if (File.Exists(filePath))
                {
                    AssetDatabase.DeleteAsset(filePath);
                }

                AssetDatabase.CreateAsset(manifestFile, filePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        public static string BundleUri(string path, string hash)
        {
            return $"{path}_{hash}";
        }

        public static string ProcessBuildPath(string path)
        {
            foreach (var property in AssetsPresetUtils.ReplaceProperties())
            {
                path = path.Replace(property.Key, property.Value());
            }

            return path;
        }

        public static string ProcessLoadPath(string path)
        {
            foreach (var property in AssetsPresetUtils.ReplaceProperties())
            {
                if (property.Type == BuildPathReplacePropertyType.EditorOnly)
                {
                    path = path.Replace(property.Key, property.Value());
                }
            }

            return path;
        }

        public static string ProcessPreviewPath(string path)
        {
            if (path != null)
            {
                foreach (var property in AssetsPresetUtils.ReplaceProperties())
                {
                    if (property.Key != null && property.Value != null)
                    {
                        path = path.Replace(property.Key, property.Value());
                    }
                }
            }

            return path;
        }
    }
}
