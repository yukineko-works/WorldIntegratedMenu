using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Data;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu.Editor
{
    [CustomEditor(typeof(WIMCore))]
    public class CoreMenu : UnityEditor.Editor
    {
        private Transform _moduleContainer;
        private ThemeManager _themeManager;
        private List<ModuleMetadata> _modulesCache;
        private ReorderableList _reorderableList;
        private DataList _themes;
        private SerializedObject _themeManagerSerializedObject;
        private List<string> _usedUniqueModuleIds;
        private List<string> _duplicatedUniqueModuleIds;

        private bool _isReferencedByProjectWindow = false;

        private int _selectedTabIndex = 0;
        private string[] _themeNames = new string[0];
        private int _selectedThemeIndex = 0;
        private bool _showThemeSettings = false;

        private void OnEnable()
        {
            if (AssemblyReloadHandler.Reloading || Application.isPlaying) return;
            _isReferencedByProjectWindow = !((WIMCore)target).gameObject.scene.IsValid();

            var moduleManager = FindObjectOfType<ModuleManager>(true);
            _moduleContainer = moduleManager == null ? null : moduleManager.ModulesRoot;
            _themeManager = FindObjectOfType<ThemeManager>(true);

            if (_themeManager != null)
            {
                _themeManagerSerializedObject = new SerializedObject(_themeManager);
                _themeManagerSerializedObject.Update();

                if (_themeManager.ThemePreset != null && VRCJson.TryDeserializeFromJson(_themeManager.ThemePreset, out var _th) && _th.TokenType == TokenType.DataList)
                {
                    _themes = _th.DataList;
                    _themeNames = _themes.Select(t =>
                    {
                        var res = t.DataDictionary.TryGetValue("$name", TokenType.String, out var name);
                        return res ? name.String : "Unknown";
                    }).ToArray();

                }
            }
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
                    var xSpacing = 0;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += EditorGUIUtility.standardVerticalSpacing * 2;

                    var module = _modulesCache[index];
                    var moduleRegistryItem = ModuleRegistry.ModuleList.TryGetValue(module.ModuleId, out var item) ? item : null;

                    if (module.moduleIcon != null)
                    {
                        rect.x += 4;
                        GUI.DrawTexture(new Rect(rect.x, rect.y + 1, 16, 16), module.moduleIcon.texture);
                        rect.x += 24;
                        xSpacing += 28;
                    }

                    EditorGUI.LabelField(rect, moduleRegistryItem != null && !module.forceUseModuleName ? moduleRegistryItem.GetTitle() : module.moduleName);

                    if (GUI.Button(new Rect(rect.x + rect.width - xSpacing - 120, rect.y, 120, EditorGUIUtility.singleLineHeight), EditorI18n.GetTranslation("moduleSettings")))
                    {
                        Selection.activeObject = module.gameObject;
                    }
                },
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 3,
                onAddDropdownCallback = (rect, list) =>
                {
                    var menu = new GenericMenu();
                    UniqueModuleDuplicateCheck();

                    foreach (var m in ModuleRegistry.ModuleList)
                    {
                        if (!_usedUniqueModuleIds.Contains(m.Key))
                        {
                            var module = m.Value;
                            menu.AddItem(new GUIContent(module.GetTitle()), false, () =>
                            {
                                var item = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(module.PrefabGuid));
                                if (item == null) return;
                                var prefab = PrefabUtility.InstantiatePrefab(item, _moduleContainer.transform) as GameObject;
                                _modulesCache.Add(prefab.GetComponent<ModuleMetadata>());
                                UniqueModuleDuplicateCheck(true);
                            });
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent(m.Value.GetTitle()));
                        }
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
                        UniqueModuleDuplicateCheck(true);
                    }
                },
                onReorderCallback = (list) =>
                {
                    var module = _modulesCache[list.index];
                    module.transform.SetSiblingIndex(list.index);
                }
            };
        }

        private void UniqueModuleDuplicateCheck(bool force = false)
        {
            if (_usedUniqueModuleIds != null && !force) return;
            _usedUniqueModuleIds = new List<string>();
            _duplicatedUniqueModuleIds = new List<string>();

            foreach (var module in _modulesCache)
            {
                if (!module.IsUnique) continue;
                if (!_usedUniqueModuleIds.Contains(module.ModuleId))
                {
                    _usedUniqueModuleIds.Add(module.ModuleId);
                }
                else if (!_duplicatedUniqueModuleIds.Contains(module.ModuleId))
                {
                    _duplicatedUniqueModuleIds.Add(module.ModuleId);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (AssemblyReloadHandler.Reloading) return;

            EditorGUILayout.LabelField("World Integrated Menu", LabelStyles.header, GUILayout.ExpandWidth(true));
            var version = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CoreMenu).Assembly).version;
            EditorGUILayout.LabelField("v" + version, LabelStyles.center, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("playModeWarning"), MessageType.Warning);
                return;
            }

            var currentIndex = InternalEditorI18n.availableLanguages.Keys.ToList().IndexOf(InternalEditorI18n.CurrentLanguage);
            var availableLanguages = InternalEditorI18n.availableLanguages.Values.ToArray();
            var langIndex = EditorGUILayout.Popup("Language", currentIndex, availableLanguages);

            if (langIndex != currentIndex)
            {
                InternalEditorI18n.CurrentLanguage = InternalEditorI18n.availableLanguages.ElementAt(langIndex).Key;
                HierachyMenu.RebuildMenu();
            }

            if (_isReferencedByProjectWindow)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("pleasePlaceInScene"), MessageType.Error);
                return;
            }

            EditorGUILayout.Space(24);
            var tabs = new[]
            {
                EditorI18n.GetTranslation("moduleSettings"),
                EditorI18n.GetTranslation("themeSettings"),
                EditorI18n.GetTranslation("versionInfo")
            };

            _selectedTabIndex = GUILayout.Toolbar(_selectedTabIndex, tabs, "LargeButton", GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(12);

            switch (_selectedTabIndex)
            {
                case 0:
                    TabModuleSettings();
                    break;
                case 1:
                    TabThemeSettings();
                    break;
                case 2:
                    TabVersionInfo();
                    break;
            }
        }

        private void TabModuleSettings()
        {
            if (_moduleContainer == null)
            {
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("moduleContainerNotFound"), MessageType.Error);
            }
            else
            {
                if (_reorderableList == null) GenerateList();
                _reorderableList.DoLayoutList();

                UniqueModuleDuplicateCheck();
                if (_duplicatedUniqueModuleIds != null && _duplicatedUniqueModuleIds.Count > 0)
                {
                    var duplicatedModules = string.Join("\n", _duplicatedUniqueModuleIds.Select(id => "- " + (ModuleRegistry.ModuleList.TryGetValue(id, out var m) ? m.GetTitle() : id)).ToArray());
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox($"{EditorI18n.GetTranslation("uniqueModuleDuplicatedError")}\n{duplicatedModules}", MessageType.Error);
                }
            }
        }

        private void TabThemeSettings()
        {
            if (_themeManager == null || _themeManagerSerializedObject == null)
            {
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("themeManagerNotFound"), MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField(EditorI18n.GetTranslation("chooseFromPresets"));
                if (_themes == null)
                {
                    EditorGUILayout.HelpBox(EditorI18n.GetTranslation("presetsNotFound"), MessageType.Error);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    _selectedThemeIndex = EditorGUILayout.Popup(_selectedThemeIndex, _themeNames);

                    if (GUILayout.Button(EditorI18n.GetTranslation("useThisTheme")))
                    {
                        var theme = _themes[_selectedThemeIndex];
                        if (theme.TokenType != TokenType.DataDictionary) return;

                        if (theme.DataDictionary.TryGetValue("accent", TokenType.String, out var hexAccent) && ColorUtility.TryParseHtmlString(hexAccent.String, out var accentColor))
                        {
                            _themeManagerSerializedObject.FindProperty("_accentColor").colorValue = accentColor;
                        }

                        if (theme.DataDictionary.TryGetValue("base", TokenType.String, out var hexBase) && ColorUtility.TryParseHtmlString(hexBase.String, out var baseColor))
                        {
                            _themeManagerSerializedObject.FindProperty("_baseColor").colorValue = baseColor;
                        }

                        if (theme.DataDictionary.TryGetValue("surface", TokenType.String, out var hexSurface) && ColorUtility.TryParseHtmlString(hexSurface.String, out var surfaceColor))
                        {
                            _themeManagerSerializedObject.FindProperty("_surfaceColor").colorValue = surfaceColor;
                        }

                        if (theme.DataDictionary.TryGetValue("text", TokenType.String, out var hexText) && ColorUtility.TryParseHtmlString(hexText.String, out var textColor))
                        {
                            _themeManagerSerializedObject.FindProperty("_textColor").colorValue = textColor;
                        }

                        if (theme.DataDictionary.TryGetValue("success", TokenType.String, out var hexSuccess) && ColorUtility.TryParseHtmlString(hexSuccess.String, out var successColor))
                        {
                            _themeManagerSerializedObject.FindProperty("_successColor").colorValue = successColor;
                        }

                        if (theme.DataDictionary.TryGetValue("warning", TokenType.String, out var hexWarning) && ColorUtility.TryParseHtmlString(hexWarning.String, out var warningColor))
                        {
                            _themeManagerSerializedObject.FindProperty("_warningColor").colorValue = warningColor;
                        }

                        if (theme.DataDictionary.TryGetValue("error", TokenType.String, out var hexError) && ColorUtility.TryParseHtmlString(hexError.String, out var errorColor))
                        {
                            _themeManagerSerializedObject.FindProperty("_errorColor").colorValue = errorColor;
                        }

                        if (theme.DataDictionary.TryGetValue("info", TokenType.String, out var hexInfo) && ColorUtility.TryParseHtmlString(hexInfo.String, out var infoColor))
                        {
                            _themeManagerSerializedObject.FindProperty("_infoColor").colorValue = infoColor;
                        }

                        _themeManagerSerializedObject.ApplyModifiedProperties();
                        _themeManager.ApplyTheme();

                        EditorApplication.delayCall += () =>
                        {
                            SceneView.RepaintAll();
                        };
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();

                _showThemeSettings = EditorGUILayout.Foldout(_showThemeSettings, EditorI18n.GetTranslation("setAsSeparately"));
                if (_showThemeSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_themeManagerSerializedObject.FindProperty("_accentColor"), new GUIContent("Accent"));
                    EditorGUILayout.PropertyField(_themeManagerSerializedObject.FindProperty("_baseColor"), new GUIContent("Base"));
                    EditorGUILayout.PropertyField(_themeManagerSerializedObject.FindProperty("_surfaceColor"), new GUIContent("Surface"));
                    EditorGUILayout.PropertyField(_themeManagerSerializedObject.FindProperty("_textColor"), new GUIContent("Text"));
                    EditorGUILayout.PropertyField(_themeManagerSerializedObject.FindProperty("_successColor"), new GUIContent("Success"));
                    EditorGUILayout.PropertyField(_themeManagerSerializedObject.FindProperty("_warningColor"), new GUIContent("Warning"));
                    EditorGUILayout.PropertyField(_themeManagerSerializedObject.FindProperty("_errorColor"), new GUIContent("Error"));
                    EditorGUILayout.PropertyField(_themeManagerSerializedObject.FindProperty("_infoColor"), new GUIContent("Info"));
                    _themeManagerSerializedObject.ApplyModifiedProperties();

                    EditorGUILayout.Space();
                    if (GUILayout.Button(EditorI18n.GetTranslation("refrectSettings")))
                    {
                        _themeManager.ApplyTheme();
                        EditorApplication.delayCall += () =>
                        {
                            SceneView.RepaintAll();
                        };
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void TabVersionInfo()
        {
            var version = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CoreMenu).Assembly).version;
            EditorGUILayout.LabelField("Version", version);
            EditorGUILayout.Space(12);

            EditorGUILayout.LabelField("Links", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("GitHub"))
            {
                Application.OpenURL("https://github.com/yukineko-works/WorldIntegratedMenu");
            }
            if (GUILayout.Button("Booth"))
            {
                Application.OpenURL("https://yukineko-works.booth.pm/");
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    internal static class LabelStyles
    {
        public readonly static GUIStyle header = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        public readonly static GUIStyle center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter };
    }
}
