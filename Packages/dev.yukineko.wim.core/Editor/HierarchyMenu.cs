using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu.Editor
{
    public static class HierachyMenu
    {
        private const string _menuParent = "GameObject/World Integrated Menu/";
        private static int _priority = 100;
        private static bool _initialized = false;

        private static void AddItem(string name, string prefabGuid, string shortcut = "", bool isChecked = false, Func<bool> validate = null)
        {
            MenuHelper.AddMenuItem(_menuParent + name, shortcut, isChecked, _priority++, () => GenerateObject(prefabGuid), validate);
        }

        private static void AddSeparator(string name = null)
        {
            MenuHelper.AddSeparator(_menuParent + (name ?? Guid.NewGuid().ToString()), _priority++);
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
            ModuleRegistry.OnModuleRegistered(RebuildMenu);
        }

        private static void BuildMenu()
        {
            AddItem(EditorI18n.GetTranslation("menuItemRoot"), "39d3a2bb8ccfe2d47b24a4fffce11afb");
            AddSeparator();

            foreach (var module in ModuleRegistry.ModuleList.Values)
            {
                AddItem(EditorI18n.GetTranslation("modules") + "/" + module.GetTitle(), module.PrefabGuid);
            }

            _initialized = true;
            Update();
        }

        public static void RebuildMenu()
        {
            if (!_initialized) return;
            MenuHelper.RemoveAllMenuItems();
            BuildMenu();
        }
    }

    // https://qiita.com/Swanman/items/279b3b679f3f96a5f925
    internal static class MenuHelper
    {
        private static List<string> _registeredItems = new List<string>();

        public static void AddMenuItem(string name, string shortcut, bool isChecked, int priority, Action execute, Func<bool> validate)
        {
            _registeredItems.Add(name);
            var addMenuItemMethod = typeof(Menu).GetMethod("AddMenuItem", BindingFlags.Static | BindingFlags.NonPublic);
            addMenuItemMethod?.Invoke(null, new object[] { name, shortcut, isChecked, priority, execute, validate });
        }

        public static void AddSeparator(string name, int priority)
        {
            _registeredItems.Add(name);
            var addSeparatorMethod = typeof(Menu).GetMethod("AddSeparator", BindingFlags.Static | BindingFlags.NonPublic);
            addSeparatorMethod?.Invoke(null, new object[] { name, priority });
        }

        public static void RemoveMenuItem(string name, bool noRemoveItem = false)
        {
            if (_registeredItems.Contains(name) && !noRemoveItem)
            {
                _registeredItems.Remove(name);
            }

            var removeMenuItemMethod = typeof(Menu).GetMethod("RemoveMenuItem", BindingFlags.Static | BindingFlags.NonPublic);
            removeMenuItemMethod?.Invoke(null, new object[] { name });
        }

        public static void RemoveAllMenuItems()
        {
            foreach (var item in _registeredItems)
            {
                RemoveMenuItem(item, true);
            }

            _registeredItems.Clear();
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