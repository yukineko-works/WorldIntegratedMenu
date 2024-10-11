
using System;
using System.Globalization;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using yukineko.WorldIntegratedMenu.EditorShared;

// RFC 5646 Language Tags
// https://gist.github.com/msikma/8912e62ed866778ff8cd

namespace yukineko.WorldIntegratedMenu
{
    public enum DynamicArgs
    {
        VRCLocalPlayerName,
    }

    [DisallowMultipleComponent]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class I18nManager : UdonSharpBehaviour
    {
        [SerializeField] private UIManager _controller;
        [SerializeField] private TextAsset _localizationJson;
        private readonly string _fallbackLanguage = "en";
        private bool _isInitialized = false;
        private DataDictionary _localization;
        private string _currentLanguage;

        public bool Initialized => _isInitialized;
        public bool HasLocalization => _localization != null;
        public string CurrentLanguage => _currentLanguage;

        public CultureInfo CurrentCulture
        {
            get
            {
                switch (TimeZoneInfo.Local.Id)
                {
                    case "Asia/Tokyo":
                    case "Tokyo Standard Time":
                        return CultureInfo.GetCultureInfo("ja-JP");

                    case "Asia/Shanghai":
                    case "China Standard Time":
                        return CultureInfo.GetCultureInfo("zh-CN");

                    case "Asia/Taipei":
                    case "Taipei Standard Time":
                        return CultureInfo.GetCultureInfo("zh-TW");

                    case "Asia/Seoul":
                    case "Korea Standard Time":
                        return CultureInfo.GetCultureInfo("ko-KR");

                    default:
                        return CultureInfo.InvariantCulture;
                }
            }

            private set { }
        }

        private void Start()
        {
            BuildLocalization();
        }

        public void BuildLocalization()
        {
            if (_isInitialized) return;
            if (_localizationJson != null)
            {
                VRCJson.TryDeserializeFromJson(_localizationJson.text, out var _loc);
                if (_loc.TokenType == TokenType.DataDictionary) _localization = _loc.DataDictionary;
            }

            _currentLanguage = GetSystemLanguage();
            _isInitialized = true;
            ApplyI18n();
        }

        public string GetTranslation(string key, string language = null)
        {
            var lang = language ?? _currentLanguage;
            if (_localization == null) return string.Empty;
            if (!_localization.TryGetValue(lang, out var translation) || !translation.DataDictionary.TryGetValue(key, out var value))
            {
                if (lang == _fallbackLanguage) return string.Empty;
                return GetTranslation(key, _fallbackLanguage);
            }

            return value.String;
        }

        public string GetTranslationWithArgs(string key, DynamicArgs[] args, string language = null)
        {
            var translation = GetTranslation(key, language);
            for (int i = 0; i < args.Length; i++)
            {
                translation = translation.Replace($"{{{i}}}", GetDynamicArg(args[i]));
            }

            return translation;
        }

        public string GetDynamicArg(DynamicArgs arg)
        {
            switch (arg)
            {
                case DynamicArgs.VRCLocalPlayerName:
                    return Networking.LocalPlayer.displayName;
                default:
                    return string.Empty;
            }
        }

        public void SetLanguage(string language = null)
        {
            _currentLanguage = language ?? GetSystemLanguage();
            Debug.Log($"[I18nManager] SetLanguage: {_currentLanguage}");
            ApplyI18n();
        }

        public void ApplyI18n()
        {
            if (_controller == null) return;
            _controller.UpdateTitle();
            foreach (var canvas in _controller.Canvas)
            {
                foreach (var component in canvas.GetComponentsInChildren<ApplyI18n>(true))
                {
                    component.Apply(_currentLanguage);
                }
            }
        }

        public string GetSystemLanguage()
        {
            // VRC内だとInvariantCultureになるので使えない
            // var currentCulture = CultureInfo.CurrentCulture;
            // var languageCodeWithRegion = currentCulture.ToString(); // e.g. "en-US"
            // var languageCode = currentCulture.TwoLetterISOLanguageName; // e.g. "en"

            // if (_localization.ContainsKey(languageCodeWithRegion))
            // {
            //     return languageCodeWithRegion;
            // }

            // if (_localization.ContainsKey(languageCode))
            // {
            //     return languageCode;
            // }

            switch (TimeZoneInfo.Local.Id)
            {
                case "Asia/Tokyo":
                case "Tokyo Standard Time":
                    return "ja";

                case "Asia/Shanghai":
                case "China Standard Time":
                    return "zh-CN";

                case "Asia/Taipei":
                case "Taipei Standard Time":
                    return "zh-TW";

                case "Asia/Seoul":
                case "Korea Standard Time":
                    return "ko";

                default:
                    return _fallbackLanguage;
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [CustomEditor(typeof(I18nManager))]
        internal class I18nManagerInspector : Editor
        {
            private I18nManager _i18nManager;
            private bool _hasModuleMetadata = false;

            private void OnEnable()
            {
                _i18nManager = target as I18nManager;
                _hasModuleMetadata = _i18nManager.gameObject.GetComponent<ModuleMetadata>() != null;
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                EditorGUILayout.LabelField("I18n Manager", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                if (!_hasModuleMetadata && _i18nManager._controller == null)
                {
                    EditorGUILayout.HelpBox(EditorI18n.GetTranslation("controllerMissing"), MessageType.Warning);
                }

                if (_i18nManager._localizationJson == null)
                {
                    EditorGUILayout.HelpBox(EditorI18n.GetTranslation("localizeFileMissing"), MessageType.Warning);
                }

                EditorGUI.BeginChangeCheck();

                if (!_hasModuleMetadata)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_controller"), new GUIContent(EditorI18n.GetTranslation("controller")));
                }
                else if (_i18nManager._controller != null)
                {
                    EditorGUILayout.HelpBox(EditorI18n.GetTranslation("i18nManagerControllerWarning"), MessageType.Warning);
                    if (GUILayout.Button(EditorI18n.GetTranslation("removeController")))
                    {
                        _i18nManager._controller = null;
                    }
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("_localizationJson"), new GUIContent(EditorI18n.GetTranslation("localizeFile")));

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
#endif
    }
}
