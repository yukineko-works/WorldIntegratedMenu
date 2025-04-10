using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SystemSettingsModule : UdonSharpBehaviour
    {
        [SerializeField] private I18nManager _systemI18nManager;
        [SerializeField] private RadioButtonHelper _languageSelector;
        [SerializeField] private RadioButtonHelper _qmKeybindSelector;
        [SerializeField] private RadioButtonHelper _qmDominantHandSelector;
        [SerializeField] private ApplyI18n _qmKeybindNoticeText;
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
            if (_systemI18nManager == null || _languageSelector == null || _qmKeybindSelector == null || _qmDominantHandSelector == null || _quickMenuManager == null || _cloudSyncManager == null || _quickMenuSizeText == null)
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

            if (data.ContainsKey("qmdominanthand") && data.TryGetValue("qmdominanthand", out var dominantHand))
            {
                _qmDominantHandSelector.SetValue(dominantHand.String);
                UpdateQMDominantHand(dominantHand.String);
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
            _cloudSyncManager.Save("lang", language);
        }

        public void UpdateQMKeyBind()
        {
            if (_quickMenuManager == null) return;
            if (_qmKeybindSelector == null) return;
            UpdateQMKeyBind(_qmKeybindSelector.Value);
            _cloudSyncManager.Save("qmkeybind", _qmKeybindSelector.Value);
        }

        public void UpdateQMKeyBind(string value)
        {
            switch (value)
            {
                case "stick":
                    _quickMenuManager.SetOpenMethod(VRQuickMenuOpenMethod.Stick);
                    break;
                case "trigger":
                    _quickMenuManager.SetOpenMethod(VRQuickMenuOpenMethod.Trigger);
                    break;
                default:
                    _quickMenuManager.ResetOpenMethod();
                    break;
            }
        }

        public void UpdateQMDominantHand()
        {
            if (_quickMenuManager == null) return;
            if (_qmDominantHandSelector == null) return;
            UpdateQMDominantHand(_qmDominantHandSelector.Value);
            _cloudSyncManager.Save("qmdominanthand", _qmDominantHandSelector.Value);

            var value = _quickMenuManager.DominantHand == VRQuickMenuDominantHand.Left ? "left" : "right";
            var argValues = new string[2];
            argValues[0] = value;
            argValues[1] = value;
            _qmKeybindNoticeText.argValues = argValues;
            _qmKeybindNoticeText.Apply();
        }

        public void UpdateQMDominantHand(string value)
        {
            switch (value)
            {
                case "left":
                    _quickMenuManager.SetDominantHand(VRQuickMenuDominantHand.Left);
                    break;
                case "right":
                    _quickMenuManager.SetDominantHand(VRQuickMenuDominantHand.Right);
                    break;
                default:
                    _quickMenuManager.ResetDominantHand();
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
            if (!skipSave) _cloudSyncManager.Save("qmsize", _quickMenuSize);
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
    public class SystemSettingsModuleInspector : ModuleInspector
    {
        protected override string I18nUUID => "a1e9f9d4be7f50b4280969b169245980";
        protected override string[] ObjectProperties => new string[] {
            "_systemI18nManager",
            "_languageSelector",
            "_qmKeybindSelector",
            "_qmDominantHandSelector",
            "_qmKeybindNoticeText",
            "_quickMenuManager",
            "_cloudSyncManager",
            "_quickMenuSizeText",
            "_quickMenuPositionXText",
            "_quickMenuPositionYText",
            "_quickMenuPositionZText"
        };

        protected override void DrawModuleInspector()
        {
            EditorGUILayout.HelpBox(_i18n.GetTranslation("description"), MessageType.Warning);
        }
    }
#endif
}
