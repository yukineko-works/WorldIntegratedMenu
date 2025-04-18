using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu.Editor
{
    [CustomEditor(typeof(WIMCore))]
    public class CoreMenu : UnityEditor.Editor
    {
        private Transform _moduleContainer;
        private ThemeManager _themeManager;
        private List<ModuleMetadata> _modulesCache;
        private ListDrawer _listDrawer;
        private ThemePreset[] _themes;
        private SerializedObject _themeManagerSerializedObject;
        private SerializedObject _uiManagerSerializedObject;
        private SerializedObject _quickMenuManagerSerializedObject;
        private List<string> _usedUniqueModuleIds;
        private List<string> _duplicatedUniqueModuleIds;

        private bool _isReferencedByProjectWindow = false;

        private int _selectedTabIndex = 0;
        private string[] _themeNames = new string[0];
        private int _selectedThemeIndex = 0;
        private bool _showThemeSettings = false;
        private bool _showExtensionModuleReference = false;
        private Dictionary<VRQuickMenuOpenMethod, string> _vrOpenMethodNames = new Dictionary<VRQuickMenuOpenMethod, string>
        {
            { VRQuickMenuOpenMethod.Stick, "openByStick" },
            { VRQuickMenuOpenMethod.Trigger, "openByTrigger" }
        };
        private Dictionary<VRQuickMenuDominantHand, string> _vrDominantHandNames = new Dictionary<VRQuickMenuDominantHand, string>
        {
            { VRQuickMenuDominantHand.Left, "leftHand" },
            { VRQuickMenuDominantHand.Right, "rightHand" },
        };

        private void OnEnable()
        {
            if (Application.isPlaying) return;
            var gameObject = ((WIMCore)target).gameObject;
            _isReferencedByProjectWindow = !gameObject.scene.IsValid();

            var moduleManager = gameObject.GetComponentInChildren<ModuleManager>(true);
            _moduleContainer = moduleManager == null ? null : moduleManager.ModulesRoot;

            _themeManager = gameObject.GetComponentInChildren<ThemeManager>(true);
            if (_themeManager != null)
            {
                _themeManagerSerializedObject = new SerializedObject(_themeManager);
                var propThemePreset = _themeManagerSerializedObject.FindProperty("_themePreset");
                if (propThemePreset.objectReferenceValue != null)
                {
                    var themePresetJson = propThemePreset.objectReferenceValue as TextAsset;
                    if (themePresetJson != null)
                    {
                        try
                        {
                            var themePresets = JsonUtility.FromJson<ThemePresetsRoot>($"{{\"presets\": {themePresetJson.text}}}");
                            _themes = themePresets.presets;
                            _themeNames = _themes.Select(t => t.name).ToArray();
                        }
                        catch (Exception)
                        {
                            Debug.LogError("Failed to parse theme presets.");
                        }
                    }
                }
            }

            var uiManager = gameObject.GetComponentInChildren<UIManager>(true);
            if (uiManager != null)
            {
                _uiManagerSerializedObject = new SerializedObject(uiManager);
            }

            var quickMenuManager = gameObject.GetComponentInChildren<QuickMenuManager>(true);
            if (quickMenuManager != null)
            {
                _quickMenuManagerSerializedObject = new SerializedObject(quickMenuManager);
            }

            GenerateList();
        }

        private void GenerateList()
        {
            if (_modulesCache == null)
            {
                _modulesCache = _moduleContainer.GetComponentsInChildren<ModuleMetadata>().ToList();
            }

            _listDrawer = new ListDrawer(_modulesCache, new ListDrawerCallbacks() {
                drawHeader = () => EditorI18n.GetTranslation("modules"),
                drawElement = (rect, index, isActive, isFocused) =>
                {
                    var xSpacing = 0;
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
                onAddDropdown = (rect, list) =>
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
                onRemove = (list) =>
                {
                    var module = _modulesCache[list.index];
                    DestroyImmediate(module.gameObject);
                    _modulesCache.RemoveAt(list.index);
                    UniqueModuleDuplicateCheck(true);
                },
                onReorder = (list) =>
                {
                    var module = _modulesCache[list.index];
                    module.transform.SetSiblingIndex(list.index);
                },
                elementCount = index => 1
            });
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
            EditorGUILayout.LabelField("World Integrated Menu", UIStyles.header, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("v" + Updater.CurrentVersion, UIStyles.center, GUILayout.ExpandWidth(true));
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

            if (Updater.AvailableUpdate)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("wimUpdateAvailable"), MessageType.Info);
            }

            if (ModuleVersionManager.AvailableUpdate)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("moduleUpdateAvailable"), MessageType.Info);
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
                EditorI18n.GetTranslation("mainSettings"),
                EditorI18n.GetTranslation("themeSettings"),
                EditorI18n.GetTranslation("versionInfo")
            };

            _selectedTabIndex = GUILayout.Toolbar(_selectedTabIndex, tabs, "LargeButton", GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(12);

            switch (_selectedTabIndex)
            {
                case 0:
                    TabMainSettings();
                    break;
                case 1:
                    TabThemeSettings();
                    break;
                case 2:
                    TabVersionInfo();
                    break;
            }
        }

        private void TabMainSettings()
        {
            UIStyles.TitleBox(EditorI18n.GetTranslation("moduleSettings"), EditorI18n.GetTranslation("moduleSettingsDescription"), false);
            EditorGUILayout.Space();

            if (_moduleContainer == null)
            {
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("moduleContainerNotFound"), MessageType.Error);
            }
            else
            {
                _listDrawer.Draw();

                UniqueModuleDuplicateCheck();
                if (_duplicatedUniqueModuleIds != null && _duplicatedUniqueModuleIds.Count > 0)
                {
                    var duplicatedModules = string.Join("\n", _duplicatedUniqueModuleIds.Select(id => "- " + (ModuleRegistry.ModuleList.TryGetValue(id, out var m) ? m.GetTitle() : id)).ToArray());
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox($"{EditorI18n.GetTranslation("uniqueModuleDuplicatedError")}\n{duplicatedModules}", MessageType.Error);
                }

                _showExtensionModuleReference = EditorGUILayout.Foldout(_showExtensionModuleReference, EditorI18n.GetTranslation("extensionModuleDescription"));
                if (_showExtensionModuleReference)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(EditorI18n.GetTranslation("goModuleListPage")))
                    {
                        Application.OpenURL("https://vpm.yukineko.dev/docs/wim-modules/intro");
                    }

                    if (GUILayout.Button(EditorI18n.GetTranslation("findExtensionModuleInBooth")))
                    {
                        Application.OpenURL(ResolveUrl.Booth("items?tags%5B%5D=WIM拡張モジュール"));
                    }
                }
            }

            UIStyles.TitleBox(EditorI18n.GetTranslation("homeSettings"), EditorI18n.GetTranslation("homeSettingsDescription"));
            if (_uiManagerSerializedObject == null)
            {
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("uiManagerNotFound"), MessageType.Error);
            }
            else
            {
                var customWelcomeText = _uiManagerSerializedObject.FindProperty("_customWelcomeText");
                var enableCustomWelcomeText = EditorGUILayout.ToggleLeft(EditorI18n.GetTranslation("enableCustomWelcomeText"), !string.IsNullOrEmpty(customWelcomeText.stringValue));
                if (enableCustomWelcomeText)
                {
                    customWelcomeText.stringValue = EditorGUILayout.TextField(customWelcomeText.stringValue.Replace("<EMPTY>", ""));
                    if (string.IsNullOrEmpty(customWelcomeText.stringValue))
                    {
                        customWelcomeText.stringValue = "<EMPTY>";
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(customWelcomeText.stringValue))
                    {
                        if (EditorUtility.DisplayDialog(EditorI18n.GetTranslation("warning"), EditorI18n.GetTranslation("beforeDisableCustomWelcomeText"), EditorI18n.GetTranslation("delete"), EditorI18n.GetTranslation("cancel")))
                        {
                            customWelcomeText.stringValue = null;
                        }
                    }
                }

                _uiManagerSerializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(EditorI18n.GetTranslation("defaultOpenModule"));
            if (_moduleContainer != null && _modulesCache != null && _uiManagerSerializedObject != null)
            {
                var currentModule = _uiManagerSerializedObject.FindProperty("_defaultOpenModule").objectReferenceValue as ModuleMetadata;
                var moduleNames = _modulesCache.Select(m =>
                {
                    var moduleName = !m.forceUseModuleName && ModuleRegistry.ModuleList.TryGetValue(m.ModuleId, out var item) ? item.GetTitle() : m.moduleName;
                    return moduleName.Replace("/", "\u2215");
                }).Prepend(EditorI18n.GetTranslation("none")).ToArray();
                var selectedModuleIndex = currentModule == null ? 0 : _modulesCache.IndexOf(currentModule) + 1;
                var newModuleIndex = EditorGUILayout.Popup(selectedModuleIndex, moduleNames);

                if (newModuleIndex != selectedModuleIndex)
                {
                    _uiManagerSerializedObject.FindProperty("_defaultOpenModule").objectReferenceValue = newModuleIndex == 0 ? null : _modulesCache[newModuleIndex - 1];
                    _uiManagerSerializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("uiManagerNotFound"), MessageType.Error);
            }

            UIStyles.TitleBox(EditorI18n.GetTranslation("quickMenuSettings"), EditorI18n.GetTranslation("quickMenuSettingsDescription"));

            if (_quickMenuManagerSerializedObject == null)
            {
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("quickMenuManagerNotFound"), MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField(EditorI18n.GetTranslation("menuOpenMethodInVR"));

                var methodEnumCount = Enum.GetNames(typeof(VRQuickMenuOpenMethod)).Length;
                var methodEnumNames = new string[methodEnumCount];

                for (var i = 0; i < methodEnumCount; i++)
                {
                    methodEnumNames[i] = EditorI18n.GetTranslation(_vrOpenMethodNames[(VRQuickMenuOpenMethod)i]);
                }

                var selectedOpenMethod = _quickMenuManagerSerializedObject.FindProperty("_vrOpenMethod").enumValueIndex;
                var newOpenMethod = EditorGUILayout.Popup(selectedOpenMethod, methodEnumNames);
                if (newOpenMethod != selectedOpenMethod)
                {
                    _quickMenuManagerSerializedObject.FindProperty("_vrOpenMethod").enumValueIndex = newOpenMethod;
                    _quickMenuManagerSerializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(EditorI18n.GetTranslation("quickmenuDominantHandInVR"));

                var dominantHandEnumCount = Enum.GetNames(typeof(VRQuickMenuDominantHand)).Length;
                var dominantHandEnumNames = new string[dominantHandEnumCount];

                for (var i = 0; i < dominantHandEnumCount; i++)
                {
                    dominantHandEnumNames[i] = EditorI18n.GetTranslation(_vrDominantHandNames[(VRQuickMenuDominantHand)i]);
                }

                var selectedDominantHand = _quickMenuManagerSerializedObject.FindProperty("_dominantHand").enumValueIndex;
                var newDominantHand = EditorGUILayout.Popup(selectedDominantHand, dominantHandEnumNames);
                if (newDominantHand != selectedDominantHand)
                {
                    _quickMenuManagerSerializedObject.FindProperty("_dominantHand").enumValueIndex = newDominantHand;
                    _quickMenuManagerSerializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space();
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
                        var theme = _themes[_selectedThemeIndex]?.color;
                        if (theme == null) return;

                        _themeManagerSerializedObject.FindProperty("_accentColor").colorValue = theme.GetColor(ColorPalette.Accent);
                        _themeManagerSerializedObject.FindProperty("_baseColor").colorValue = theme.GetColor(ColorPalette.Base);
                        _themeManagerSerializedObject.FindProperty("_surfaceColor").colorValue = theme.GetColor(ColorPalette.Surface);
                        _themeManagerSerializedObject.FindProperty("_textColor").colorValue = theme.GetColor(ColorPalette.Text);
                        _themeManagerSerializedObject.FindProperty("_successColor").colorValue = theme.GetColor(ColorPalette.Success);
                        _themeManagerSerializedObject.FindProperty("_warningColor").colorValue = theme.GetColor(ColorPalette.Warning);
                        _themeManagerSerializedObject.FindProperty("_errorColor").colorValue = theme.GetColor(ColorPalette.Error);
                        _themeManagerSerializedObject.FindProperty("_infoColor").colorValue = theme.GetColor(ColorPalette.Info);

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
            UIStyles.TitleBox("World Integrated Menu", margin: false);
            EditorGUILayout.Space();

#pragma warning disable CS0162
            if (Updater.availableVpmResolver)
            {
                if (Updater.AvailableUpdate)
                {
                    EditorGUILayout.LabelField($"{EditorI18n.GetTranslation("updateAvailable")} (v{Updater.CurrentVersion} → v{Updater.LatestVersion})");
                }
                else if(Updater.LatestVersion == null)
                {
                    EditorGUILayout.LabelField(EditorI18n.GetTranslation("checkingForUpdate"));
                }
                else
                {
                    EditorGUILayout.LabelField($"{EditorI18n.GetTranslation("upToDate")} (v{Updater.CurrentVersion})");
                }

                UIStyles.UrlLabel(EditorI18n.GetTranslation("openChangelog"), "https://vpm.yukineko.dev/docs/wim-core/changelog");
                EditorGUILayout.Space();

                using (var x = new EditorGUI.ChangeCheckScope())
                {
                    Updater.UseUnstableVersion = EditorGUILayout.ToggleLeft(EditorI18n.GetTranslation("useUnstableVersion"), Updater.UseUnstableVersion);
                    if (x.changed)
                    {
                        Updater.CheckForUpdate();
                    }
                }

                if (Updater.AvailableUpdate)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button(EditorI18n.GetTranslation("update"), GUILayout.Height(32)))
                    {
                        Updater.RunUpdate();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField(EditorI18n.GetTranslation("currentVersion"), Updater.CurrentVersion);
                UIStyles.UrlLabel(EditorI18n.GetTranslation("openChangelog"), "https://vpm.yukineko.dev/docs/wim-core/changelog");
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("vpmResolverNotImported"), MessageType.Warning);
            }
#pragma warning restore CS0162

            if (ModuleVersionManager.AvailableModules)
            {
                UIStyles.TitleBox(EditorI18n.GetTranslation("modules"));
                UIStyles.UrlLabel(EditorI18n.GetTranslation("openChangelog"), "https://vpm.yukineko.dev/docs/wim-modules/changelog/");
                EditorGUILayout.Space();

                foreach (var item in ModuleVersionManager.CurrentVersions)
                {
                    var packageName = item.Key;
                    EditorGUILayout.LabelField(ModuleVersionManager.GetPackageName(packageName), EditorStyles.boldLabel);

                    if (ModuleVersionManager.HasUpdate(packageName))
                    {
                        EditorGUILayout.LabelField($"{EditorI18n.GetTranslation("updateAvailable")} (v{item.Value} → v{ModuleVersionManager.GetLatestVersion(packageName)})");
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"{EditorI18n.GetTranslation("upToDate")} (v{item.Value})");
                    }

                    EditorGUILayout.Space();
                }

                if (ModuleVersionManager.AvailableUpdate)
                {
                    if (GUILayout.Button(EditorI18n.GetTranslation("downloadLatestVersionFromBooth"), GUILayout.Height(32)))
                    {
                        Application.OpenURL("https://accounts.booth.pm/orders?q=wim&sort=order_created_at_desc");
                    }
                }
            }

            UIStyles.TitleBox(EditorI18n.GetTranslation("links"));

            UIStyles.UrlLabel(EditorI18n.GetTranslation("openDocs"), "https://vpm.yukineko.dev/docs/wim-core/intro");
            UIStyles.UrlLabel("BOOTH", "https://yukineko-works.booth.pm/");
            UIStyles.UrlLabel("GitHub", "https://github.com/yukineko-works/WorldIntegratedMenu");
        }
    }

    internal static class ResolveUrl
    {
        public static string Booth(string path)
        {
            switch(InternalEditorI18n.CurrentLanguage)
            {
                case "ja":
                    return $"https://booth.pm/ja/{path}";
                case "zh-CN":
                    return $"https://booth.pm/zh-cn/{path}";
                case "zh-TW":
                    return $"https://booth.pm/zh-tw/{path}";
                case "ko":
                    return $"https://booth.pm/ko/{path}";
                default:
                    return $"https://booth.pm/en/{path}";
            }
        }
    }

    [Serializable]
    internal class ThemePresetsRoot
    {
        public ThemePreset[] presets;
    }

    [Serializable]
    internal class ThemePreset
    {
        public string name;
        public PresetColor color;
    }

    [Serializable]
    internal class PresetColor
    {
        public string accent;
        public string bg;
        public string surface;
        public string text;
        public string success;
        public string warning;
        public string error;
        public string info;

        public Color GetColor(ColorPalette colorPalette)
        {
            switch (colorPalette)
            {
                case ColorPalette.Accent:
                    return ColorUtility.TryParseHtmlString(accent, out var accentColor) ? accentColor : Color.white;
                case ColorPalette.Base:
                    return ColorUtility.TryParseHtmlString(bg, out var bgColor) ? bgColor : Color.white;
                case ColorPalette.Surface:
                    return ColorUtility.TryParseHtmlString(surface, out var surfaceColor) ? surfaceColor : Color.white;
                case ColorPalette.Text:
                    return ColorUtility.TryParseHtmlString(text, out var textColor) ? textColor : Color.white;
                case ColorPalette.Success:
                    return ColorUtility.TryParseHtmlString(success, out var successColor) ? successColor : Color.white;
                case ColorPalette.Warning:
                    return ColorUtility.TryParseHtmlString(warning, out var warningColor) ? warningColor : Color.white;
                case ColorPalette.Error:
                    return ColorUtility.TryParseHtmlString(error, out var errorColor) ? errorColor : Color.white;
                case ColorPalette.Info:
                    return ColorUtility.TryParseHtmlString(info, out var infoColor) ? infoColor : Color.white;
                default:
                    return Color.white;
            }
        }
    }
}
