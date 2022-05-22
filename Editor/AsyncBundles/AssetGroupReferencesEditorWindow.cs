using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUGD.AsyncBundles.Presets;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public class AssetGroupReferencesEditorWindow : EditorWindow
    {
        enum Status
        {
            None,
            Dependencies,
            AssetReference,
            Ready
        };

        struct Pair
        {
            public Object Asset;
            public AssetGroup Group;
            public Object[] References;
        }

        private static AssetsPreset _preset;

        private static readonly Dictionary<string, HashSet<string>> _dependencyMap =
            new Dictionary<string, HashSet<string>>();

        private static readonly Dictionary<string, HashSet<string>> _referencesMap =
            new Dictionary<string, HashSet<string>>();

        private static string[] _allAssets;
        private static int _index;
        private static IEnumerator _process;
        private static Status _status;
        private static int _assetsProcessed;
        private static int _assetsFound;
        private static int _assetTotal;
        private static Pair[] _unresolved;
        private static Vector2 _scroll;

        public static void Open(AssetsPreset preset)
        {
            _preset = preset;
            var window = GetWindow<AssetGroupReferencesEditorWindow>("References");
            window.Reset();
            window.Show(true);
        }

        void Reset()
        {
            _status = Status.None;
            _allAssets = AssetDatabase.GetAllAssetPaths().Where(p => p != null && !Directory.Exists(p)).ToArray();
        }

        void OnGUI()
        {
            if (_preset == null) return;
            if (_status == Status.None)
            {
                _process = Process();
                EditorApplication.update += OnFrame;
            }
            else
            {
                if (_process != null)
                {
                    if (_status == Status.Dependencies)
                    {
                        var rect = EditorGUILayout.GetControlRect(true);
                        EditorGUI.ProgressBar(rect, _index / (float) _allAssets.Length,
                            $"find dependencies, processed: {_index}/{_allAssets.Length} ({(int) ((_index / (float) _allAssets.Length) * 10000) / 100f}%)");
                    }
                    else
                    {
                        var rect = EditorGUILayout.GetControlRect(true);
                        EditorGUI.ProgressBar(rect, _assetsProcessed / (float) _assetTotal,
                            $"find asset references, processed: {_assetsProcessed}/{_assetTotal}, unresolved:{_assetsFound}");
                    }
                }
                else
                {
                    if (GUILayout.Button("Remove From Assets All"))
                    {
                        var assets = _unresolved.Select(p => p.Asset).ToArray();
                        var groups = new List<AssetGroup>();
                        foreach (Object asset in assets)
                        {
                            var group = AssetsPresetUtils.GetGroup(_preset, asset);
                            if (group != null)
                            {
                                groups.Add(group);
                            }
                        }

                        Undo.RecordObjects(groups.ToArray(), "AssetGroups");

                        foreach (var asset in assets)
                        {
                            AssetsPresetUtils.GetAndRemove(_preset, asset, false);
                        }

                        AssetsPresetUtils.Save(_preset);
                        _status = Status.None;
                    }

                    GUILayout.Label("UNRESOLVED:");
                    GUILayout.Space(16f);
                    _scroll = EditorGUILayout.BeginScrollView(_scroll);
                    if (_unresolved != null)
                    {
                        Pair toRemove = new Pair();
                        foreach (Pair pair in _unresolved)
                        {
                            AssetEditorUtils.PushColor(AssetsPresetUtils.GetAssetGroupColor(_preset, pair.Group));
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            GUILayout.Label(pair.Group.name);
                            AssetEditorUtils.PopColor();
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button("Remove From Asset", EditorStyles.miniButton, GUILayout.Width(150)))
                            {
                                toRemove = pair;
                                AssetsPresetUtils.GetAndRemove(_preset, pair.Asset);
                            }

                            EditorGUILayout.ObjectField(pair.Asset, typeof(UnityEngine.Object), false);
                            EditorGUILayout.EndHorizontal();
                            GUILayout.Space(8f);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(26f);
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            foreach (Object reference in pair.References)
                            {
                                EditorGUILayout.ObjectField(reference, typeof(UnityEngine.Object), false);
                            }

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                        }

                        if (toRemove.Asset != null)
                        {
                            var index = ArrayUtility.IndexOf(_unresolved, toRemove);
                            if (index != -1)
                            {
                                ArrayUtility.RemoveAt(ref _unresolved, index);
                            }
                        }

                        if (_unresolved.Length == 0)
                        {
                            GUILayout.Label("Empty, not found");
                        }
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void OnFrame()
        {
            if (_process != null)
            {
                if (!_process.MoveNext())
                {
                    _process = null;
                    EditorApplication.update -= OnFrame;
                }

                Repaint();
            }
        }

        private IEnumerator Process()
        {
            _index = 0;
            _status = Status.Dependencies;
            yield return null;

            foreach (var assetPath in _allAssets)
            {
                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                HashSet<string> set;
                if (!_dependencyMap.TryGetValue(assetGuid, out set))
                {
                    _dependencyMap[assetGuid] = set = new HashSet<string>();
                }

                var dependencies = AssetDatabase.GetDependencies(assetPath);
                foreach (var dependency in dependencies)
                {
                    var dependencyGuid = AssetDatabase.AssetPathToGUID(dependency);
                    set.Add(dependencyGuid);
                    HashSet<string> references;
                    if (!_referencesMap.TryGetValue(dependencyGuid, out references))
                    {
                        _referencesMap[dependencyGuid] = references = new HashSet<string>();
                    }

                    references.Add(assetGuid);
                }

                _index++;
                if ((_index % 20) == 0)
                    yield return null;
            }

            _status = Status.AssetReference;
            _assetTotal = _preset.Groups.Where(g => g != null).Select(g => g.Assets).Where(a => a != null)
                .Select(a => a.Length).Sum();
            _assetsProcessed = 0;
            _assetsFound = 0;
            var index = 0;
            var unresolved = new Dictionary<string, HashSet<string>>();
            foreach (AssetGroup assetGroup in _preset.Groups)
            {
                if (assetGroup != null && assetGroup.Assets != null)
                {
                    foreach (AssetInfo asset in assetGroup.Assets)
                    {
                        if (asset != null)
                        {
                            _assetsProcessed++;
                            HashSet<string> references;
                            if (_referencesMap.TryGetValue(asset.Guid, out references))
                            {
                                foreach (string guid in references)
                                {
                                    var assetInfo = AssetsPresetUtils.GetAssetInfo(_preset, guid);
                                    if (assetInfo == null)
                                    {
                                        _assetsFound++;
                                        HashSet<string> hashSet;
                                        if (!unresolved.TryGetValue(asset.Guid, out hashSet))
                                        {
                                            unresolved[asset.Guid] = hashSet = new HashSet<string>();
                                        }

                                        hashSet.Add(guid);
                                    }
                                }
                            }

                            index++;
                            if ((index % 20) == 0)
                                yield return null;
                        }
                    }
                }
            }

            _unresolved = unresolved.Select(a =>
            {
                var path = AssetDatabase.GUIDToAssetPath(a.Key);
                return new Pair
                {
                    Asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object)),
                    Group = AssetsPresetUtils.GetGroup(_preset, a.Key),
                    References = a.Value.Select(b =>
                    {
                        var bPath = AssetDatabase.GUIDToAssetPath(b);
                        return AssetDatabase.LoadAssetAtPath(bPath, typeof(UnityEngine.Object));
                    }).ToArray()
                };
            }).OrderBy(g => g.Group.name).ToArray();

            _status = Status.Ready;
        }
    }
}
