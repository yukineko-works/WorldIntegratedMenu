
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon;
using System;
using yukineko.WorldIntegratedMenu.EditorShared;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class RadioButtonHelper : UdonSharpBehaviour
    {
        [SerializeField] private GameObject _radioButtonTemplate;
        [SerializeField] private Transform _radioButtonContainer;
        [SerializeField] private string[] _radioButtonLabels;
        [SerializeField] private string[] _radioButtonValues;
        [SerializeField] private int _defaultSelectedIndex;
        [SerializeField] private UdonBehaviour _udonBehaviour;
        [SerializeField] private int _udonBehaviourIndex = -1;
        [SerializeField] private string _udonCustomEventMethod;
        [SerializeField] private I18nManager _i18nManager;

        private ToggleGroup _toggleButtonGroup;
        private string _currentValue;
        private bool _initialized = false;

        public string Value => _currentValue;

        private void Start()
        {
            if (_udonBehaviour != null && _udonBehaviourIndex != -1)
            {
                var udonBehaviours = _udonBehaviour.GetComponents<UdonBehaviour>();
                if (udonBehaviours.Length > _udonBehaviourIndex)
                {
                    _udonBehaviour = udonBehaviours[_udonBehaviourIndex];
                }
            }

            _currentValue = _radioButtonValues[_defaultSelectedIndex];
            _toggleButtonGroup = GetComponent<ToggleGroup>();
            if (_toggleButtonGroup == null)
            {
                Debug.LogError("RadioButtonHelper: ToggleGroup component not found on the same GameObject.");
                return;
            }

            if (_radioButtonLabels.Length != _radioButtonValues.Length)
            {
                Debug.LogError("RadioButtonHelper: RadioButtonLabels and RadioButtonValues arrays must have the same length.");
                return;
            }

            _radioButtonTemplate.SetActive(false);
            _radioButtonTemplate.GetComponent<Toggle>().isOn = false;

            for (int i = 0; i < _radioButtonLabels.Length; i++)
            {
                var radioButton = Instantiate(_radioButtonTemplate, _radioButtonContainer);
                radioButton.name = _radioButtonValues[i];
                radioButton.SetActive(true);

                var textObject = radioButton.transform.Find("Label");
                var label = _radioButtonLabels[i];
                if (_i18nManager != null && label.StartsWith("i18n:"))
                {
                    var i18n = textObject.GetComponent<ApplyI18n>();
                    i18n.manager = _i18nManager;
                    i18n.key = label.Replace("i18n:", "");
                    i18n.Apply();
                }
                else
                {
                    textObject.GetComponent<Text>().text = label;
                }

                radioButton.GetComponent<Toggle>().isOn = i == _defaultSelectedIndex;
            }

            _initialized = true;
        }

        public void SetValue(string value)
        {
            if (!_initialized)
            {
                var index = Array.IndexOf(_radioButtonValues, value);
                if (index >= 0) _defaultSelectedIndex = index;
            }
            else
            {
                foreach (var toggle in _radioButtonContainer.GetComponentsInChildren<Toggle>())
                {
                    if (toggle.name == value)
                    {
                        toggle.isOn = true;
                        break;
                    }
                }
            }
        }

        public void OnValueChanged()
        {
            var changed = false;
            foreach (var toggle in _radioButtonContainer.GetComponentsInChildren<Toggle>())
            {
                if (toggle.isOn)
                {
                    if (toggle.name == _currentValue) break;
                    _currentValue = toggle.name;
                    changed = true;
                    break;
                }
            }

            if (!changed || _udonBehaviour == null || string.IsNullOrEmpty(_udonCustomEventMethod)) return;
            _udonBehaviour.SendCustomEvent(_udonCustomEventMethod);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(RadioButtonHelper))]
    internal class RadioButtonHelperInspector : Editor
    {
        private bool _showObjectProperties = false;
        private ReorderableList _reorderableList;

        private void GenerateList()
        {
            var labels = serializedObject.FindProperty("_radioButtonLabels");
            var values = serializedObject.FindProperty("_radioButtonValues");

            if (labels.arraySize != values.arraySize)
            {
                labels.arraySize = values.arraySize = Mathf.Max(labels.arraySize, values.arraySize);
            }

            _reorderableList = new ReorderableList(serializedObject, labels, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, EditorI18n.GetTranslation("labelsAndValues")),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, labels.GetArrayElementAtIndex(index), EditorI18n.GetGUITranslation("label"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, values.GetArrayElementAtIndex(index), EditorI18n.GetGUITranslation("value"));
                },
                drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
                {
                    if (index % 2 == 0)
                    {
                        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
                    }
                },
                elementHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 3,
                onAddCallback = (list) =>
                {
                    labels.InsertArrayElementAtIndex(labels.arraySize);
                    values.InsertArrayElementAtIndex(values.arraySize);

                    labels.GetArrayElementAtIndex(labels.arraySize - 1).stringValue = "";
                    values.GetArrayElementAtIndex(values.arraySize - 1).stringValue = "";
                },
                onRemoveCallback = (list) =>
                {
                    if (Event.current.shift || EditorUtility.DisplayDialog(EditorI18n.GetTranslation("warning"), EditorI18n.GetTranslation("beforeDelete"), EditorI18n.GetTranslation("delete"), EditorI18n.GetTranslation("cancel")))
                    {
                        labels.DeleteArrayElementAtIndex(list.index);
                        values.DeleteArrayElementAtIndex(list.index);
                    }
                },
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RadioButtonHelper", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            var udonBehaviour = serializedObject.FindProperty("_udonBehaviour");
            EditorGUILayout.PropertyField(udonBehaviour);
            if (udonBehaviour.objectReferenceValue != null)
            {
                var target = udonBehaviour.objectReferenceValue as UdonBehaviour;
                var udonBehaviours = target.GetComponents<UdonBehaviour>();
                if (udonBehaviours.Length > 1)
                {
                    var udonList = udonBehaviours.Select(x => x.programSource.name).ToArray();
                    var udonIndex = serializedObject.FindProperty("_udonBehaviourIndex");
                    var udonBehaviourIndex = EditorGUILayout.Popup("Program source", udonIndex.intValue, udonList);
                    if (udonBehaviourIndex >= 0)
                    {
                        udonIndex.intValue = udonBehaviourIndex;
                    }
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_udonCustomEventMethod"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_i18nManager"));
            EditorGUILayout.Space();

            if (_reorderableList == null) GenerateList();
            _reorderableList.DoLayoutList();

            var defaultIndex = serializedObject.FindProperty("_defaultSelectedIndex");
            var serializedKeys = serializedObject.FindProperty("_radioButtonValues");
            var keys = new List<string>();
            for (int i = 0; i < serializedKeys.arraySize; i++)
            {
                keys.Add(serializedKeys.GetArrayElementAtIndex(i).stringValue.Replace('/', '\u2215'));
            }

            var index = EditorGUILayout.Popup(EditorI18n.GetTranslation("defaultItem"), defaultIndex.intValue, keys.ToArray());
            if (index >= 0)
            {
                defaultIndex.intValue = index;
            }

            _showObjectProperties = EditorGUILayout.Foldout(_showObjectProperties, EditorI18n.GetTranslation("internalProperties"));
            if (_showObjectProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_radioButtonTemplate"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_radioButtonContainer"));
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
