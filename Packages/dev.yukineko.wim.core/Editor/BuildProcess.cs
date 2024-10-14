using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace yukineko.WorldIntegratedMenu.Editor
{
    public class BuildProcess : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var version = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(BuildProcess).Assembly).version;
            var versionInfoModule = Object.FindObjectOfType<VersionInfoModule>();

            if (versionInfoModule != null)
            {
                versionInfoModule.SetProgramVariable("version", version);
            }
        }
    }
}