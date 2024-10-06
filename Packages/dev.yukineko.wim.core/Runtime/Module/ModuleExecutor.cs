
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ModuleExecutor : UdonSharpBehaviour
    {
        public ModuleMetadata module;
        public UIManager manager;

        public void Execute()
        {
            manager.UseModule(module);
        }
    }
}
