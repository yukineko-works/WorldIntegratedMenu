using UnityEditor;

namespace yukineko.WorldIntegratedMenu.Editor
{
    [InitializeOnLoad]
    public static class AssemblyReloadHandler
    {
        private static bool _reloading = false;
        public static bool Reloading => _reloading;

        static AssemblyReloadHandler()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () => _reloading = true;
            AssemblyReloadEvents.afterAssemblyReload += () => _reloading = false;
        }
    }
}