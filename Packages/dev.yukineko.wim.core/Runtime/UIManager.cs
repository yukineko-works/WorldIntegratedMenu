
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UIManager : UdonSharpBehaviour
    {
        [SerializeField] private GameObject[] _canvas;
        [SerializeField] private Transform _rootCanvas;
        [SerializeField] private Transform _panel;
        [SerializeField] private GameObject _usingMessagePanel;
        [SerializeField] private Transform _moduleContentContainer;
        [SerializeField] private Animator _moduleContentAnimator;
        [SerializeField] private Animator _navigationMenuAnimator;
        [SerializeField] private Text _currentDate;
        [SerializeField] private Text _currentTime;
        [SerializeField] private Text _homeWelcomeText;
        [SerializeField] private ModuleManager _moduleManager;
        [SerializeField] private ThemeManager _themeManager;
        [SerializeField] private I18nManager _i18nManager;
        [SerializeField] private GameObject _titleTemplateA;
        [SerializeField] private GameObject _titleTemplateB;
        [SerializeField] private GameObject _linkTemplate;
        [SerializeField] private Transform _linkContainer;
        [SerializeField] private GameObject _navigationButtonTemplate;
        [SerializeField] private Transform _navigationButtonContainer;

        private ModuleMetadata _currentModule;
        private ModuleMetadata _nextModule;

        private bool _titleTemplateASide = false;
        private Animator _titleTemplateAAnimator;
        private Animator _titleTemplateBAnimator;

        public GameObject[] Canvas
        {
            get
            {
                while (true)
                {
                    var nullCanvasIndex = -1;
                    for (int i = 0; i < _canvas.Length; i++)
                    {
                        if (_canvas[i] == null)
                        {
                            nullCanvasIndex = i;
                            break;
                        }
                    }

                    if (nullCanvasIndex == -1) break;
                    _canvas = ArrayUtils.RemoveAt(_canvas, nullCanvasIndex);
                }

                return _canvas;
            }
        }

        public ThemeManager ThemeManager => _themeManager;

        private void Start()
        {
            UpdateCurrentDateTime();

            if (_moduleManager != null && !_moduleManager.Initialized)
            {
                _moduleManager.Initialize();
            }

            foreach (var module in _moduleManager.Modules)
            {
                if (module == null)
                {
                    Debug.LogWarning("Module is null");
                    continue;
                }

                module.RegenerateUuid();

                if (!module.HideInMenu)
                {
                    var link = Instantiate(_linkTemplate, _linkContainer);
                    link.name = module.Uuid;
                    link.SetActive(true);
                    link.transform.Find("Icon").GetComponent<Image>().sprite = module.moduleIcon;

                    var linkExecutor = link.GetComponent<ModuleExecutor>();
                    linkExecutor.manager = this;
                    linkExecutor.module = module;

                    var navigationButton = Instantiate(_navigationButtonTemplate, _navigationButtonContainer);
                    navigationButton.name = module.Uuid;
                    navigationButton.SetActive(true);
                    navigationButton.transform.Find("Icon").GetComponent<Image>().sprite = module.moduleIcon;
                    navigationButton.transform.Find("Active/Icon").GetComponent<Image>().sprite = module.moduleIcon;
                    var navigationButtonExecutor = navigationButton.GetComponent<ModuleExecutor>();
                    navigationButtonExecutor.manager = this;
                    navigationButtonExecutor.module = module;

                    if (!module.forceUseModuleName && module.i18nManager != null)
                    {
                        if (!module.i18nManager.Initialized) module.i18nManager.BuildLocalization();
                        if (module.i18nManager.HasLocalization)
                        {
                            link.transform.Find("Title").GetComponent<ApplyI18n>().manager = module.i18nManager;
                            navigationButton.transform.Find("Title").GetComponent<ApplyI18n>().manager = module.i18nManager;
                        }
                        else
                        {
                            link.transform.Find("Title").GetComponent<Text>().text = module.moduleName;
                            navigationButton.transform.Find("Title").GetComponent<Text>().text = module.moduleName;
                        }
                    }
                    else
                    {
                        link.transform.Find("Title").GetComponent<Text>().text = module.moduleName;
                        navigationButton.transform.Find("Title").GetComponent<Text>().text = module.moduleName;
                    }
                }

                module.content.name = module.Uuid;
                module.content.transform.SetParent(_moduleContentContainer, false);
                module.content.SetActive(false);
            }

            _linkTemplate.SetActive(false);
            _navigationButtonTemplate.SetActive(false);
            _usingMessagePanel.SetActive(false);
            SetTitle();

            _themeManager.ApplyTheme();
            _i18nManager.ApplyI18n();
        }

        public void UseModule(ModuleMetadata module)
        {
            if (_currentModule != null && _currentModule.Uuid == module.Uuid) return;

            Debug.Log("UsingModule: " + module.moduleName);
            _navigationMenuAnimator.SetBool("isShow", true);

            if (_currentModule != null)
            {
                SetBottomNavigationButtonSelected(_currentModule.Uuid, false);
            }

            SetBottomNavigationButtonSelected(module.Uuid, true);
            UpdateTitle(module);

            module.SendCustomEvent("OnModuleCalled");

            _nextModule = module;
            _moduleContentAnimator.SetBool("show", false);
            SendCustomEventDelayedSeconds(nameof(UpdateContent), 0.15f);
        }

        public void UpdateContent()
        {
            if (_currentModule != null)
            {
                _currentModule.content.SetActive(false);
            }

            if (_nextModule == null)
            {
                _currentModule = null;
                return;
            }

            _currentModule = _nextModule;
            _currentModule.content.SetActive(true);
            _nextModule = null;

            _moduleContentAnimator.SetBool("show", true);
        }

        public void CloseModuleMenu()
        {
            _moduleContentAnimator.SetBool("show", false);
            _navigationMenuAnimator.SetBool("isShow", false);

            if (_currentModule != null)
            {
                SetBottomNavigationButtonSelected(_currentModule.Uuid, false);
            }

            SendCustomEventDelayedSeconds(nameof(UpdateContent), 0.15f);
            SetTitle();
        }

        public void UpdateCurrentDateTime()
        {
            var now = DateTime.Now;
            _currentDate.text = now.ToString("d", _i18nManager.CurrentCulture);
            _currentTime.text = now.ToString("t", _i18nManager.CurrentCulture);

            var nextUpdate = 60 - DateTime.Now.Second;
            SendCustomEventDelayedSeconds(nameof(UpdateCurrentDateTime), nextUpdate);
        }

        private void SetTitle(string title = null)
        {
            var titleTemplate = _titleTemplateASide ? _titleTemplateA : _titleTemplateB;
            titleTemplate.GetComponent<Text>().text = title ?? _i18nManager.GetTranslation("home");

            if (_titleTemplateAAnimator == null)
            {
                _titleTemplateAAnimator = _titleTemplateA.GetComponent<Animator>();
            }

            if (_titleTemplateBAnimator == null)
            {
                _titleTemplateBAnimator = _titleTemplateB.GetComponent<Animator>();
            }


            _titleTemplateAAnimator.SetBool("show", _titleTemplateASide);
            _titleTemplateBAnimator.SetBool("show", !_titleTemplateASide);

            _titleTemplateASide = !_titleTemplateASide;
        }

        public void UpdateTitle(ModuleMetadata targetModule = null)
        {
            var module = targetModule ?? _currentModule;
            if (module == null)
            {
                SetTitle();
                return;
            }

            if (!module.forceUseModuleName && module.i18nManager != null && module.i18nManager.HasLocalization && module.i18nManager.Initialized)
            {
                SetTitle(module.i18nManager.GetTranslation("$moduleName", _i18nManager.CurrentLanguage));
            }
            else
            {
                SetTitle(module.moduleName);
            }
        }

        private void SetBottomNavigationButtonSelected(string uuid, bool selected)
        {
            var targetModule = _navigationButtonContainer.Find(uuid);
            if (targetModule == null) return;
            targetModule.GetComponent<Animator>().SetBool("selected", selected);
        }

        public void SetMenuParent(Transform parent)
        {
            _usingMessagePanel.SetActive(parent != null && parent != _rootCanvas);
            _panel.SetParent(parent == null ? _rootCanvas : parent, false);
        }

        public void SetMenuParent()
        {
            SetMenuParent(null);
        }

        public void OpenCloudSyncModule()
        {
            var module = _moduleManager.GetModule("CloudSyncModule");
            if (module != null)
            {
                UseModule(module);
            }
        }
    }
}
