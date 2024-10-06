
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ModuleManager : UdonSharpBehaviour
    {
        [SerializeField] private Transform _modulesRoot;
        [SerializeField] private ModuleMetadata[] _systemModules;
        private ModuleMetadata[] _modules;
        private DataList _availableModules = new DataList();
        private bool _isInitialized = false;

        public ModuleMetadata[] Modules => _modules;
        public bool Initialized => _isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _modules = _modulesRoot.GetComponentsInChildren<ModuleMetadata>();
            _modules = ArrayUtils.Concat(_modules, _systemModules);

            foreach (var module in _modules)
            {
                if (module == null) continue;
                if (_availableModules.Contains(module.ModuleId) && module.IsUnique)
                {
                    Debug.LogWarning($"[ModuleManager] Destroying duplicated module: {module.ModuleId}");
                    Destroy(module.gameObject);
                    continue;
                }

                _availableModules.Add(module.ModuleId);
                module.i18nManager = module.GetComponent<I18nManager>(); // cache
            }
        }

        public ModuleMetadata GetModule(string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId)) return null;

            foreach (var m in _modules)
            {
                if (m == null) continue;
                if (m.ModuleId == moduleId)
                {
                    return m;
                }
            }

            return null;
        }
    }
}
