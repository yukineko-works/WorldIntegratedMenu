using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu
{
    public enum I18nTimeFormat
    {
        DateTimeFull,
        DateTimeShort,
        TimeFull,
        TimeShort,
        Date
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ApplyTimeI18n : UdonSharpBehaviour
    {
        public I18nManager manager;
        public DateTimeOffset time;
        public I18nTimeFormat format = I18nTimeFormat.DateTimeFull;
        private Text _text;
        private bool _applyOnStart = false;

        private void Start()
        {
            _text = GetComponent<Text>();
            if (_applyOnStart) Apply();
        }

        public void Apply()
        {
            if (_text == null)
            {
                _applyOnStart = true;
                return;
            }

            if (manager == null || !manager.Initialized) return;
            var time = this.time != null ? this.time : DateTimeOffset.Now.ToLocalTime();
            var timeFormat = string.Empty;

            switch (format)
            {
                case I18nTimeFormat.DateTimeFull:
                    timeFormat = "G";
                    break;

                case I18nTimeFormat.DateTimeShort:
                    timeFormat = "g";
                    break;

                case I18nTimeFormat.TimeFull:
                    timeFormat = "T";
                    break;

                case I18nTimeFormat.TimeShort:
                    timeFormat = "t";
                    break;

                case I18nTimeFormat.Date:
                    timeFormat = "d";
                    break;
            }

            _text.text = time.ToString(timeFormat, manager.CurrentCulture);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(ApplyTimeI18n))]
    internal class ApplyTimeI18nInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Apply Time I18n", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(EditorI18n.GetTranslation("applyI18nTips"), MessageType.Info);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("manager"), new GUIContent("Manager"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("format"), new GUIContent("Format"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}