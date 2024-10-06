
using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CloudSyncModule : UdonSharpBehaviour
    {
        [SerializeField] private CloudSyncManager _cloudSyncManager;
        [SerializeField] private Animator _saveStatusAnimator;
        [SerializeField] private Text _lastSaveTimeText;
        [SerializeField] private InputField _saveUrlCopyField;
        [SerializeField] private VRCUrlInputField _saveUrlPasteField;
        private VRCUrl _emptyUrl = new VRCUrl("");
        private I18nManager _i18nManager;

        private void Start()
        {
            if (_cloudSyncManager == null || _saveStatusAnimator == null || _lastSaveTimeText == null || _saveUrlCopyField == null || _saveUrlPasteField == null)
            {
                Debug.LogError("CloudSyncModule: Missing required components.");
                return;
            }

            _saveStatusAnimator.keepAnimatorStateOnDisable = true;
            _cloudSyncManager.OnLoad(this, nameof(RefreshStatus));

            _i18nManager = GetComponent<I18nManager>();
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
                        _lastSaveTimeText.text = _cloudSyncManager.LastSaveTime.ToLocalTime().ToString("G", _i18nManager.CurrentCulture);
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
        }

        public void OnSaveRequested()
        {
            _cloudSyncManager.RequestSave(_saveUrlPasteField.GetUrl());
            _saveUrlPasteField.SetUrl(_emptyUrl);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(CloudSyncModule))]
    public class CloudSyncModuleInspector : Editor
    {
        private InternalEditorI18n _i18n;
        private bool _showObjectProperties = false;

        private void OnEnable()
        {
            _i18n = new InternalEditorI18n("924493d0692e091469e86bb170d34d8e");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_i18n.GetTranslation("$title"), EditorStyles.largeLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(EditorI18n.GetTranslation("noSettings"), MessageType.Info);
            EditorGUILayout.Space();

            _showObjectProperties = EditorGUILayout.Foldout(_showObjectProperties, EditorI18n.GetTranslation("internalProperties"));
            if (_showObjectProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_cloudSyncManager"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_saveStatusAnimator"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_lastSaveTimeText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_saveUrlCopyField"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_saveUrlPasteField"));
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
