
using System;
using System.Text;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu
{
    [RequireComponent(typeof(CloudSyncUtils))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CloudSyncManager : UdonSharpBehaviour
    {
        [SerializeField] private string _apiBaseUrl = "https://vrc-api.yukineko.dev/wim";
        [SerializeField] private string _apiSchemaRev = "1";
        [SerializeField] private VRCUrl _apiLoadUrl = new VRCUrl("https://vrc-api.yukineko.dev/wim/load?rev=1");
        [SerializeField] private ThemeManager _themeManager;
        [SerializeField] private GameObject _syncStatus;
        [SerializeField] private Sprite _syncStatusUnknownIcon;
        [SerializeField] private Sprite _syncStatusSuccessIcon;
        [SerializeField] private Sprite _syncStatusErrorIcon;

        private Image _syncStatusImage;
        private ApplyTheme _syncStatusTheme;
        private CloudSyncUtils _cloudSyncUtils;
        private string _uid;
        private string _key;
        private DataDictionary _data = new DataDictionary();
        private DateTimeOffset _lastSaveTime = DateTimeOffset.MinValue;
        private bool _usingPersistenceData = false;
        private UdonSharpBehaviour[] _onLoadCallbackBehaviours = new UdonSharpBehaviour[0];
        private string[] _onLoadCallbackMethods = new string[0];
        private bool _initializedInternal = false;

        // State: unknown, success, error
        private string _lastState = "unknown";
        private DataDictionary _saveQueue = new DataDictionary();

        public bool Initialized => _initializedInternal && _data != null;
        public DataDictionary SyncData => _data;
        public DateTimeOffset LastSaveTime => _lastSaveTime;
        public string LastState => _lastState;
        public bool UsingPersistenceData => _usingPersistenceData;

        #region Internal Methods
        private void Start()
        {
            if (_syncStatus == null || _syncStatusUnknownIcon == null || _syncStatusSuccessIcon == null || _syncStatusErrorIcon == null)
            {
                Debug.LogError("[CloudSyncManager] Missing required components.");
                return;
            }

            _syncStatusImage = _syncStatus.GetComponent<Image>();
            _syncStatusTheme = _syncStatus.GetComponent<ApplyTheme>();
            _cloudSyncUtils = GetComponent<CloudSyncUtils>();
            _uid = _cloudSyncUtils.MD5Hash(Networking.LocalPlayer.displayName);
            _key = _cloudSyncUtils.MD5Hash($"key_{_uid}");
            _initializedInternal = true;
            RequestLoad();
        }

        public void OnLoad(UdonSharpBehaviour behaviour, string method)
        {
            Debug.Log($"[CloudSyncManager] Registering callback: {behaviour.name}.{method}");
            _onLoadCallbackBehaviours = ArrayUtils.Add(_onLoadCallbackBehaviours, behaviour);
            _onLoadCallbackMethods = ArrayUtils.Add(_onLoadCallbackMethods, method);

            if (Initialized)
            {
                behaviour.SendCustomEvent(method);
            }
        }

        public void RequestLoad()
        {
            if (!_initializedInternal) return;
            VRCStringDownloader.LoadUrl(_apiLoadUrl, (IUdonEventReceiver)this);
        }

        public void RequestSave(VRCUrl url)
        {
            if (!_initializedInternal) return;
            if (url.Get() != GetSaveUrl()) return;

            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }

        public string GetSaveUrl()
        {
            if (!_initializedInternal) return string.Empty;
            var savedata = GetSavedataJson(false);
            if (savedata == null) return string.Empty;
            var encodedSettings = Convert.ToBase64String(Encoding.UTF8.GetBytes(savedata)).Replace("/", "_").Replace("+", "-").Replace("=", "");
            var saveUrl = $"{_apiBaseUrl}/save?rev={_apiSchemaRev}&uid={_uid}&cfg={encodedSettings}";
            return saveUrl;
        }

        private string GetSavedataJson(bool withMetadata)
        {
            var json = withMetadata ? new DataDictionary() : _saveQueue;
            if (withMetadata)
            {
                json.SetValue("config", _saveQueue);
                json.SetValue("updatedAt", DateTimeOffset.Now.ToString("o"));
            }

            if (!VRCJson.TrySerializeToJson(json, JsonExportType.Minify, out var result)) return null;
            return result.String;
        }

        private void LoadSavedata(DataDictionary data, bool fromPersistence)
        {
            var savetime = data.TryGetValue("updatedAt", out var _timeTmp) ? DateTimeOffset.Parse(_timeTmp.String) : DateTimeOffset.MinValue;
            if (_lastSaveTime >= savetime)
            {
                Debug.Log($"[CloudSyncManager] Skipped loading config from {(fromPersistence ? "persistence" : "cloud")} on {savetime}");
                return;
            }

            _data = data.TryGetValue("config", out var _confTmp) ? _confTmp.DataDictionary : new DataDictionary();
            _lastSaveTime = savetime;
            _usingPersistenceData = fromPersistence;
            SetState("success", fromPersistence);

            for (int i = 0; i < _onLoadCallbackBehaviours.Length; i++)
            {
                if (_onLoadCallbackBehaviours[i] == null || string.IsNullOrEmpty(_onLoadCallbackMethods[i])) continue;
                _onLoadCallbackBehaviours[i].SendCustomEvent(_onLoadCallbackMethods[i]);
            }

            Debug.Log($"[CloudSyncManager] Config loaded from {(fromPersistence ? "persistence" : "cloud")} on {savetime}");
        }

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            if (PlayerData.TryGetString(player, "wim:cloudsync", out var savedata) && VRCJson.TryDeserializeFromJson(savedata, out var data))
            {
                LoadSavedata(data.DataDictionary, true);
            }
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            if (result.Url == _apiLoadUrl)
            {
                if (VRCJson.TryDeserializeFromJson(result.Result, out var _tmp) && _tmp.DataDictionary.TryGetValue(_key, out var data))
                {
                    LoadSavedata(data.DataDictionary, false);
                }
                return;
            }

            if (result.Url.Get().Contains("save"))
            {
                _saveQueue.Clear();
                Debug.Log("[CloudSyncManager] Config saved successfully.");
                RequestLoad();
                return;
            }
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.LogError($"[CloudSyncManager] Error loading string: {result.ErrorCode} - {result.Error}");

            if (result.Url == _apiLoadUrl && _lastSaveTime == DateTimeOffset.MinValue)
            {
                SetState("error");
            }
        }

        private void SetState(string state, bool persistence = false)
        {
            switch (state)
            {
                case "success":
                    _lastState = "success";
                    _syncStatusImage.sprite = _lastSaveTime == DateTimeOffset.MinValue ? _syncStatusUnknownIcon : _syncStatusSuccessIcon;
                    _syncStatusTheme.colorPalette = persistence ? ColorPalette.Warning : ColorPalette.Success;
                    break;
                case "error":
                    _lastState = "error";
                    _syncStatusImage.sprite = _syncStatusErrorIcon;
                    _syncStatusTheme.colorPalette = ColorPalette.Error;
                    break;
                default:
                    _lastState = "unknown";
                    _syncStatusImage.sprite = _syncStatusUnknownIcon;
                    _syncStatusTheme.colorPalette = ColorPalette.Text;
                    break;
            }

            _syncStatusTheme.Apply(_themeManager.GetColor(_syncStatusTheme.colorPalette));
        }
        #endregion

        #region Public
        public void Save(string key, DataToken value)
        {
            // Queueにkeyが存在しておらず、なおかつ読み込まれている値と保存しようとしている値が同じ場合は保存しない (初回ロード時の不必要なデータ保存対策)
            if (!_saveQueue.ContainsKey(key) && _data.TryGetValue(key, out var _tmp) && _tmp.Equals(value)) return;
            _saveQueue.SetValue(key, value);

            var savedata = GetSavedataJson(true);
            if (savedata == null) return;
            PlayerData.SetString("wim:cloudsync", savedata);
            _lastSaveTime = DateTimeOffset.Now;
            _usingPersistenceData = true;
            SetState("success", true);
        }
        #endregion

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [CustomEditor(typeof(CloudSyncManager))]
        internal class CloudSyncManagerInspector : Editor
        {
            private bool _apiUrlPreview = false;
            private bool _showInternalProperties = false;

            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Cloud Sync Manager", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                var apiBaseUrl = serializedObject.FindProperty("_apiBaseUrl");
                var apiSchemaRev = serializedObject.FindProperty("_apiSchemaRev");
                EditorGUILayout.PropertyField(apiBaseUrl);
                EditorGUILayout.PropertyField(apiSchemaRev);
                EditorGUILayout.Space();

                var apiLoadUrl = $"{apiBaseUrl.stringValue}/load?rev={apiSchemaRev.stringValue}";

                _apiUrlPreview = EditorGUILayout.Foldout(_apiUrlPreview, "API URL Preview");
                if (_apiUrlPreview)
                {
                    var style = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = true
                    };

                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("API Load URL", apiLoadUrl, style);
                    EditorGUILayout.LabelField("API Save URL", $"{apiBaseUrl.stringValue}/save?rev={apiSchemaRev.stringValue}&uid=<UsernameHash>&cfg=<Base64EncodedSettings>", style);
                    EditorGUI.indentLevel--;
                }

                _showInternalProperties = EditorGUILayout.Foldout(_showInternalProperties, EditorI18n.GetTranslation("internalProperties"));
                if (_showInternalProperties)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_themeManager"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_syncStatus"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_syncStatusUnknownIcon"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_syncStatusSuccessIcon"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_syncStatusErrorIcon"));
                    EditorGUI.indentLevel--;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();

                    ((CloudSyncManager)target)._apiLoadUrl = new VRCUrl(apiLoadUrl);
                    EditorUtility.SetDirty(target);
                }
            }
        }
#endif
    }
}
