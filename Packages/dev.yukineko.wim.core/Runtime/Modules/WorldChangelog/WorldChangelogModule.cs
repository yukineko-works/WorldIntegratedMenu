using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections.Generic;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WorldChangelogModule : UdonSharpBehaviour
    {
        [SerializeField] private GameObject _changelogItemTemplate;
        [SerializeField] private Transform _changelogListContent;
        [SerializeField] private string[] _changelogList;

        private void Start()
        {
            if (_changelogItemTemplate == null || _changelogListContent == null)
            {
                Debug.LogError("WorldChangelogModule: Missing required components.");
                return;
            }

            _changelogItemTemplate.SetActive(false);

            foreach (var changelog in _changelogList)
            {
                var changelogItem = Instantiate(_changelogItemTemplate, _changelogListContent);
                changelogItem.SetActive(true);

                var parsedChangelogText = changelog.Split("<SPLIT>");
                changelogItem.transform.Find("Title").GetComponentInChildren<Text>().text = parsedChangelogText[0];
                if (parsedChangelogText.Length > 1)
                {
                    changelogItem.transform.Find("Text").GetComponentInChildren<Text>().text = parsedChangelogText[1];
                }
                else
                {
                    Destroy(changelogItem.transform.Find("Text").gameObject);
                }
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(WorldChangelogModule))]
    internal class WorldChangelogModuleInspector : Editor
    {
        private InternalEditorI18n _i18n;
        private bool _showObjectProperties = false;
        private ReorderableList _reorderableList;
        private Dictionary<int, Vector2> _scrollPosition = new Dictionary<int, Vector2>();

        private void OnEnable()
        {
            _i18n = new InternalEditorI18n("ff0c90b29aac9784184b3982777b0c83");
        }

        private void GenerateList()
        {
            var prop = serializedObject.FindProperty("_changelogList");
            _reorderableList = new ReorderableList(serializedObject, prop, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, _i18n.GetTranslation("changelog")),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = prop.GetArrayElementAtIndex(index);
                    var item = element.stringValue.Split("<SPLIT>");
                    if (item.Length < 2) item = new string[] { item[0], "" };

                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.LabelField(rect, _i18n.GetTranslation("title"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    item[0] = EditorGUI.TextField(rect, item[0]);
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
                    EditorGUI.LabelField(rect, _i18n.GetTranslation("content"));
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    rect.height = 100;

                    var method = typeof(EditorGUI).GetMethod("ScrollableTextAreaInternal", BindingFlags.Static | BindingFlags.NonPublic);
                    object[] parameters = new object[] {
                        rect,
                        item[1],
                        _scrollPosition.GetValueOrDefault(index, Vector2.zero),
                        EditorStyles.textArea
                    };

                    object methodResult = method.Invoke(null, parameters);
                    _scrollPosition[index] = (Vector2)parameters[2];
                    item[1] = methodResult.ToString();

                    element.stringValue = string.Join("<SPLIT>", item);
                },
                elementHeight = (EditorGUIUtility.singleLineHeight * 4) + (EditorGUIUtility.standardVerticalSpacing * 3) + 100,
                onAddCallback = list =>
                {
                    prop.InsertArrayElementAtIndex(0);
                    prop.GetArrayElementAtIndex(0).stringValue = "New Changelog<SPLIT>";
                },
                onRemoveCallback = list =>
                {
                    if (EditorUtility.DisplayDialog(EditorI18n.GetTranslation("warning"), EditorI18n.GetTranslation("beforeDelete"), EditorI18n.GetTranslation("delete"), EditorI18n.GetTranslation("cancel")))
                    {
                        prop.DeleteArrayElementAtIndex(list.index);
                    }
                },
                onReorderCallback = list =>
                {
                    _scrollPosition.Clear();
                }
            };
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
            if (_reorderableList == null) GenerateList();
            _reorderableList.DoLayoutList();

            EditorGUILayout.Space();
            _showObjectProperties = EditorGUILayout.Foldout(_showObjectProperties, EditorI18n.GetTranslation("internalProperties"));
            if (_showObjectProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_changelogItemTemplate"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_changelogListContent"));
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
