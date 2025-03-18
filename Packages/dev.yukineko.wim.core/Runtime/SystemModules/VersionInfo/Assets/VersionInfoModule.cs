
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VersionInfoModule : UdonSharpBehaviour
    {
        public string version = "0.0.0";
        [SerializeField] private Text _versionText;

        private void Start()
        {
            if (_versionText != null)
            {
                _versionText.text = $"Version {version}";
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(VersionInfoModule))]
    public class VersionInfoModuleInspector : ModuleInspector
    {
        protected override string I18nUUID => "4808d31699fba654f86b406d56d0e5c7";
        protected override string[] ObjectProperties => new string[] { "version", "_versionText" };

        protected override void DrawModuleInspector()
        {
            EditorGUILayout.HelpBox(EditorI18n.GetTranslation("noSettings"), MessageType.Info);
            EditorGUILayout.HelpBox(_i18n.GetTranslation("warning"), MessageType.Warning);
        }
    }
#endif
}
