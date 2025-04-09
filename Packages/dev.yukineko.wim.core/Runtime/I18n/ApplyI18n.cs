using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using yukineko.WorldIntegratedMenu.EditorShared;
using System;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ApplyI18n : UdonSharpBehaviour
    {
        public I18nManager manager;
        public string key;
        public I18nArgumentType[] args;
        public string[] argValues;


        public void Apply(string language = null)
        {
            if (manager == null || !manager.Initialized || string.IsNullOrEmpty(key)) return;

            var text = GetComponent<Text>();
            if (text == null) return;

            if (args != null)
            {
                text.text = manager.GetTranslationWithArgs(key, args, argValues, language);
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
        private ListDrawer _listDrawer;

        private void OnEnable()
        {
            serializedObject.Update();
            var args = serializedObject.FindProperty("args");
            var argValues = serializedObject.FindProperty("argValues");

            if (args.arraySize != argValues.arraySize)
            {
                args.arraySize = argValues.arraySize = Mathf.Max(args.arraySize, argValues.arraySize);
                Apply(true);
            }

            GenerateList();
        }

        private void GenerateList()
        {
            serializedObject.Update();
            var args = serializedObject.FindProperty("args");
            var argValues = serializedObject.FindProperty("argValues");

            var menu = new GenericMenu();
            foreach (int type in Enum.GetValues(typeof(I18nArgumentType)))
            {
                menu.AddItem(new GUIContent(Enum.GetName(typeof(I18nArgumentType), type)), false, () =>
                {
                    args.InsertArrayElementAtIndex(args.arraySize);
                    argValues.InsertArrayElementAtIndex(argValues.arraySize);
                    var element = args.GetArrayElementAtIndex(args.arraySize - 1);
                    var valueElement = argValues.GetArrayElementAtIndex(argValues.arraySize - 1);

                    element.enumValueIndex = type;
                    valueElement.stringValue = string.Empty;

                    Apply(true);
                });
            }

            _listDrawer = new ListDrawer(args, new ListDrawerCallbacks() {
                drawHeader = () => EditorI18n.GetTranslation("args"),
                drawElement = (rect, index, isActive, isFocused) =>
                {
                    var element = args.GetArrayElementAtIndex(index);
                    var valueElement = argValues.GetArrayElementAtIndex(index);

                    var label = new GUIContent($"[{index}] {Enum.GetName(typeof(I18nArgumentType), element.enumValueIndex)}");
                    if (element.enumValueIndex == (int)I18nArgumentType.Dynamic)
                    {
                        var enumValue = Enum.TryParse<I18nDynamicArgumentType>(valueElement.stringValue, out var value) ? value : I18nDynamicArgumentType.None;
                        valueElement.stringValue = EditorGUI.EnumPopup(rect, label, enumValue).ToString();
                    }
                    else
                    {
                        EditorGUI.PropertyField(rect, valueElement, label);
                    }
                },
                elementCount = (index) => 1,
                onAddDropdown = (rect, list) =>
                {
                    menu.DropDown(rect);
                },
                onRemove = (list) =>
                {
                    args.DeleteArrayElementAtIndex(list.index);
                    argValues.DeleteArrayElementAtIndex(list.index);
                    Apply(true);
                },
                onReorderWithDetails = (list, beforeIndex, afterIndex) =>
                {
                    var before = argValues.GetArrayElementAtIndex(beforeIndex);
                    var after = argValues.GetArrayElementAtIndex(afterIndex);
                    var beforeValue = before.stringValue;
                    var afterValue = after.stringValue;

                    before.stringValue = afterValue;
                    after.stringValue = beforeValue;

                    Apply(true);
                },
            });
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

            _listDrawer.Draw();

            Apply();
        }

        private void Apply(bool force = false)
        {
            if (force || EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}
