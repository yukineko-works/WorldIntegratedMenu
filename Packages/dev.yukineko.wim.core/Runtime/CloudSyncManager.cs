
using System;
using System.Text;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace yukineko.WorldIntegratedMenu
{
    [RequireComponent(typeof(CloudSyncUtils))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CloudSyncManager : UdonSharpBehaviour
    {
        [SerializeField] private string _apiBaseUrl = "https://vrc-api.yukineko.dev/wim";
        [SerializeField] private string _apiSchemaRev = "1";
        [SerializeField] private VRCUrl _apiLoadUrl = new VRCUrl("https://vrc-api.yukineko.dev/wim/load?rev=1");
        [SerializeField] private Animator _topUiAnimator;

        private CloudSyncUtils _cloudSyncUtils;
        private string _uid;
        private string _key;
        private DataDictionary _data;
        private DateTimeOffset _lastSaveTime;
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
        public DataDictionary SaveQueue => _saveQueue;

        private void Start()
        {
            if (_topUiAnimator == null)
            {
                Debug.LogError("[CloudSyncManager] Missing required components.");
                return;
            }

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
            if (!VRCJson.TrySerializeToJson(SaveQueue, JsonExportType.Minify, out var json)) return string.Empty;
            var encodedSettings = Convert.ToBase64String(Encoding.UTF8.GetBytes(json.String)).Replace("/", "_").Replace("+", "-").Replace("=", "");
            var saveUrl = $"{_apiBaseUrl}/save?rev={_apiSchemaRev}&uid={_uid}&cfg={encodedSettings}";
            return saveUrl;
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            if (result.Url == _apiLoadUrl)
            {
                if (VRCJson.TryDeserializeFromJson(result.Result, out var _tmp) && _tmp.DataDictionary.TryGetValue(_key, out var data))
                {
                    _data = data.DataDictionary.TryGetValue("config", out var _confTmp) ? _confTmp.DataDictionary : new DataDictionary();
                    _lastSaveTime = data.DataDictionary.TryGetValue("updatedAt", out var _timeTmp) ? DateTimeOffset.Parse(_timeTmp.String) : DateTimeOffset.MinValue;
                }
                else
                {
                    _data = new DataDictionary();
                    _lastSaveTime = DateTimeOffset.MinValue;
                }

                SetState("success");
                for (int i = 0; i < _onLoadCallbackBehaviours.Length; i++)
                {
                    if (_onLoadCallbackBehaviours[i] == null || string.IsNullOrEmpty(_onLoadCallbackMethods[i])) continue;
                    _onLoadCallbackBehaviours[i].SendCustomEvent(_onLoadCallbackMethods[i]);
                }

                Debug.Log("[CloudSyncManager] Config synced successfully.");
                return;
            }

            if (result.Url.Get().Contains("save"))
            {
                SaveQueue.Clear();
                Debug.Log("[CloudSyncManager] Config saved successfully.");

                RequestLoad();
                return;
            }
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.LogError($"[CloudSyncManager] Error loading string: {result.ErrorCode} - {result.Error}");

            if (result.Url == _apiLoadUrl)
            {
                SetState("error");
                for (int i = 0; i < _onLoadCallbackBehaviours.Length; i++)
                {
                    _onLoadCallbackBehaviours[i].SendCustomEvent(_onLoadCallbackMethods[i]);
                }
            }
        }

        private void SetState(string state)
        {
            switch (state)
            {
                case "success":
                    _lastState = "success";
                    _topUiAnimator.SetTrigger(_lastSaveTime == DateTimeOffset.MinValue ? "unknown" : "success");
                    break;
                case "error":
                    _lastState = "error";
                    _topUiAnimator.SetTrigger("error");
                    break;
                default:
                    _lastState = "unknown";
                    _topUiAnimator.SetTrigger("unknown");
                    break;
            }
        }

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
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_topUiAnimator"));
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
