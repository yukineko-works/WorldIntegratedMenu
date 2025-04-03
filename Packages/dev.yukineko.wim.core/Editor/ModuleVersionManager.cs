using System;
using UnityEngine;
using System.Net.Http;
using System.Collections.Generic;
using VRC.SDK3.Data;
using UnityEditor.PackageManager;
using System.Threading.Tasks;
using System.Linq;

namespace yukineko.WorldIntegratedMenu.Editor
{
    public static class ModuleVersionManager
    {
        private const string _listingUrl = "https://vpm.yukineko.dev/latest-version.json";
        private static readonly Dictionary<string, SemanticVersioning.Version> _latestVersions = new Dictionary<string, SemanticVersioning.Version>();
        private static readonly Dictionary<string, SemanticVersioning.Version> _currentVersions = new Dictionary<string, SemanticVersioning.Version>();
        private static readonly List<string> _updateAvailableModules = new List<string>();

        private static readonly Dictionary<string, string> _packageNameCache = new Dictionary<string, string>();

        public static Dictionary<string, SemanticVersioning.Version> LatestVersions => _latestVersions;
        public static Dictionary<string, SemanticVersioning.Version> CurrentVersions => _currentVersions;
        public static List<string> UpdateAvailableModules => _updateAvailableModules;
        public static bool AvailableUpdate => _updateAvailableModules.Count > 0;
        public static bool AvailableModules => _currentVersions.Count > 0;

        static ModuleVersionManager()
        {
            CheckLatestVersions();
        }

        private static async void CheckLatestVersions()
        {
            #region Fetch latest versions

            using var client = new HttpClient();
            var result = string.Empty;

            try
            {
                result = await client.GetStringAsync(_listingUrl).ContinueWith(task =>
                {
                    if (!task.IsCompletedSuccessfully) throw task.Exception;
                    return task.Result;
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModuleVersionManager] Request Failed: {e.Message}");
            }

            if (!VRCJson.TryDeserializeFromJson(result, out var listing) && listing.TokenType != TokenType.DataDictionary)
            {
                Debug.LogError($"[ModuleVersionManager] Failed to parse JSON: {result}");
                return;
            }

            var self = PackageInfo.FindForAssembly(typeof(ModuleVersionManager).Assembly);
            if (!listing.DataDictionary.TryGetValue(self.name, TokenType.DataDictionary, out var modules))
            {
                Debug.LogError($"[ModuleVersionManager] Failed to get module list: {self.name}");
                return;
            }

            _latestVersions.Clear();
            foreach (var module in modules.DataDictionary)
            {
                if (module.Key.TokenType != TokenType.String || module.Value.TokenType != TokenType.String)
                {
                    Debug.LogError($"[ModuleVersionManager] Invalid module format: {module.Key}");
                    continue;
                }

                _latestVersions[module.Key.String] = new SemanticVersioning.Version(module.Value.String);
            }

            #endregion
            #region Get current versions

            var pack = Client.List();
            while (!pack.IsCompleted) await Task.Delay(100);

            if (pack.Status != StatusCode.Success)
            {
                Debug.LogError($"[ModuleVersionManager] Failed to get current versions: {pack.Error.message}");
                return;
            }

            _currentVersions.Clear();
            _packageNameCache.Clear();

            foreach (var moduleName in _latestVersions.Keys)
            {
                var module = pack.Result.FirstOrDefault(x => x.name == moduleName);
                if (module == null) continue;

                _currentVersions[moduleName] = new SemanticVersioning.Version(module.version);
                _packageNameCache[moduleName] = module.displayName;
            }

            #endregion
            #region Check for updates

            _updateAvailableModules.Clear();
            foreach (var module in _latestVersions.Keys)
            {
                if (_currentVersions.TryGetValue(module, out var currentVersion) && _latestVersions[module] > currentVersion)
                {
                    _updateAvailableModules.Add(module);
                }
            }
            #endregion
        }

        public static string GetPackageName(string moduleName)
        {
            if (_packageNameCache.TryGetValue(moduleName, out var packageName))
            {
                return packageName;
            }

            return moduleName;
        }

        public static bool HasUpdate(string moduleName)
        {
            return _updateAvailableModules.Contains(moduleName);
        }

        public static string GetLatestVersion(string moduleName)
        {
            if (_latestVersions.TryGetValue(moduleName, out var version))
            {
                return version.ToString();
            }

            return string.Empty;
        }
    }
}