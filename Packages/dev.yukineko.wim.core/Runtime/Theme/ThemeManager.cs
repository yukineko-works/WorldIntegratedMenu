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

        public string ThemePreset => _themePreset.text;

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
    }
}
