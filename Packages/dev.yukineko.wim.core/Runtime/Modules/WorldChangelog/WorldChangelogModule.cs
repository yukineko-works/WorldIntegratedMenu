using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections.Generic;
using yukineko.WorldIntegratedMenu.EditorShared;

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
    internal class WorldChangelogModuleInspector : ModuleInspector
    {
        protected override string I18nUUID => "ff0c90b29aac9784184b3982777b0c83";
        protected override string[] ObjectProperties => new string[] { "_changelogItemTemplate", "_changelogListContent" };
        private ListDrawer _listDrawer;
        private Dictionary<int, Vector2> _scrollPosition = new Dictionary<int, Vector2>();

        protected override void OnEnable()
        {
            base.OnEnable();
            GenerateList();
        }

        private void GenerateList()
        {
            var prop = serializedObject.FindProperty("_changelogList");
            _listDrawer = new ListDrawer(prop, new ListDrawerCallbacks() {
                drawHeader = () => _i18n.GetTranslation("changelog"),
                drawElement = (rect, index, isActive, isFocused) =>
                {
                    var element = prop.GetArrayElementAtIndex(index);
                    var item = element.stringValue.Split("<SPLIT>");
                    if (item.Length < 2) item = new string[] { item[0], "" };

                    EditorGUI.LabelField(rect, _i18n.GetTranslation("title"));
                    item[0] = EditorGUI.TextField(ListDrawerUtils.AdjustRect(ref rect), item[0]);
                    EditorGUI.LabelField(ListDrawerUtils.AdjustRect(ref rect), _i18n.GetTranslation("content"));
                    ListDrawerUtils.AdjustRect(ref rect);

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
                elementHeight = index => (EditorGUIUtility.singleLineHeight * 4) + (EditorGUIUtility.standardVerticalSpacing * 3) + 100,
                onAdd = list =>
                {
                    prop.InsertArrayElementAtIndex(0);
                    prop.GetArrayElementAtIndex(0).stringValue = "New Changelog<SPLIT>";
                },
                onRemove = list => prop.DeleteArrayElementAtIndex(list.index),
                onReorder = list => _scrollPosition.Clear(),
            });
        }

        protected override void DrawModuleInspector()
        {
            EditorGUILayout.HelpBox(_i18n.GetTranslation("description"), MessageType.Info);
            EditorGUILayout.Space();
            _listDrawer.Draw();
        }
    }
#endif
}
