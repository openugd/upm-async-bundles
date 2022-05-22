using System;
using System.Collections.Generic;
using System.Linq;
using OpenUGD.AsyncBundles.Presets;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OpenUGD.AsyncBundles
{
    [InitializeOnLoad]
    public static class AssetInspectorEditor
    {
        private static readonly Dictionary<string, AssetGroup> TempGuidGroup = new Dictionary<string, AssetGroup>();
        private static readonly HashSet<AssetGroup> TempAssetGroups = new HashSet<AssetGroup>();
        private static readonly AssetGroup[] TempAssetGroupArray = new AssetGroup[1];
        private static string[] TempGroupNames = new string[0];

        static AssetInspectorEditor()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += EditorOnFinishedDefaultHeaderGui;
        }

        private static void EditorOnFinishedDefaultHeaderGui(UnityEditor.Editor editor)
        {
            var preset = AssetsPresetUtils.Get();
            if (preset != null && preset.Groups != null)
            {
                var targets = ListPool<Object>.Pop(editor.targets.Where(AssetsPresetUtils.AvailableAsset));

                if (targets.Count != 0)
                {
                    preset.Groups = preset.Groups.Where(g => g != null).ToArray();

                    AssetEditorUtils.PushColor();
                    GUI.color = new Color(0.7f, 0.7f, 0.7f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    TempGuidGroup.Clear();
                    TempAssetGroups.Clear();

                    AssetsPresetUtils.Fill(preset, TempGuidGroup);

                    var guids = ListPool<string>.Pop(targets.Select(t =>
                    {
                        var path = AssetDatabase.GetAssetPath(t);
                        var guid = AssetDatabase.AssetPathToGUID(path);
                        return guid;
                    }));

                    var hasMiss = false;
                    foreach (var guid in guids)
                    {
                        AssetGroup assetGroup;
                        if (TempGuidGroup.TryGetValue(guid, out assetGroup))
                        {
                            TempAssetGroups.Add(assetGroup);
                        }
                        else
                        {
                            hasMiss = true;
                        }
                    }

                    var label = EditorGUIUtility.IconContent("Assembly Icon");
                    label.text = "Bundle";

                    var groupNames = ListPool<string>.Pop();
                    groupNames.Add("-Remove-");
                    groupNames.Add("");
                    foreach (var group in preset.Groups)
                    {
                        if (group != null)
                        {
                            groupNames.Add(group.name);
                        }
                    }

                    if (groupNames.Count != TempGroupNames.Length)
                    {
                        TempGroupNames = groupNames.ToArray();
                    }
                    else
                    {
                        groupNames.CopyTo(TempGroupNames);
                    }

                    var index = -1;
                    if (TempAssetGroups.Count == 1 && !hasMiss)
                    {
                        TempAssetGroups.CopyTo(TempAssetGroupArray);
                        var currentGroup = TempAssetGroupArray[0].name;
                        index = Array.IndexOf(TempGroupNames, currentGroup);
                    }

                    EditorGUILayout.BeginHorizontal();
                    var newIndex = EditorGUILayout.Popup(label, index, TempGroupNames);
                    if (index != -1)
                    {
                        if (GUILayout.Button(new GUIContent("S")
                        {
                            tooltip = "SELECT GROUP"
                        }, EditorStyles.miniButtonLeft, GUILayout.Width(16)))
                        {
                            foreach (var group in preset.Groups)
                            {
                                if (group.name == TempGroupNames[index])
                                {
                                    Selection.activeObject = group;
                                    break;
                                }
                            }
                        }

                        if (GUILayout.Button(new GUIContent("D")
                        {
                            tooltip = "SELECT ALL DEPENDENCIES"
                        }, EditorStyles.miniButtonRight, GUILayout.Width(16)))
                        {
                            AssetDependencyEditorWindow.Open(targets.ToArray());
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    if (newIndex != index)
                    {
                        AssetGroup currentGroup = null;
                        foreach (var group in preset.Groups)
                        {
                            if (group.name == TempGroupNames[newIndex])
                            {
                                currentGroup = group;
                                break;
                            }
                        }

                        var groups = ListPool<AssetGroup>.Pop();
                        var assetInfos = ListPool<AssetInfo>.Pop();
                        foreach (var guid in guids)
                        {
                            AssetGroup group = AssetsPresetUtils.GetGroup(preset, guid);
                            groups.Add(group);
                        }

                        if (groups.Count != 0)
                        {
                            var array = groups.Where(g => g != null).Cast<UnityEngine.Object>().ToArray();
                            if (array.Length != 0)
                            {
                                Undo.RecordObjects(array,
                                    $"{nameof(AssetsPreset)}.{nameof(AssetsPresetUtils.GetAndRemove)}");
                            }
                        }

                        foreach (var guid in guids)
                        {
                            var assetInfo = AssetsPresetUtils.GetAndRemove(preset, guid);
                            if (assetInfo == null)
                            {
                                assetInfo = new AssetInfo
                                {
                                    Guid = guid,
                                    Name = AssetsPresetUtils.GetAssetNameByGuid(guid)
                                };
                            }

                            assetInfos.Add(assetInfo);
                        }

                        if (currentGroup != null)
                        {
                            foreach (var assetInfo in assetInfos)
                            {
                                AssetsPresetUtils.Add(assetInfo, currentGroup, preset);
                            }
                        }

                        AssetsPresetUtils.Save(preset);

                        ListPool<AssetInfo>.Push(assetInfos);
                        ListPool<AssetGroup>.Push(groups);
                    }

                    ListPool<string>.Push(guids);

                    ListPool<string>.Push(groupNames);

                    EditorGUILayout.EndVertical();
                    AssetEditorUtils.PopColor();
                }

                ListPool<Object>.Push(targets);
            }
        }
    }
}
