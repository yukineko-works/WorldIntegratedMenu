using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using yukineko.WorldIntegratedMenu.EditorShared;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AssetListModule : UdonSharpBehaviour
    {
        [SerializeField] private GameObject _assetItemTemplate;
        [SerializeField] private Transform _assetListContent;
        [SerializeField] private string[] _assetList;

        private void Start()
        {
            if (_assetItemTemplate == null || _assetListContent == null)
            {
                Debug.LogError("AssetListModule: Missing required components.");
                return;
            }

            _assetItemTemplate.SetActive(false);

            foreach (var asset in _assetList)
            {
                var assetItem = Instantiate(_assetItemTemplate, _assetListContent);
                assetItem.SetActive(true);

                var parsedAssetText = asset.Split("<SPLIT>");
                assetItem.transform.Find("Text/Name").GetComponentInChildren<Text>().text = parsedAssetText[0];
                if (parsedAssetText.Length > 1 && !string.IsNullOrEmpty(parsedAssetText[1]))
                {
                    assetItem.transform.Find("Text/Link").GetComponentInChildren<Text>().text = parsedAssetText[1];
                }
                else
                {
                    Destroy(assetItem.transform.Find("Text/Link").gameObject);
                }
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(AssetListModule))]
    internal class AssetListModuleInspector : Editor
    {
        private InternalEditorI18n _i18n;
        private bool _showObjectProperties = false;
        private ReorderableList _reorderableList;

        private void OnEnable()
        {
            _i18n = new InternalEditorI18n("abc798ea58083ae4d9834dc8fcf94586");
        }

        private void GenerateList()
        {
            var prop = serializedObject.FindProperty("_assetList");
            _reorderableList = new ReorderableList(serializedObject, prop, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, _i18n.GetTranslation("assetList")),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = prop.GetArrayElementAtIndex(index);
                    var item = element.stringValue.Split("<SPLIT>");
                    if (item.Length < 2) item = new string[] { item[0], "" };

                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += EditorGUIUtility.standardVerticalSpacing;
                    item[0] = EditorGUI.TextField(rect, _i18n.GetTranslation("name"), item[0]);
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    item[1] = EditorGUI.TextField(rect, _i18n.GetTranslation("url"), item[1]);

                    element.stringValue = string.Join("<SPLIT>", item);
                },
                drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
                {
                    if (index % 2 == 0)
                    {
                        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.2f));
                    }
                },
                elementHeight = (EditorGUIUtility.singleLineHeight * 2) + (EditorGUIUtility.standardVerticalSpacing * 3),
                onAddCallback = list =>
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = "<SPLIT>";
                },
                onRemoveCallback = list =>
                {
                    if (Event.current.shift || EditorUtility.DisplayDialog(EditorI18n.GetTranslation("warning"), EditorI18n.GetTranslation("beforeDelete"), EditorI18n.GetTranslation("delete"), EditorI18n.GetTranslation("cancel")))
                    {
                        prop.DeleteArrayElementAtIndex(list.index);
                    }
                },
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_assetItemTemplate"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_assetListContent"));
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
