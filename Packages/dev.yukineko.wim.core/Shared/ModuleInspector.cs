using UdonSharp;
using UnityEditor;
using UnityEngine;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
#endif

namespace yukineko.WorldIntegratedMenu.EditorShared
{
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    public abstract class ModuleInspector : Editor
#else
    public abstract class ModuleInspector
#endif
    {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        protected InternalEditorI18n _i18n;
        protected abstract string I18nUUID { get; }
        protected virtual string[] ObjectProperties { get; }
        protected virtual string DocumentationURL { get; }

        private bool _showObjectProperties = false;
        private bool _showUdonSharpHeader = false;
        private bool _isUSharp = false;

        protected virtual void OnEnable()
        {
            _i18n = new InternalEditorI18n(I18nUUID);
            _isUSharp = target is UdonSharpBehaviour;
        }

        protected virtual void OnDisable()
        {
            _i18n = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(_i18n.GetTranslation("$title"), UIStyles.title);

                if (!string.IsNullOrEmpty(DocumentationURL))
                {
                    UIStyles.UrlLabel(EditorI18n.GetTranslation("openDocs"), DocumentationURL);
                }
            }
            EditorGUILayout.Space();

            if (_isUSharp && _showUdonSharpHeader)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                using (new EditorGUI.IndentLevelScope())
                {
                    if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target, true, false)) return;
                }
                EditorGUILayout.Space();
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(EditorI18n.GetTranslation("playModeWarning"), MessageType.Warning);
                return;
            }

            UIStyles.DrawBorder();
            EditorGUILayout.Space();

            DrawModuleInspector();

            if (ObjectProperties != null && ObjectProperties.Length > 0)
            {
                EditorGUILayout.Space();
                _showObjectProperties = EditorGUILayout.Foldout(_showObjectProperties, EditorI18n.GetTranslation("internalProperties"));
                if (_showObjectProperties)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        foreach (var prop in ObjectProperties)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
                        }

                        _showUdonSharpHeader = EditorGUILayout.Toggle("Enable U# debug", _showUdonSharpHeader);
                    }
                }
            }

            EditorGUILayout.Space();
            ApplyModifiedProperties();
        }

        protected abstract void DrawModuleInspector();

        protected virtual void ApplyModifiedProperties()
        {
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}