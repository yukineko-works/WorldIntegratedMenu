﻿
using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SystemSettingsModule : UdonSharpBehaviour
    {
        [SerializeField] private I18nManager _systemI18nManager;
        [SerializeField] private RadioButtonHelper _languageSelector;
        [SerializeField] private RadioButtonHelper _qmKeybindSelector;
        [SerializeField] private QuickMenuManager _quickMenuManager;
        [SerializeField] private CloudSyncManager _cloudSyncManager;
        [SerializeField] private Text _quickMenuSizeText;
        [SerializeField] private Text _quickMenuPositionXText;
        [SerializeField] private Text _quickMenuPositionYText;
        [SerializeField] private Text _quickMenuPositionZText;

        private float _quickMenuSize = 1f;
        private float _vrScreenPositionX = 0.0f;
        private float _vrScreenPositionY = -0.15f;
        private float _vrScreenPositionZ = 0.15f;

        private void Start()
        {
            if (_systemI18nManager == null || _languageSelector == null || _qmKeybindSelector == null || _quickMenuManager == null || _cloudSyncManager == null || _quickMenuSizeText == null)
            {
                Debug.LogError("SystemSettingsModule: Missing required components.");
                return;
            }

            ChangeQuickMenuSize(true);
            // ChangeQuickMenuPosition();

            _cloudSyncManager.OnLoad(this, nameof(OnCloudSyncLoaded));
        }

        public void OnCloudSyncLoaded()
        {
            var data = _cloudSyncManager.SyncData;
            if (data == null) return;

            if (data.ContainsKey("qmsize") && data.TryGetValue("qmsize", out var size))
            {
                switch (size.TokenType)
                {
                    case TokenType.Double:
                        _quickMenuSize = (float)size.Double;
                        break;
                    case TokenType.Float:
                        _quickMenuSize = size.Float;
                        break;
                    case TokenType.Int:
                        _quickMenuSize = size.Int;
                        break;
                }
                ChangeQuickMenuSize(true);
            }

            if (data.ContainsKey("qmkeybind") && data.TryGetValue("qmkeybind", out var keybind))
            {
                _qmKeybindSelector.SetValue(keybind.String);
                UpdateQMKeyBind(keybind.String);
            }

            if (data.ContainsKey("lang") && data.TryGetValue("lang", out var lang))
            {
                _languageSelector.SetValue(lang.String);
                _systemI18nManager.SetLanguage(lang.String);
            }
        }

        public void UpdateLanguage()
        {
            if (_systemI18nManager == null) return;
            if (_languageSelector == null) return;

            var language = _languageSelector.Value;
            _systemI18nManager.SetLanguage(language);
            _cloudSyncManager.SaveQueue.SetValue("lang", language);
        }

        public void UpdateQMKeyBind()
        {
            if (_quickMenuManager == null) return;
            if (_qmKeybindSelector == null) return;
            UpdateQMKeyBind(_qmKeybindSelector.Value);
            _cloudSyncManager.SaveQueue.SetValue("qmkeybind", _qmKeybindSelector.Value);
        }

        public void UpdateQMKeyBind(string value)
        {
            switch (value)
            {
                case "default":
                    _quickMenuManager.ResetOpenMethod();
                    break;
                case "stick":
                    _quickMenuManager.SetOpenMethod(VRQuickMenuOpenMethod.Stick);
                    break;
                case "trigger":
                    _quickMenuManager.SetOpenMethod(VRQuickMenuOpenMethod.Trigger);
                    break;
            }
        }

        public void QMSizeUp()
        {
            _quickMenuSize = Mathf.Clamp(_quickMenuSize + 0.1f, 0.5f, 1.5f);
            ChangeQuickMenuSize();
        }

        public void QMSizeDown()
        {
            _quickMenuSize = Mathf.Clamp(_quickMenuSize - 0.1f, 0.5f, 1.5f);
            ChangeQuickMenuSize();
        }

        public void QMPositionXUp()
        {
            _vrScreenPositionX += 0.01f;
            ChangeQuickMenuPosition();
        }

        public void QMPositionXDown()
        {
            _vrScreenPositionX -= 0.01f;
            ChangeQuickMenuPosition();
        }

        public void QMPositionYUp()
        {
            _vrScreenPositionY += 0.01f;
            ChangeQuickMenuPosition();
        }

        public void QMPositionYDown()
        {
            _vrScreenPositionY -= 0.01f;
            ChangeQuickMenuPosition();
        }

        public void QMPositionZUp()
        {
            _vrScreenPositionZ += 0.01f;
            ChangeQuickMenuPosition();
        }

        public void QMPositionZDown()
        {
            _vrScreenPositionZ -= 0.01f;
            ChangeQuickMenuPosition();
        }

        public void ChangeQuickMenuSize(bool skipSave = false)
        {
            if (_quickMenuManager == null) return;
            if (_quickMenuSizeText == null) return;

            _quickMenuManager.SetMenuSize(_quickMenuSize);
            _quickMenuSizeText.text = _quickMenuSize.ToString("P0");
            if (!skipSave) _cloudSyncManager.SaveQueue.SetValue("qmsize", _quickMenuSize);
        }

        public void ChangeQuickMenuPosition()
        {
            if (_quickMenuManager == null) return;
            if (_quickMenuPositionXText == null || _quickMenuPositionYText == null || _quickMenuPositionZText == null) return;

            _quickMenuManager.SetScreenPosition(new Vector3(_vrScreenPositionX, _vrScreenPositionY, _vrScreenPositionZ));
            _quickMenuPositionXText.text = _vrScreenPositionX.ToString("N2");
            _quickMenuPositionYText.text = _vrScreenPositionY.ToString("N2");
            _quickMenuPositionZText.text = _vrScreenPositionZ.ToString("N2");
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(SystemSettingsModule))]
    public class SystemSettingsModuleInspector : Editor
    {
        private InternalEditorI18n _i18n;
        private bool _showObjectProperties = false;

        private void OnEnable()
        {
            _i18n = new InternalEditorI18n("a1e9f9d4be7f50b4280969b169245980");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_i18n.GetTranslation("$title"), EditorStyles.largeLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(_i18n.GetTranslation("description"), MessageType.Warning);
            EditorGUILayout.Space();

            _showObjectProperties = EditorGUILayout.Foldout(_showObjectProperties, EditorI18n.GetTranslation("internalProperties"));
            if (_showObjectProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_systemI18nManager"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_languageSelector"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_qmKeybindSelector"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_quickMenuManager"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_cloudSyncManager"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_quickMenuSizeText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_quickMenuPositionXText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_quickMenuPositionYText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_quickMenuPositionZText"));
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}
