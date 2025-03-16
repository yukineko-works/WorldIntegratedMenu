using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using yukineko.WorldIntegratedMenu.EditorShared;

#if USE_VPM_RESOLVER
using VRC.PackageManagement.Core;
using VRC.PackageManagement.Core.Types;
using VRC.PackageManagement.Resolver;
#endif

namespace yukineko.WorldIntegratedMenu.Editor
{
    public static class Updater
    {
        private static readonly bool _availableUpdate = false;

#if USE_VPM_RESOLVER
        private static readonly SemanticVersioning.Version _latestVersion;
        public const bool availableVpmResolver = true;
#else
        private static readonly string _latestVersion;
        public const bool availableVpmResolver = false;
#endif

        private static readonly UnityEditor.PackageManager.PackageInfo _packageInfo;

        public static bool AvailableUpdate => _availableUpdate;
        public static string LatestVersion => _latestVersion?.ToString();
        public static string CurrentVersion => _packageInfo.version;

        public static bool UseUnstableVersion
        {
            get => EditorPrefs.GetBool("WIM_UseUnstableVersion", false);
            set => EditorPrefs.SetBool("WIM_UseUnstableVersion", value);
        }

        static Updater()
        {
            _packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(CoreMenu).Assembly);

#if USE_VPM_RESOLVER
            var includeUnstableVersion = UseUnstableVersion;
            var availableVersions = Resolver.GetAllVersionsOf(_packageInfo.name)
                .Select(x => new SemanticVersioning.Version(x))
                .Where(x => x != null && (includeUnstableVersion || !x.IsPreRelease));

            _latestVersion = availableVersions.OrderByDescending(x => x).First();
            if (_latestVersion != null)
            {
                _availableUpdate = _latestVersion > new SemanticVersioning.Version(_packageInfo.version);
            }
#endif
        }

        public static void RunUpdate()
        {
            if (!_availableUpdate || _latestVersion == null || EditorApplication.isPlaying || EditorApplication.isCompiling)
            {
                Debug.LogError("[WIM Updater] Update was not executed. There is no update or it is in Play mode or compiling.");
                return;
            }

#if USE_VPM_RESOLVER
            var package = Repos.GetPackageWithVersionMatch(_packageInfo.name, LatestVersion);
            var affectedPackages = Resolver.GetAffectedPackageList(package);

            if (affectedPackages.Count > 0 && !EditorUtility.DisplayDialog("WIM Updater", $"{EditorI18n.GetTranslation("updaterPackageAffectWarn")}\n\n{string.Join("\n", affectedPackages)}", "Yes", "No"))
            {
                return;
            }

            EditorApplication.delayCall += () => {
                Resolver.ForceRefresh();
                try
                {
                    new UnityProject(Resolver.ProjectDir).UpdateVPMPackage(package);
                    EditorUtility.DisplayDialog("WIM Updater", EditorI18n.GetTranslation("updateSuccessfull"), "OK");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WIM Updater] An error occurred while updating the package: {e.Message}");
                    EditorUtility.DisplayDialog("WIM Updater", "Update failed", "OK");
                }
                Resolver.ForceRefresh();
            };
#endif
        }
    }
}