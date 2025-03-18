using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CloudSyncModule : UdonSharpBehaviour
    {
        [SerializeField] private CloudSyncManager _cloudSyncManager;
        [SerializeField] private Animator _saveStatusAnimator;
        [SerializeField] private ApplyTimeI18n _lastSaveTime;
        [SerializeField] private InputField _saveUrlCopyField;
        [SerializeField] private VRCUrlInputField _saveUrlPasteField;
        private VRCUrl _emptyUrl = new VRCUrl("");

        private void Start()
        {
            if (_cloudSyncManager == null || _saveStatusAnimator == null || _lastSaveTime == null || _saveUrlCopyField == null || _saveUrlPasteField == null)
            {
                Debug.LogError("CloudSyncModule: Missing required components.");
                return;
            }

            _saveStatusAnimator.keepAnimatorStateOnDisable = true;
        }

        public void RefreshStatus()
        {
            Debug.Log("CloudSyncModule: Refreshing status with state " + _cloudSyncManager.LastState);
            switch (_cloudSyncManager.LastState)
            {
                case "unknown":
                    _saveStatusAnimator.SetTrigger("loading");
                    break;
                case "success":
                    if (_cloudSyncManager.LastSaveTime == DateTimeOffset.MinValue)
                    {
                        _saveStatusAnimator.SetTrigger("notfound");
                    }
                    else
                    {
                        _lastSaveTime.time = _cloudSyncManager.LastSaveTime.ToLocalTime();
                        _lastSaveTime.Apply();
                        _saveStatusAnimator.SetBool("fromPersistence", _cloudSyncManager.UsingPersistenceData);
                        _saveStatusAnimator.SetTrigger("success");
                    }
                    break;
                case "error":
                    _saveStatusAnimator.SetTrigger("error");
                    break;
            }
        }

        public void OnModuleCalled()
        {
            _saveUrlCopyField.text = _cloudSyncManager.GetSaveUrl();
            RefreshStatus();
        }

        public void OnSaveRequested()
        {
            _cloudSyncManager.RequestSave(_saveUrlPasteField.GetUrl());
            _saveUrlPasteField.SetUrl(_emptyUrl);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(CloudSyncModule))]
    public class CloudSyncModuleInspector : ModuleInspector
    {
        protected override string I18nUUID => "924493d0692e091469e86bb170d34d8e";
        protected override string[] ObjectProperties => new string[] { "_cloudSyncManager", "_saveStatusAnimator", "_lastSaveTime", "_saveUrlCopyField", "_saveUrlPasteField" };

        protected override void DrawModuleInspector()
        {
            EditorGUILayout.HelpBox(EditorI18n.GetTranslation("noSettings"), MessageType.Info);
        }
    }
#endif
}
