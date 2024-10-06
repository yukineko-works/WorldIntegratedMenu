
using System;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Linq;


#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace yukineko.WorldIntegratedMenu
{
    [DisallowMultipleComponent]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ModuleMetadata : UdonSharpBehaviour
    {
        public string moduleName;
        public Sprite moduleIcon;
        public bool forceUseModuleName = false;
        public GameObject content;

        [HideInInspector] public I18nManager i18nManager;
        [SerializeField] private string _moduleId = "";
        [SerializeField] private bool _isUnique = true;
        [SerializeField] private bool _hideInMenu = false;
        [SerializeField] private UdonBehaviour[] _onModuleCalledBehaviours = new UdonBehaviour[0];
        [SerializeField] private int[] _onModuleCalledBehaviourIndexes = new int[0];
        private UdonBehaviour[] _onModuleCalledMethods;
        private string _uuid = Guid.NewGuid().ToString();

        public string Uuid => _uuid;
        public string ModuleId => _moduleId;
        public bool IsUnique => _isUnique;
        public bool HideInMenu => _hideInMenu;

        private void Start()
        {
            _onModuleCalledMethods = new UdonBehaviour[_onModuleCalledBehaviours.Length];
            for (int i = 0; i < _onModuleCalledBehaviours.Length; i++)
            {
                if (_onModuleCalledBehaviours[i] != null && _onModuleCalledBehaviourIndexes[i] != -1)
                {
                    var udonBehaviours = _onModuleCalledBehaviours[i].GetComponents<UdonBehaviour>();
                    if (udonBehaviours.Length > _onModuleCalledBehaviourIndexes[i])
                    {
                        _onModuleCalledMethods[i] = udonBehaviours[_onModuleCalledBehaviourIndexes[i]];
                    }
                }
            }
        }

        public void RegenerateUuid()
        {
            _uuid = Guid.NewGuid().ToString();
        }

        public void OnModuleCalled()
        {
            foreach (var udonBehaviour in _onModuleCalledMethods)
            {
                if (udonBehaviour != null)
                {
                    udonBehaviour.SendCustomEvent("OnModuleCalled");
                }
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(ModuleMetadata))]
    internal class ModuleMetadataInspector : Editor
    {
        private ModuleMetadata _moduleMetadata;
        private bool _isOpeningInternalProperties;
        private bool _isOpeningDeveloperProperties;
        private ReorderableList _reorderableList;

        private void GenerateList()
        {
            var onModuleCalledBehaviours = serializedObject.FindProperty("_onModuleCalledBehaviours");
            var onModuleCalledBehaviourIndexes = serializedObject.FindProperty("_onModuleCalledBehaviourIndexes");

            if (onModuleCalledBehaviours.arraySize != onModuleCalledBehaviourIndexes.arraySize)
            {
                onModuleCalledBehaviourIndexes.arraySize = onModuleCalledBehaviours.arraySize = Math.Max(onModuleCalledBehaviours.arraySize, onModuleCalledBehaviourIndexes.arraySize);
            }

            _reorderableList = new ReorderableList(serializedObject, onModuleCalledBehaviours, true, true, true, true)
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "onModuleCalled");
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += EditorGUIUtility.standardVerticalSpacing;

                    var element = onModuleCalledBehaviours.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, GUIContent.none);

                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    var udonBehaviour = element.objectReferenceValue as UdonBehaviour;
                    if (udonBehaviour != null)
                    {
                        var udonBehaviours = udonBehaviour.GetComponents<UdonBehaviour>();
                        var udonBehaviourIndex = onModuleCalledBehaviourIndexes.GetArrayElementAtIndex(index);
                        var udonList = udonBehaviours.Select(x => x.programSource.name).ToArray();
                        var udonBehaviourIndexValue = udonBehaviourIndex.intValue;
                        udonBehaviourIndexValue = EditorGUI.Popup(rect, udonBehaviourIndexValue, udonList);
                        udonBehaviourIndex.intValue = udonBehaviourIndexValue;
                    }
                },
                elementHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 3,
                onAddCallback = (list) =>
                {
                    onModuleCalledBehaviours.InsertArrayElementAtIndex(onModuleCalledBehaviours.arraySize);
                    onModuleCalledBehaviourIndexes.InsertArrayElementAtIndex(onModuleCalledBehaviourIndexes.arraySize);

                    onModuleCalledBehaviours.GetArrayElementAtIndex(onModuleCalledBehaviours.arraySize - 1).objectReferenceValue = null;
                    onModuleCalledBehaviourIndexes.GetArrayElementAtIndex(onModuleCalledBehaviourIndexes.arraySize - 1).intValue = -1;
                },
                onRemoveCallback = (list) =>
                {
                    if (EditorUtility.DisplayDialog(EditorI18n.GetTranslation("warning"), EditorI18n.GetTranslation("beforeDelete"), EditorI18n.GetTranslation("delete"), EditorI18n.GetTranslation("cancel")))
                    {
                        onModuleCalledBehaviours.DeleteArrayElementAtIndex(list.index);
                        onModuleCalledBehaviourIndexes.DeleteArrayElementAtIndex(list.index);
                    }
                },
            };
        }

        private void OnEnable()
        {
            _moduleMetadata = target as ModuleMetadata;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Module Metadata", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            _moduleMetadata.moduleName = EditorGUILayout.TextField(EditorI18n.GetTranslation("moduleName"), _moduleMetadata.moduleName);
            _moduleMetadata.moduleIcon = (Sprite)EditorGUILayout.ObjectField(EditorI18n.GetTranslation("moduleIcon"), _moduleMetadata.moduleIcon, typeof(Sprite), false);
            EditorGUILayout.Space();

            _isOpeningInternalProperties = EditorGUILayout.Foldout(_isOpeningInternalProperties, EditorI18n.GetTranslation("internalProperties"));
            if (_isOpeningInternalProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("content"));
                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }

            _isOpeningDeveloperProperties = EditorGUILayout.Foldout(_isOpeningDeveloperProperties, EditorI18n.GetTranslation("developerProperties"));
            if (_isOpeningDeveloperProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_moduleId"), EditorI18n.GetGUITranslation("moduleUniqueId"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_isUnique"), EditorI18n.GetGUITranslation("disallowMultipleInstances"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("forceUseModuleName"), EditorI18n.GetGUITranslation("forceUseModuleName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_hideInMenu"), EditorI18n.GetGUITranslation("hideInMenu"));
                EditorGUILayout.Space();

                if (_reorderableList == null)
                {
                    GenerateList();
                }

                _reorderableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_moduleMetadata);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
