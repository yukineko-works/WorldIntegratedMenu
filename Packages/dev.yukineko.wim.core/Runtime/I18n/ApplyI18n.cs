
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections.Generic;
using yukineko.WorldIntegratedMenu.EditorShared;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ApplyI18n : UdonSharpBehaviour
    {
        public I18nManager manager;
        public string key;
        public DynamicArgs[] args;

        public void Apply(string language = null)
        {
            if (manager == null || !manager.Initialized || string.IsNullOrEmpty(key)) return;

            var text = GetComponent<Text>();
            if (text == null) return;

            if (args != null)
            {
                text.text = manager.GetTranslationWithArgs(key, args, language);
            }
            else
            {
                text.text = manager.GetTranslation(key, language);
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(ApplyI18n))]
    internal class ApplyI18nInspector : Editor
    {
        private ApplyI18n _applyI18n;
        private ReorderableList _reorderableList;
        private List<DynamicArgs> _argsList;

        private void OnEnable()
        {
            _applyI18n = target as ApplyI18n;
            GenerateList();
        }

        private void GenerateList()
        {
            _argsList = _applyI18n.args != null ? new List<DynamicArgs>(_applyI18n.args) : new List<DynamicArgs>();
            _reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("args"), true, true, true, true)
            {
                draggable = true,
                elementHeight = EditorGUIUtility.singleLineHeight + (EditorGUIUtility.standardVerticalSpacing * 2),
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, EditorI18n.GetTranslation("args"));
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    var element = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, element, new GUIContent($"Argument {index + 1}"));
                },
                onAddCallback = (ReorderableList list) =>
                {
                    _argsList.Add(DynamicArgs.VRCLocalPlayerName);
                },
                onRemoveCallback = (ReorderableList list) =>
                {
                    _argsList.RemoveAt(list.index);
                },
                onChangedCallback = (ReorderableList list) =>
                {
                    Apply(true);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Apply I18n", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(EditorI18n.GetTranslation("applyI18nTips"), MessageType.Info);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("manager"), new GUIContent("Manager"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("key"), new GUIContent("Key"));
            EditorGUILayout.Space(20);

            _reorderableList.DoLayoutList();

            Apply();
        }

        private void Apply(bool force = false)
        {
            if (force || EditorGUI.EndChangeCheck())
            {
                serializedObject.FindProperty("args").arraySize = _argsList.Count;
                for (int i = 0; i < _argsList.Count; i++)
                {
                    serializedObject.FindProperty("args").GetArrayElementAtIndex(i).enumValueIndex = (int)_argsList[i];
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}
