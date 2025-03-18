using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using yukineko.WorldIntegratedMenu.EditorShared;

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
    internal class AssetListModuleInspector : ModuleInspector
    {
        protected override string I18nUUID => "abc798ea58083ae4d9834dc8fcf94586";
        protected override string[] ObjectProperties => new string[] { "_assetItemTemplate", "_assetListContent" };
        private ListDrawer _listDrawer;

        protected override void OnEnable()
        {
            base.OnEnable();
            GenerateList();
        }

        private void GenerateList()
        {
            var prop = serializedObject.FindProperty("_assetList");
            _listDrawer = new ListDrawer(prop, new ListDrawerCallbacks() {
                drawHeader = () => _i18n.GetTranslation("assetList"),
                drawElement = (rect, index, isActive, isFocused) =>
                {
                    var element = prop.GetArrayElementAtIndex(index);
                    var item = element.stringValue.Split("<SPLIT>");
                    if (item.Length < 2) item = new string[] { item[0], "" };

                    item[0] = EditorGUI.TextField(rect, _i18n.GetTranslation("name"), item[0]);
                    item[1] = EditorGUI.TextField(ListDrawerUtils.AdjustRect(ref rect), _i18n.GetTranslation("url"), item[1]);

                    element.stringValue = string.Join("<SPLIT>", item);
                },
                onAdd = list =>
                {
                    prop.InsertArrayElementAtIndex(prop.arraySize);
                    prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = "<SPLIT>";
                },
                onRemove = list => prop.DeleteArrayElementAtIndex(list.index),
                elementCount = index => 2
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
