using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using yukineko.WorldIntegratedMenu.EditorMenu;

namespace yukineko.WorldIntegratedMenu
{
    [CustomEditor(typeof(WIMCore))]
    public class CoreMenu : Editor
    {
        private GameObject _moduleContainer;
        private ThemeManager _themeManager;
        private List<ModuleMetadata> _modulesCache;

        private ReorderableList _reorderableList;

        private void OnEnable()
        {
            _moduleContainer = GameObject.Find("Modules");
            _themeManager = FindObjectOfType<ThemeManager>();
        }

        private void GenerateList()
        {
            if (_modulesCache == null)
            {
                _modulesCache = _moduleContainer.GetComponentsInChildren<ModuleMetadata>().ToList();
            }

            _reorderableList = new ReorderableList(_modulesCache, typeof(ModuleMetadata))
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, EditorI18n.GetTranslation("modules")),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += EditorGUIUtility.standardVerticalSpacing * 2;

                    var module = _modulesCache[index];
                    var moduleRegistryItem = ModuleRegistry.ModuleList.TryGetValue(module.ModuleId, out var item) ? item : null;

                    EditorGUI.LabelField(rect, moduleRegistryItem != null && !module.forceUseModuleName ? moduleRegistryItem.GetTitle() : module.moduleName);

                    if (GUI.Button(new Rect(rect.x + rect.width - 120, rect.y, 120, EditorGUIUtility.singleLineHeight), EditorI18n.GetTranslation("moduleSettings")))
                    {
                        Selection.activeObject = module.gameObject;
                    }
                },
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 3,
                onAddDropdownCallback = (rect, list) =>
                {
                    var menu = new GenericMenu();
                    foreach (var module in ModuleRegistry.ModuleList.Values)
                    {
                        menu.AddItem(new GUIContent(module.GetTitle()), false, () =>
                        {
                            var item = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(module.PrefabGuid));
                            if (item == null) return;
                            var prefab = PrefabUtility.InstantiatePrefab(item, _moduleContainer.transform) as GameObject;
                            _modulesCache.Add(prefab.GetComponent<ModuleMetadata>());
                        });
                    }

                    menu.DropDown(rect);
                },
                onRemoveCallback = (list) =>
                {
                    if (EditorUtility.DisplayDialog(EditorI18n.GetTranslation("warning"), EditorI18n.GetTranslation("beforeDelete"), EditorI18n.GetTranslation("delete"), EditorI18n.GetTranslation("cancel")))
                    {
                        var module = _modulesCache[list.index];
                        DestroyImmediate(module.gameObject);
                        _modulesCache.RemoveAt(list.index);
                    }
                },
                onReorderCallback = (list) =>
                {
                    var module = _modulesCache[list.index];
                    module.transform.SetSiblingIndex(list.index);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            // LanguageSelector
            var currentIndex = InternalEditorI18n.availableLanguages.Keys.ToList().IndexOf(InternalEditorI18n.CurrentLanguage);
            var availableLanguages = InternalEditorI18n.availableLanguages.Values.ToArray();
            var langIndex = EditorGUILayout.Popup("Language", currentIndex, availableLanguages);

            if (langIndex != currentIndex)
            {
                InternalEditorI18n.CurrentLanguage = InternalEditorI18n.availableLanguages.ElementAt(langIndex).Key;
                HierachyMenu.RebuildMenu();
            }

            if (_moduleContainer == null)
            {
                EditorGUILayout.HelpBox("Modules container not found.", MessageType.Error);
                return;
            }

            if (_themeManager == null)
            {
                EditorGUILayout.HelpBox("Theme Manager not found.", MessageType.Error);
                return;
            }

            if (_reorderableList == null) GenerateList();
            _reorderableList.DoLayoutList();
        }
    }
}
