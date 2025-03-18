
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
    internal class FreeTextModuleInspector : ModuleInspector
    {
        protected override string I18nUUID => "c4bf8ba219fc3d14a8df85911b3973aa";
        protected override string[] ObjectProperties => new string[] { "_textComponent" };
        private Vector2 _scrollPosition = Vector2.zero;

        protected override void DrawModuleInspector()
        {
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
        }
    }
#endif
}