using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UdonSharp;
using UnityEditor;
using VRC.SDK3.Data;
using System.Linq;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ThemeManager : UdonSharpBehaviour
    {
        [SerializeField] private TextAsset _themePreset;
        [SerializeField] private UIManager _controller;
        [SerializeField] private Color _accentColor = new Color(137f / 255f, 180f / 255f, 250f / 255f, 1.0f);
        [SerializeField] private Color _baseColor = new Color(30f / 255f, 30f / 255f, 46f / 255f, 1.0f);
        [SerializeField] private Color _surfaceColor = new Color(49f / 255f, 50f / 255f, 68f / 255f, 1.0f);
        [SerializeField] private Color _textColor = new Color(205f / 255f, 214f / 255f, 244f / 255f, 1.0f);
        [SerializeField] private Color _successColor = new Color(166f / 255f, 227f / 255f, 161f / 255f, 1.0f);
        [SerializeField] private Color _warningColor = new Color(250f / 255f, 179f / 255f, 135f / 255f, 1.0f);
        [SerializeField] private Color _errorColor = new Color(243f / 255f, 139f / 255f, 168f / 255f, 1.0f);
        [SerializeField] private Color _infoColor = new Color(137f / 255f, 220f / 255f, 235f / 255f, 1.0f);

        // private DataDictionary _themes;

        // void Start()
        // {
        // if (themePreset == null) return;
        // VRCJson.TryDeserializeFromJson(themePreset.text, out var _th);
        // if (_th.TokenType != TokenType.DataDictionary) return;
        // _themes = _th.DataDictionary;
        // ApplyTheme();
        // }

        public Color GetColor(ColorPalette colorPalette, float alpha = 1.0f)
        {
            switch (colorPalette)
            {
                case ColorPalette.Accent:
                    return new Color(_accentColor.r, _accentColor.g, _accentColor.b, alpha);
                case ColorPalette.Base:
                    return new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
                case ColorPalette.Surface:
                    return new Color(_surfaceColor.r, _surfaceColor.g, _surfaceColor.b, alpha);
                case ColorPalette.Text:
                    return new Color(_textColor.r, _textColor.g, _textColor.b, alpha);
                case ColorPalette.Success:
                    return new Color(_successColor.r, _successColor.g, _successColor.b, alpha);
                case ColorPalette.Warning:
                    return new Color(_warningColor.r, _warningColor.g, _warningColor.b, alpha);
                case ColorPalette.Error:
                    return new Color(_errorColor.r, _errorColor.g, _errorColor.b, alpha);
                case ColorPalette.Info:
                    return new Color(_infoColor.r, _infoColor.g, _infoColor.b, alpha);
                default:
                    return new Color(0, 0, 0, 1.0f);
            }
        }

        public void ApplyTheme()
        {
            foreach (var canvas in _controller.Canvas)
            {
                foreach (var component in canvas.GetComponentsInChildren<ApplyTheme>(true))
                {
                    component.Apply(GetColor(component.colorPalette, component.alpha));
                }
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [CustomEditor(typeof(ThemeManager))]
        internal class RadioButtonHelperInspector : Editor
        {
            private ThemeManager _themeManager;
            private DataList _themes;
            private int _selectedThemeIndex = 0;

            private void OnEnable()
            {
                _themeManager = target as ThemeManager;
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                if (_themes == null && _themeManager._themePreset != null)
                {
                    VRCJson.TryDeserializeFromJson(_themeManager._themePreset.text, out var _th);
                    if (_th.TokenType == TokenType.DataList)
                    {
                        _themes = _th.DataList;
                    }
                }
                if (_themes != null)
                {
                    _selectedThemeIndex = EditorGUILayout.Popup(_selectedThemeIndex, _themes.Select(t =>
                    {
                        var res = t.DataDictionary.TryGetValue("$name", out var name);
                        return res ? name.String : "Unknown";
                    }).ToArray());

                    if (GUILayout.Button("Use this theme"))
                    {
                        var theme = _themes[_selectedThemeIndex];
                        if (theme.TokenType != TokenType.DataDictionary) return;

                        serializedObject.FindProperty("_accentColor").colorValue = theme.DataDictionary.TryGetValue("accent", out var hexAccent) && ColorUtility.TryParseHtmlString(hexAccent.String, out var accentColor) ? accentColor : _themeManager._accentColor;
                        serializedObject.FindProperty("_baseColor").colorValue = theme.DataDictionary.TryGetValue("base", out var hexBase) && ColorUtility.TryParseHtmlString(hexBase.String, out var baseColor) ? baseColor : _themeManager._baseColor;
                        serializedObject.FindProperty("_surfaceColor").colorValue = theme.DataDictionary.TryGetValue("surface", out var hexSurface) && ColorUtility.TryParseHtmlString(hexSurface.String, out var surfaceColor) ? surfaceColor : _themeManager._surfaceColor;
                        serializedObject.FindProperty("_textColor").colorValue = theme.DataDictionary.TryGetValue("text", out var hexText) && ColorUtility.TryParseHtmlString(hexText.String, out var textColor) ? textColor : _themeManager._textColor;
                        serializedObject.FindProperty("_successColor").colorValue = theme.DataDictionary.TryGetValue("success", out var hexSuccess) && ColorUtility.TryParseHtmlString(hexSuccess.String, out var successColor) ? successColor : _themeManager._successColor;
                        serializedObject.FindProperty("_warningColor").colorValue = theme.DataDictionary.TryGetValue("warning", out var hexWarning) && ColorUtility.TryParseHtmlString(hexWarning.String, out var warningColor) ? warningColor : _themeManager._warningColor;
                        serializedObject.FindProperty("_errorColor").colorValue = theme.DataDictionary.TryGetValue("error", out var hexError) && ColorUtility.TryParseHtmlString(hexError.String, out var errorColor) ? errorColor : _themeManager._errorColor;
                        serializedObject.FindProperty("_infoColor").colorValue = theme.DataDictionary.TryGetValue("info", out var hexInfo) && ColorUtility.TryParseHtmlString(hexInfo.String, out var infoColor) ? infoColor : _themeManager._infoColor;

                        serializedObject.ApplyModifiedProperties();
                    }
                }

                EditorGUILayout.Space();
                base.OnInspectorGUI();
                EditorGUILayout.Space();
                if (GUILayout.Button("Apply Theme"))
                {
                    _themeManager.ApplyTheme();
                }
            }
        }
#endif
    }
}
