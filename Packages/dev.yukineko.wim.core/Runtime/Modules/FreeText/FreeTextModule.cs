
using System.Reflection;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using yukineko.WorldIntegratedMenu.EditorShared;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FreeTextModule : UdonSharpBehaviour
    {
        [SerializeField] private Text _textComponent;
        [SerializeField] private string _text = "Hello, World!";

        private void Start()
        {
            _textComponent.text = _text;
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(FreeTextModule))]
    internal class FreeTextModuleInspector : Editor
    {
        private InternalEditorI18n _i18n;
        private bool _showObjectProperties = false;
        private Vector2 _scrollPosition = Vector2.zero;

        private void OnEnable()
        {
            _i18n = new InternalEditorI18n("c4bf8ba219fc3d14a8df85911b3973aa");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_i18n.GetTranslation("$title"), EditorStyles.largeLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(_i18n.GetTranslation("description"), MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(_i18n.GetTranslation("text"));
            var textProperty = serializedObject.FindProperty("_text");

            var method = typeof(EditorGUI).GetMethod("ScrollableTextAreaInternal", BindingFlags.Static | BindingFlags.NonPublic);
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(200));
            object[] parameters = new object[] {
                rect,
                textProperty.stringValue,
                _scrollPosition,
                EditorStyles.textArea
            };

            object methodResult = method.Invoke(null, parameters);
            _scrollPosition = (Vector2)parameters[2];
            textProperty.stringValue = methodResult.ToString();

            EditorGUILayout.Space();
            _showObjectProperties = EditorGUILayout.Foldout(_showObjectProperties, EditorI18n.GetTranslation("internalProperties"));
            if (_showObjectProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_textComponent"));
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