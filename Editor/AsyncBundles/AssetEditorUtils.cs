using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    public static class AssetEditorUtils
    {
        struct ColorInfo
        {
            public Color color;
            public Color backgroundColor;
            public Color contentColor;
        }

        private static readonly Stack<ColorInfo> _colorsStack = new Stack<ColorInfo>();
        private static readonly Stack<bool> _enableStack = new Stack<bool>();
        private static readonly Stack<int> _indentLevelStack = new Stack<int>();

        public static string RenameToUnderscore(string name)
        {
            string fileName = name;
            var newFileName = Regex.Replace(fileName, "(?<=[a-z0-9])[A-Z]", m => "_" + m.Value);
            newFileName = newFileName.Replace("-", "_");
            newFileName = newFileName.ToLowerInvariant();
            newFileName = newFileName.Replace(' ', '_');
            return newFileName;
        }

        public static void ExecuteInEditorUpdate(Action action)
        {
            void update()
            {
                EditorApplication.update -= update;
                action();
            }

            EditorApplication.update += update;
        }

        public static Color ToColor(string value, int range = 333)
        {
            if (value == null) return Color.white;
            var result = Color.HSVToRGB((((uint) value.GetHashCode()) % range) / (float) range, 0.5f, 1.5f);
            result.a = 1f;
            return result;
        }

        public static Color ToColor(int color)
        {
            var result = new Color32((byte) (color >> 24 & 0xFF), (byte) (color >> 16 & 0xFF),
                (byte) (color >> 8 & 0xFF), (byte) (color & 0xFF));
            return result;
        }

        public static int ToColor(Color32 color)
        {
            var r = color.r & 0xFF;
            var g = color.g & 0xFF;
            var b = color.b & 0xFF;
            var a = color.a & 0xFF;

            var rgba = (r << 24) + (g << 16) + (b << 8) + (a);
            return rgba;
        }

        public static void PushIndentLevel()
        {
            _indentLevelStack.Push(EditorGUI.indentLevel);
        }

        public static void PushIndentLevel(int newIndentLevel)
        {
            _indentLevelStack.Push(EditorGUI.indentLevel);
            EditorGUI.indentLevel = newIndentLevel;
        }

        public static void PopIndentLevel()
        {
            if (_indentLevelStack.Count != 0) EditorGUI.indentLevel = _indentLevelStack.Pop();
        }

        public static void PushEnable()
        {
            _enableStack.Push(GUI.enabled);
        }

        public static void PushEnable(bool newValue, bool inherited = true)
        {
            _enableStack.Push(GUI.enabled);
            GUI.enabled = newValue && GUI.enabled;
        }

        public static void PopEnable()
        {
            if (_enableStack.Count != 0)
            {
                GUI.enabled = _enableStack.Pop();
            }
        }

        public static void PushColor()
        {
            _colorsStack.Push(new ColorInfo
            {
                color = GUI.color,
                backgroundColor = GUI.backgroundColor,
                contentColor = GUI.contentColor,
            });
        }

        public static void PushColor(Color color)
        {
            _colorsStack.Push(new ColorInfo
            {
                color = GUI.color,
                backgroundColor = GUI.backgroundColor,
                contentColor = GUI.contentColor,
            });
            GUI.color = color;
        }

        public static void PopColor()
        {
            if (_colorsStack.Count != 0)
            {
                var colorInfo = _colorsStack.Pop();
                GUI.color = colorInfo.color;
                GUI.backgroundColor = colorInfo.backgroundColor;
                GUI.contentColor = colorInfo.contentColor;
            }
        }

        public static void Header(string text, float space = 0f)
        {
            GUILayout.BeginHorizontal();
            text = "<b>" + text + "</b>";
            GUILayout.Toggle(true, "☼ " + text, "dragtab", GUILayout.MinWidth(20f));
            GUILayout.EndHorizontal();
            GUILayout.Space(space);
        }

        public static void BeginVerticalHeader(string text)
        {
            Header(text, -4f);
            BeginVertical();
        }

        public static void EndVerticalHeader()
        {
            EndVertical();
        }

        public static void BeginVertical(bool selected = true, float space = 2f)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        }

        public static bool FoldoutHeader(string text, bool enable, float space = 3f)
        {
            var lastBackgroundColor = GUI.backgroundColor;
            if (!enable) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);

            text = "<b>" + text + "</b>";

            if (enable)
            {
                text = "\u25BC " + text;
            }
            else
            {
                text = "\u25BA " + text;
            }

            if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f))) enable = !enable;

            GUI.backgroundColor = lastBackgroundColor;
            if (!enable) GUILayout.Space(space);

            return enable;
        }

        public static void EndVertical(float space = 3f)
        {
            EditorGUILayout.EndVertical();
        }

        public static void BeginHorizontal(bool selected = true, float space = 0f)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.MinHeight(10f));
            GUILayout.Space(space);
        }

        public static void EndHorizontal(float space = 0f)
        {
            GUILayout.Space(space);
            EditorGUILayout.EndHorizontal();
        }

        public static int UpDownArrows(GUIContent label, int value, GUIStyle labelStyle, GUIStyle upArrow,
            GUIStyle downArrow)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 10);
            GUILayout.Label(label, labelStyle, GUILayout.Width(170));

            if (downArrow == null || upArrow == null)
            {
                upArrow = GUI.skin.FindStyle("Button");
                downArrow = upArrow;
            }

            if (GUILayout.Button("", upArrow, GUILayout.Width(16), GUILayout.Height(12)))
            {
                value++;
            }

            if (GUILayout.Button("", downArrow, GUILayout.Width(16), GUILayout.Height(12)))
            {
                value--;
            }

            GUILayout.Space(100);
            GUILayout.EndHorizontal();
            return value;
        }

        public static Rect RectLeft(ref Rect rect, float width)
        {
            var result = new Rect(rect)
            {
                width = width
            };
            rect.xMin += width;
            return result;
        }

        public static Rect RectRight(ref Rect rect, float width)
        {
            var result = new Rect(rect)
            {
                width = width
            };
            rect.xMax -= width;
            return result;
        }

        public static Rect RectTop(ref Rect rect, float height)
        {
            var result = new Rect(rect)
            {
                height = height
            };
            rect.yMin += height;
            return result;
        }

        public static Rect RectBottom(ref Rect rect, float height)
        {
            var result = new Rect(rect)
            {
                height = height
            };
            rect.yMax -= height;
            return result;
        }

        public static object GetPropertyOwner(SerializedProperty property)
        {
            return GetPropertyOwner(property.serializedObject.targetObject, property.propertyPath);
        }

        public static int GetPropertyIndexInArray(SerializedProperty property)
        {
            return GetPropertyIndexInArray(property.propertyPath, 0);
        }

        public static object GetPropertyOwner(object value, string path)
        {
            var split = path.Split('.');
            if (split.Length == 1) return value;
            var property = split[0];
            var field = value.GetType().GetField(property,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField);
            if (field.FieldType.IsArray || typeof(IList).IsAssignableFrom(field.FieldType))
            {
                var indexStr = split[2];
                indexStr = indexStr.Replace("data[", "");
                indexStr = indexStr.Replace("]", "");
                var index = int.Parse(indexStr);
                var array = (IList) field.GetValue(value);
                return GetPropertyOwner(array[index], string.Join(".", split.Skip(3)));
            }

            return GetPropertyOwner(field.GetValue(value), string.Join(".", split.Skip(1)));
        }

        public static int GetPropertyIndexInArray(string path, int pathIndex)
        {
            var start = path.LastIndexOf('[');
            if (start != -1)
            {
                start += 1;
                var end = path.IndexOf(']', start);
                var value = path.Substring(start, end - start);
                pathIndex = int.Parse(value);
            }

            return pathIndex;
        }
    }
}
