
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VersionInfoModule : UdonSharpBehaviour
    {
        [SerializeField] private TextAsset _version;
        [SerializeField] private Text _versionText;

        private void Start()
        {
            if (_versionText != null && _version != null)
            {
                _versionText.text = $"Version {_version.text}";
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(VersionInfoModule))]
    public class VersionInfoModuleInspector : Editor
    {
        private InternalEditorI18n _i18n;
        private bool _showObjectProperties = false;

        private void OnEnable()
        {
            _i18n = new InternalEditorI18n("4808d31699fba654f86b406d56d0e5c7");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_i18n.GetTranslation("$title"), EditorStyles.largeLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(EditorI18n.GetTranslation("noSettings"), MessageType.Info);
            EditorGUILayout.HelpBox(_i18n.GetTranslation("warning"), MessageType.Warning);
            EditorGUILayout.Space();

            _showObjectProperties = EditorGUILayout.Foldout(_showObjectProperties, EditorI18n.GetTranslation("internalProperties"));
            if (_showObjectProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_version"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_versionText"));
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
#endif
}
