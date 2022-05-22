using OpenUGD.AsyncBundles.Presets;
using UnityEditor;

namespace OpenUGD.AsyncBundles
{
    public class AssetPresetEditorWindow : EditorWindow
    {
        [MenuItem(AssetsPresetUtils.MenuItem + "/Preset", priority = -1003)]
        public static void Open()
        {
            var preset = AssetsPresetUtils.Get();
            if (preset != null)
            {
                _preset = preset;
                var window = GetWindow<AssetPresetEditorWindow>("AssetsPreset");
                window.Show(true);
            }
        }

        private static AssetsPreset _preset;
        private Editor _editor;

        void OnEnable()
        {
            if (_preset != null)
            {
                _editor = Editor.CreateEditor(_preset);
            }
        }

        void OnGUI()
        {
            if (_editor != null)
            {
                _editor.OnInspectorGUI();
            }
        }

        void OnDisable()
        {
            if (_editor != null)
            {
                DestroyImmediate(_editor);
            }
        }
    }
}
