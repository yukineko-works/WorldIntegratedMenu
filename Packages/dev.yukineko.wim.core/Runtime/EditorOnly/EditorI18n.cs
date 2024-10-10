using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;

namespace yukineko.WorldIntegratedMenu
{
    public class InternalEditorI18n
    {
        public InternalEditorI18n(string i18nJsonGuid)
        {
#if UNITY_EDITOR
            var i18nJson = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(i18nJsonGuid));
            var data = i18nJson != null && VRCJson.TryDeserializeFromJson(i18nJson.text, out var i) ? i : new DataToken();
            _i18n = data.TokenType == TokenType.DataDictionary ? data.DataDictionary : new DataDictionary();
#else
            _i18n = new DataDictionary();
#endif
        }

        private DataDictionary _i18n;
        public DataDictionary I18n => _i18n;

        private static readonly string _fallbackLanguage = "en";

        public static readonly Dictionary<string, string> availableLanguages = new Dictionary<string, string>() {
            { "en", "English" },
            { "ja", "日本語" },
            { "zh-CN", "简体中文" },
            { "zh-TW", "繁體中文" },
            { "ko", "한국어" },
        };

        private static string _currentLanguage;

        public static string CurrentLanguage
        {
            get
            {
                if (!string.IsNullOrEmpty(_currentLanguage)) return _currentLanguage;
#if UNITY_EDITOR
                var lang = EditorPrefs.GetString("ynworks_language");
#else
                var lang = "en";
#endif
                if (!string.IsNullOrEmpty(lang) && availableLanguages.ContainsKey(lang)) return lang;

                var currentCulture = CultureInfo.CurrentCulture;
                if (availableLanguages.ContainsKey(currentCulture.Name))
                {
                    _currentLanguage = currentCulture.Name;
                    return _currentLanguage;
                }

                if (availableLanguages.ContainsKey(currentCulture.TwoLetterISOLanguageName))
                {
                    _currentLanguage = currentCulture.TwoLetterISOLanguageName;
                    return _currentLanguage;
                }

                _currentLanguage = _fallbackLanguage;
                return _currentLanguage;
            }
            set
            {
                if (availableLanguages.ContainsKey(value))
                {
                    _currentLanguage = value;
#if UNITY_EDITOR
                    EditorPrefs.SetString("ynworks_language", value);
#endif
                }
            }
        }

        public string GetTranslation(string key, string language = null)
        {
            var lang = language ?? CurrentLanguage;
            if (I18n.TryGetValue(lang, out var translation) && translation.DataDictionary.TryGetValue(key, out var value)) return value.String;
            if (lang == _fallbackLanguage) return "[Translation Not Found] " + key;
            return GetTranslation(key, _fallbackLanguage);
        }

        public GUIContent GetGUITranslation(string key, string language = null) => new GUIContent(GetTranslation(key, language));
    }

    public class EditorI18n
    {
        private static InternalEditorI18n _instance = new InternalEditorI18n("1951d403fb3f455469e6264bc069e3f4");
        public static InternalEditorI18n InternalEditorI18n => _instance;

        public static string GetTranslation(string key, string language = null) => _instance.GetTranslation(key, language);
        public static GUIContent GetGUITranslation(string key, string language = null) => _instance.GetGUITranslation(key, language);
    }
}
