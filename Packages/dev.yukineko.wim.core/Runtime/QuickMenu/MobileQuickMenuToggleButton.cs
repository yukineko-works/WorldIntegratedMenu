
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class MobileQuickMenuToggleButton : UdonSharpBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Sprite _openIcon;
        [SerializeField] private Sprite _closeIcon;
        [SerializeField] private QuickMenuManager _quickMenuManager;
        [SerializeField] private GameObject _canvas;

        private void Start()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            // Android or iOS以外ならDestroy
            Destroy(gameObject);
#else
            // VR環境ならDestroy
            if (Networking.LocalPlayer != null && Networking.LocalPlayer.IsUserInVR())
            {
                Destroy(gameObject);
            }
#endif

            _quickMenuManager.RegisterMenuStateChangeCallback(this);
            _canvas.SetActive(true);
        }

        public void ToggleMenu()
        {
            _quickMenuManager.ShowMenu(!_quickMenuManager.IsShowing);
        }

        public void OnQuickMenuStateChanged()
        {
            if (_quickMenuManager.IsShowing)
            {
                _icon.sprite = _closeIcon;
            }
            else
            {
                _icon.sprite = _openIcon;
            }
        }
    }
}