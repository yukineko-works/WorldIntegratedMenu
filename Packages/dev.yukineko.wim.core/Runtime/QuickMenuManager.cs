
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QuickMenuManager : UdonSharpBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private Animator _displayController;
        [SerializeField] private GameObject _canvas;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Vector3 _desktopScreenPosition = new Vector3(0f, 0f, 0.2f);
        [SerializeField] private Vector3 _vrScreenPosition = new Vector3(0f, -0.15f, 0.15f);
        [SerializeField] private float _vrQuickMenuStandardSize = 0.00016f;
        [SerializeField] private float _desktopQuickMenuStandardSize = 0.00025f;
        [SerializeField] private float _vrHoldTime = 0.5f;
        [SerializeField] private float _controllerInputThreshold = 0.1f;

        private VRCPlayerApi _player;
        private bool _isVR;
        private Quaternion _lockedRotation = Quaternion.identity;
        private Quaternion _inversedLastHandRotation = Quaternion.identity;
        private float _holdTime = 0.0f;
        private bool _isInitialized = false;
        private bool _isShowing = false;
        private bool _isInputting = false;
        private bool _cancelPostClose = false;

        public bool IsOpened => _holdTime >= _vrHoldTime;
        public bool IsOpening => _isInputting && !IsOpened;

        private void Start()
        {
            if (_uiManager == null || _canvas == null || _displayController == null || _progressBar == null)
            {
                Debug.LogError("QuickMenuManager: Missing required components.");
                return;
            }
            _player = Networking.LocalPlayer;
            _isVR = _player.IsUserInVR();
            _canvas.SetActive(false);

            transform.SetParent(transform.root.parent, false);
            SetMenuSize(1f);
            _isInitialized = true;
        }

        // https://udonsharp.docs.vrchat.com/events/#udon-update-events
        public override void PostLateUpdate()
        {
            if (!_isInitialized) return;

            if (_isVR)
            {
                if (!_isInputting && !_isShowing) return;
                var rightHandTrackingData = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);

                if (IsOpening)
                {
                    _holdTime += Time.deltaTime;
                    _progressBar.value = _holdTime / _vrHoldTime;

                    var headTrackingData = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    _lockedRotation = Quaternion.LookRotation(transform.position - headTrackingData.position);
                }
                else if (!_isShowing)
                {
                    _inversedLastHandRotation = Quaternion.Inverse(rightHandTrackingData.rotation);
                    ShowMenu(true);
                }

                transform.SetPositionAndRotation(rightHandTrackingData.position + (rightHandTrackingData.rotation * _vrScreenPosition), IsOpening ? _lockedRotation : rightHandTrackingData.rotation * _inversedLastHandRotation * _lockedRotation);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Tab)) ShowMenu(true);
                if (Input.GetKeyUp(KeyCode.Tab)) ShowMenu(false);

                if (!_isShowing) return;
                var trackingData = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                transform.SetPositionAndRotation(trackingData.position + (trackingData.rotation * _desktopScreenPosition), trackingData.rotation);
            }
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            if (!_isVR) return;
            if (value < (1 - _controllerInputThreshold))
            {
                _holdTime = 0.0f;
                _isInputting = false;
                _displayController.SetBool("showProgress", false);
                if (_isShowing) ShowMenu(false);
                return;
            }

            _isInputting = true;
            _displayController.SetBool("showProgress", true);
        }

        public void SetMenuSize(float value)
        {
            var size = (_isVR ? _vrQuickMenuStandardSize : _desktopQuickMenuStandardSize) * value;
            _canvas.transform.localScale = new Vector3(size, size, size * 100f);
        }

        public void SetScreenPosition(Vector3 value)
        {
            _vrScreenPosition = value;
        }

        public void ShowMenu(bool show)
        {
            if (show)
            {
                _isShowing = true;
                _cancelPostClose = true;
                _canvas.SetActive(true);
                _uiManager.SetMenuParent(_panel.transform);
                _displayController.SetBool("showMenu", true);
            }
            else
            {
                _cancelPostClose = false;
                _displayController.SetBool("showMenu", false);
                SendCustomEventDelayedSeconds(nameof(PostClose), 0.3f);
            }
        }

        public void PostClose()
        {
            if (_cancelPostClose) return;
            _uiManager.SetMenuParent(null);
            _canvas.SetActive(false);
            _isShowing = false;
        }
    }
}
