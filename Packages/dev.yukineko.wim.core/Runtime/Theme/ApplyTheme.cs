using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UdonSharp;
using UnityEditor;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu
{
    [DisallowMultipleComponent]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ApplyTheme : UdonSharpBehaviour
    {
        public ColorPalette colorPalette;
        public ThemeManager themeManager;
        public float alpha = 1.0f;

        public void Apply()
        {
            if (themeManager == null) return;
            var color = themeManager.GetColor(colorPalette, alpha);
            var img = GetComponent<Image>();
            if (img != null) img.color = color;
            var text = GetComponent<Text>();
            if (text != null) text.color = color;
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(ApplyTheme))]
    internal class ApplyThemeInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Apply Theme", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("colorPalette"), EditorI18n.GetGUITranslation("colorPalette"));
            var alpha = serializedObject.FindProperty("alpha");
            alpha.floatValue = EditorGUILayout.Slider(EditorI18n.GetTranslation("alpha"), alpha.floatValue, 0.0f, 1.0f);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}
