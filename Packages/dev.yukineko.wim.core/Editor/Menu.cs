using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace yukineko.WorldIntegratedMenu.EditorMenu
{
    public static class MenuUtils
    {
        private const string _menuParent = "GameObject/World Integrated Menu/";
        private static int _priority = 100;
        private static bool _initialized = false;

        private static Dictionary<string, string> _itemQueue = new Dictionary<string, string>();

        public static void TryAddModuleItem(string name, string prefabGuid)
        {
            name = EditorI18n.GetTranslation("modules") + "/" + name;

            if (_initialized)
            {
                AddItem(name, prefabGuid);
                Update();
            }
            else
            {
                _itemQueue[name] = prefabGuid;
            }
        }

        private static void AddItem(string name, string prefabGuid, string shortcut = "", bool isChecked = false, Func<bool> validate = null)
        {
            MenuHelper.AddMenuItem(_menuParent + name, shortcut, isChecked, _priority++, () => GenerateObject(prefabGuid), validate);
        }

        private static void AddSeparator(string name = "")
        {
            MenuHelper.AddSeparator(_menuParent + name, _priority++);
        }

        public static void RemoveItem(string name)
        {
            MenuHelper.RemoveMenuItem(_menuParent + name);
        }

        public static void Update()
        {
            MenuHelper.Update();
        }

        private static void GenerateObject(string guid)
        {
            var item = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null) return;

            var prefab = PrefabUtility.InstantiatePrefab(item, Selection.activeTransform);
            if (prefab == null) return;

            Selection.activeGameObject = prefab as GameObject;
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () => BuildMenu();
        }

        private static void BuildMenu()
        {
            AddItem(EditorI18n.GetTranslation("menuItemRoot"), ""); // TODO: Add prefab GUID
            AddSeparator();
            AddItem(EditorI18n.GetTranslation("modules") + "/" + EditorI18n.GetTranslation("assetList"), ""); // TODO: Add prefab GUID
            AddItem(EditorI18n.GetTranslation("modules") + "/" + EditorI18n.GetTranslation("worldChangelog"), ""); // TODO: Add prefab GUID
            AddItem(EditorI18n.GetTranslation("modules") + "/" + EditorI18n.GetTranslation("freeText"), ""); // TODO: Add prefab GUID

            foreach (var item in _itemQueue)
            {
                AddItem(item.Key, item.Value);
            }

            _initialized = true;
            _itemQueue.Clear();

            Update();
        }
    }

    // https://qiita.com/Swanman/items/279b3b679f3f96a5f925
    internal static class MenuHelper
    {
        public static void AddMenuItem(string name, string shortcut, bool isChecked, int priority, Action execute, Func<bool> validate)
        {
            var addMenuItemMethod = typeof(Menu).GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.NonPublic);
            addMenuItemMethod?.Invoke(null, new object[] { name, shortcut, isChecked, priority, execute, validate });
        }

        public static void AddSeparator(string name, int priority)
        {
            var addSeparatorMethod = typeof(Menu).GetMethod("AddSeparator", BindingFlags.Static | BindingFlags.NonPublic);
            addSeparatorMethod?.Invoke(null, new object[] { name, priority });
        }

        public static void RemoveMenuItem(string name)
        {
            var removeMenuItemMethod = typeof(Menu).GetMethod("RemoveMenuItem", BindingFlags.Static | BindingFlags.NonPublic);
            removeMenuItemMethod?.Invoke(null, new object[] { name });
        }

        public static void Update()
        {
            var internalUpdateAllMenus = typeof(EditorUtility).GetMethod("Internal_UpdateAllMenus", BindingFlags.Static | BindingFlags.NonPublic);
            internalUpdateAllMenus?.Invoke(null, null);

            var shortcutIntegrationType = Type.GetType("UnityEditor.ShortcutManagement.ShortcutIntegration, UnityEditor.CoreModule");
            var instanceProp = shortcutIntegrationType?.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
            var instance = instanceProp?.GetValue(null);
            var rebuildShortcutsMethod = instance?.GetType().GetMethod("RebuildShortcuts", BindingFlags.Instance | BindingFlags.NonPublic);
            rebuildShortcutsMethod?.Invoke(instance, null);
        }
    }
}