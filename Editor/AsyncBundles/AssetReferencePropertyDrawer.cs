using System;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [CustomPropertyDrawer(typeof(AssetReference), true)]
    public class AssetReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var guidProperty = property.FindPropertyRelative(AssetReference.Internal.GuidProperty);
            var guid = guidProperty.stringValue;
            var owner = AssetEditorUtils.GetPropertyOwner(guidProperty);
            Type type = typeof(UnityEngine.Object);
            if (owner != null)
            {
                var currentType = owner.GetType();
                while (currentType != null)
                {
                    if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(AssetReference<>))
                    {
                        type = currentType.GetGenericArguments()[0];
                        break;
                    }

                    currentType = currentType.BaseType;
                }
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var value = AssetDatabase.LoadAssetAtPath(path, type);
            var newValue = EditorGUI.ObjectField(position, label, value, type, false);
            if (value != newValue)
            {
                if (newValue == null)
                {
                    guidProperty.stringValue = string.Empty;
                }
                else
                {
                    var newPath = AssetDatabase.GetAssetPath(newValue);
                    guidProperty.stringValue = AssetDatabase.AssetPathToGUID(newPath);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
