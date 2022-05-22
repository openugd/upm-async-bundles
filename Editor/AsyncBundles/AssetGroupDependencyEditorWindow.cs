using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUGD.AsyncBundles
{
    public class AssetGroupDependencyEditorWindow : EditorWindow
    {
        [MenuItem(AssetsPresetUtils.MenuItem + "/Dependency Resolver")]
        public static void Open()
        {
            GetWindow<AssetGroupDependencyEditorWindow>("Dependency Resolver").Show(true);
        }

        private static bool _showUnresolved;
        private Unresolved[] _unResolved;
        private Vector2 _unResolvedScroll;
        private int _extIndex;
        private string[] _extensions;

        void OnEnable()
        {
            minSize = new Vector2(720, 500);
            _extIndex = -1;
        }

        void OnDisable()
        {
        }

        void OnGUI()
        {
            //if (GUILayout.Button("Find Dependencies"))
            if (_unResolved == null)
            {
                var unResolverDependencies = new Dictionary<string, HashSet<string>>();
                var ext = new HashSet<string>();

                var preset = AssetsPresetUtils.Get();
                foreach (var presetGroup in preset.Groups)
                {
                    if (presetGroup.PackToBundle || AsyncAssets.Settings.DependencyResolverUseDisableGroups)
                    {
                        foreach (var assetInfo in presetGroup.Assets)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(assetInfo.Guid);
                            var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                            if (asset != null && AssetsPresetUtils.AvailableAsset(asset))
                            {
                                var dependencies = AssetDatabase.GetDependencies(path, true);
                                foreach (var dependency in dependencies)
                                {
                                    if (AssetsPresetUtils.AvailableAssetPath(dependency))
                                    {
                                        ext.Add(Path.GetExtension(dependency));
                                        var dependencyGuid = AssetDatabase.AssetPathToGUID(dependency);
                                        if (AssetsPresetUtils.GetAssetInfo(preset, dependencyGuid) == null)
                                        {
                                            HashSet<string> hasSet;
                                            if (!unResolverDependencies.TryGetValue(dependencyGuid, out hasSet))
                                            {
                                                unResolverDependencies[dependencyGuid] = hasSet = new HashSet<string>();
                                            }

                                            hasSet.Add(assetInfo.Guid);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                _extensions = new[] {"---", ""}.Concat(ext).ToArray();
                _unResolved = unResolverDependencies
                    .Select(guid => new Unresolved {Guid = guid.Key, AssetGuids = guid.Value.ToArray()}).ToArray();
            }

            if (_unResolved != null && _unResolved.Length != 0)
            {
                EditorGUILayout.Separator();

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var preset = AssetsPresetUtils.Get();
                var groups = preset.Groups;
                if (groups != null && groups.Length != 0)
                {
                    var names = groups.Select(g => g == null ? "" : g.name).ToArray();
                    var newValue = EditorGUILayout.Popup("Move To: ", -1, names);
                    if (newValue != -1)
                    {
                        var group = groups[newValue];
                        Undo.RecordObject(group, "Resolve objects");
                        foreach (var unresolved in _unResolved)
                        {
                            if (unresolved.IsChecked)
                            {
                                var path = AssetDatabase.GUIDToAssetPath(unresolved.Guid);
                                if (AssetDatabase.LoadAssetAtPath(path, typeof(Object)) != null)
                                {
                                    AssetsPresetUtils.Add(unresolved.Guid, group, preset);
                                }
                            }
                        }

                        AssetsPresetUtils.Save(preset);

                        _unResolved = null;
                        _extensions = null;
                        _extIndex = -1;
                    }
                }

                if (_unResolved != null && _unResolved.Length != 0)
                {
                    EditorGUILayout.Separator();
                    _showUnresolved = EditorGUILayout.Foldout(_showUnresolved, "UnResolved: " + _unResolved.Length);
                    EditorGUILayout.Separator();

                    if (_showUnresolved)
                    {
                        GUILayout.BeginHorizontal(EditorStyles.helpBox);
                        if (GUILayout.Button("select", EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                        {
                            foreach (Unresolved unresolved in _unResolved)
                            {
                                var path = AssetDatabase.GUIDToAssetPath(unresolved.Guid);
                                if (ResolveFilter(path))
                                {
                                    unresolved.IsChecked = true;
                                }
                            }
                        }

                        if (GUILayout.Button("unselect", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                        {
                            foreach (Unresolved unresolved in _unResolved)
                            {
                                var path = AssetDatabase.GUIDToAssetPath(unresolved.Guid);
                                if (ResolveFilter(path))
                                {
                                    unresolved.IsChecked = false;
                                }
                            }
                        }

                        GUILayout.Space(16f);
                        _extIndex = EditorGUILayout.Popup(_extIndex, _extensions);
                        GUILayout.EndHorizontal();

                        _unResolvedScroll = EditorGUILayout.BeginScrollView(_unResolvedScroll);

                        foreach (var unresolved in _unResolved)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(unresolved.Guid);
                            if (ResolveFilter(path))
                            {
                                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                                if (asset != null)
                                {
                                    var objContent = new GUIContent("");
                                    objContent.tooltip = path;

                                    var height = 16f;
                                    if (unresolved.IsExpanded)
                                    {
                                        height += unresolved.AssetGuids.Length * 16 + 16;
                                    }

                                    var rect = EditorGUILayout.GetControlRect(true, height);

                                    var toggleRect = rect;
                                    toggleRect.width = 20f;
                                    var foldRect = rect;
                                    foldRect.xMin += 20;
                                    var objRect = foldRect;
                                    objRect.xMin += 20;
                                    objRect.height = 16;
                                    foldRect.width = 16f;
                                    foldRect.height = 16f;
                                    unresolved.IsChecked = GUI.Toggle(toggleRect, unresolved.IsChecked, objContent);
                                    unresolved.IsExpanded = EditorGUI.Foldout(foldRect, unresolved.IsExpanded,
                                        GUIContent.none);
                                    EditorGUI.ObjectField(objRect, objContent, asset, asset.GetType(), false);
                                    if (unresolved.IsExpanded)
                                    {
                                        for (var i = 0; i < unresolved.AssetGuids.Length; i++)
                                        {
                                            string assetGuid = unresolved.AssetGuids[i];
                                            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                                            var assetValue =
                                                AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
                                            var objR = objRect;
                                            objR.xMin += 16;
                                            objR.xMax -= 4;
                                            objR.yMin += 16 + 8 + i * 16;
                                            objR.height = 16;
                                            var group = AssetsPresetUtils.GetGroup(preset, assetGuid);
                                            if (group != null)
                                            {
                                                var groupRect = objR;
                                                groupRect.width = 150;
                                                if (GUI.Button(groupRect,
                                                    new GUIContent(group.name) {tooltip = group.name},
                                                    EditorStyles.miniButton))
                                                {
                                                    Selection.activeObject = group;
                                                }

                                                objR.xMin += 150;
                                            }

                                            EditorGUI.ObjectField(objR, new GUIContent {tooltip = assetPath},
                                                assetValue, assetValue.GetType(), false);
                                        }
                                    }
                                }
                            }
                        }

                        EditorGUILayout.EndScrollView();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            else
            {
                EditorGUILayout.Separator();
                GUILayout.Label("not found");
            }
        }

        private bool ResolveFilter(string path)
        {
            if (_extIndex <= 1) return true;
            return Path.GetExtension(path) == _extensions[_extIndex];
        }

        private class Unresolved
        {
            public string Guid;
            public string[] AssetGuids;
            public bool IsChecked;
            public bool IsExpanded;
        }

        private class Filter
        {
            public string Name;
            public Func<string, bool> Resolver;

            public Filter(string name, Func<string, bool> resolver)
            {
                Name = name;
                Resolver = resolver;
            }
        }
    }
}
