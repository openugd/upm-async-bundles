using System;
using System.Collections.Generic;
using System.Linq;
using OpenUGD.AsyncBundles.Presets;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUGD.AsyncBundles
{
    public class AssetDependencyEditorWindow : EditorWindow
    {
        private class Dependency
        {
            public Object Target;
            public bool IsChecked;
            public AssetGroup Group;
            public bool Available;
        }

        private enum View
        {
            Object,
            Path
        }

        private Dependency[] _targets;
        private Vector2 _scroll;
        private Object[] _objects;
        private bool _needToReset;
        private View _view;

        public static void Open(Object[] targets, bool select = false)
        {
            new AssetDependencyEditorWindow(targets, select).Show(true);
        }

        private AssetDependencyEditorWindow(Object[] targets, bool select)
        {
            var paths = targets.Select(AssetDatabase.GetAssetPath).ToArray();
            var dependenciesPath = AssetDatabase.GetDependencies(paths, true);
            var dependencies = new List<Object>();
            foreach (string p in dependenciesPath)
            {
                dependencies.Add(AssetDatabase.LoadAssetAtPath(p, typeof(Object)));
            }

            var objects = dependencies.Where(d => d != null).ToArray();

            if (select)
            {
                Selection.objects = objects;
            }

            _objects = objects;
            UpdateTargets();
        }

        private void UpdateTargets()
        {
            var preset = AssetsPresetUtils.Get();
            titleContent = new GUIContent("Dependencies");
            var objects = _objects.Where(t => t != null).OrderBy(t => t.GetType().Name).ToArray();
            _targets = new Dependency[objects.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                _targets[i] = new Dependency
                {
                    Target = objects[i],
                    Group = AssetsPresetUtils.GetGroup(preset, objects[i]),
                    IsChecked = false,
                    Available = AssetsPresetUtils.AvailableAsset(objects[i])
                };
            }

            Array.Sort(_targets,
                (l, r) => string.Compare(AssetDatabase.GetAssetPath(l.Target), AssetDatabase.GetAssetPath(r.Target),
                    StringComparison.InvariantCulture));

            _needToReset = false;
        }

        void OnGUI()
        {
            if (_needToReset)
            {
                UpdateTargets();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_targets != null)
            {
                GUILayout.Label("Dependency: " + _targets.Count(t => t != null));

                GUILayout.Space(16f);

                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    if (GUILayout.Button("select", EditorStyles.miniButtonLeft, GUILayout.Width(100)))
                    {
                        foreach (var target in _targets)
                        {
                            target.IsChecked = true;
                        }
                    }

                    if (GUILayout.Button("unselect", EditorStyles.miniButtonRight, GUILayout.Width(100)))
                    {
                        foreach (var target in _targets)
                        {
                            target.IsChecked = false;
                        }
                    }
                }
                GUILayout.Space(16f);
                _view = (View) EditorGUILayout.EnumPopup(_view, GUILayout.MaxWidth(100));
                GUILayout.Space(16f);

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
                        foreach (var dependency in _targets)
                        {
                            if (dependency.IsChecked)
                            {
                                AssetsPresetUtils.UndoPreset(preset);
                                if (AssetsPresetUtils.AvailableAsset(dependency.Target))
                                {
                                    AssetsPresetUtils.Add(dependency.Target, group, preset);
                                }
                            }
                        }

                        AssetsPresetUtils.Save(preset);
                        _needToReset = true;
                    }
                }

                GUILayout.EndHorizontal();

                var index = 0;
                foreach (var target in _targets)
                {
                    if (target != null)
                    {
                        index++;
                        var rect = EditorGUILayout.GetControlRect();
                        var toggleRect = rect;
                        toggleRect.width = 26;
                        var groupRect = rect;
                        groupRect.xMin += 26;
                        groupRect.width = 150;
                        var objRect = rect;
                        objRect.xMin += 26 + 150;

                        if (target.Available)
                        {
                            target.IsChecked = EditorGUI.Toggle(toggleRect, GUIContent.none, target.IsChecked);

                            if (target.Group != null)
                            {
                                AssetEditorUtils.PushColor(AssetsPresetUtils.GetAssetGroupColor(preset, target.Group));
                                if (GUI.Button(groupRect, target.Group.name, EditorStyles.miniButton))
                                {
                                    Selection.activeObject = target.Group;
                                }

                                AssetEditorUtils.PopColor();
                            }
                        }

                        if (_view == View.Object)
                        {
                            EditorGUI.ObjectField(objRect, target.Target, target.Target.GetType(), false);
                        }
                        else if (_view == View.Path)
                        {
                            EditorGUI.TextField(objRect, AssetDatabase.GetAssetPath(target.Target));
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
