using OpenUGD.AsyncBundles.Presets;
using UnityEditor;
using UnityEngine;

namespace OpenUGD.AsyncBundles
{
    [CustomPropertyDrawer(typeof(AssetInfo))]
    public class AssetInfoPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AssetEditorUtils.PushIndentLevel(EditorGUI.indentLevel + 0);
            AssetEditorUtils.PushColor();
            GUI.color = new Color(.7f, .7f, .7f);
            EditorGUI.BeginProperty(position, label, property);

            AssetEditorUtils.RectTop(ref position, 2);
            AssetEditorUtils.RectLeft(ref position, EditorGUI.indentLevel * 16);
            GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

            var guidProperty = property.FindPropertyRelative(nameof(AssetInfo.Guid));
            var nameProperty = property.FindPropertyRelative(nameof(AssetInfo.Name));
            var tagsProperty = property.FindPropertyRelative(nameof(AssetInfo.Tags));

            EditorGUI.BeginChangeCheck();

            var namePosition = AssetEditorUtils.RectTop(ref position, 16);
            EditorGUI.PropertyField(namePosition, nameProperty);

            AssetEditorUtils.RectTop(ref position, 2);
            var objectPosition = AssetEditorUtils.RectTop(ref position, 16);
            var op = objectPosition;
            AssetEditorUtils.RectLeft(ref op, 2);
            var depPosition = AssetEditorUtils.RectLeft(ref op, 26);

            var path = AssetDatabase.GUIDToAssetPath(guidProperty.stringValue);
            var obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

            if (GUI.Button(depPosition, new GUIContent("D") {tooltip = "Dependency"}, EditorStyles.miniButton))
            {
                AssetDependencyEditorWindow.Open(new Object[] {obj});
            }

            EditorGUI.ObjectField(objectPosition, obj, typeof(UnityEngine.Object), false);

            AssetEditorUtils.RectTop(ref position, 4);
            var tagPosition = AssetEditorUtils.RectTop(ref position, 16);
            EditorGUI.PropertyField(tagPosition, tagsProperty, true);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();

            AssetEditorUtils.PopColor();
            AssetEditorUtils.PopIndentLevel();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var tagsProperty = property.FindPropertyRelative(nameof(AssetInfo.Tags));
            return 44f + EditorGUI.GetPropertyHeight(tagsProperty, true);
        }
    }
}
