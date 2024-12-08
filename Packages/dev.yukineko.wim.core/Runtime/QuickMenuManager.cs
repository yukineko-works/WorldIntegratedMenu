
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace yukineko.WorldIntegratedMenu
{
    public enum VRQuickMenuOpenMethod
    {
        Stick,
        Trigger
    }

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
        [SerializeField] private int _vrToggleOpenThreshold = 500;
        [SerializeField] private VRQuickMenuOpenMethod _vrOpenMethod = VRQuickMenuOpenMethod.Stick;

        private VRCPlayerApi _player;
        private bool _isVR;
        private BoxCollider _collider;
        private GraphicRaycaster _raycaster;
        private VRQuickMenuOpenMethod _defaultOpenMethod;
        private Quaternion _lockedRotation = Quaternion.identity;
        private Quaternion _inversedLastHandRotation = Quaternion.identity;
        private float _holdTime = 0.0f;
        private bool _isInitialized = false;
        private bool _isShowing = false;
        private bool _isInputting = false;
        private bool _cancelPostClose = false;
        private bool _vrToggleOpen = false;
        private long _vrToggleLastInput = 0;

        public bool IsOpened => _holdTime >= _vrHoldTime;
        public bool IsOpening => _isInputting && !IsOpened;
        public VRQuickMenuOpenMethod DefaultOpenMethod => _defaultOpenMethod;
        public VRQuickMenuOpenMethod CurrentOpenMethod => _vrOpenMethod;

        private void Start()
        {
            if (_uiManager == null || _canvas == null || _displayController == null || _progressBar == null)
            {
                Debug.LogError("QuickMenuManager: Missing required components.");
                return;
            }
            _player = Networking.LocalPlayer;
            _isVR = _player.IsUserInVR();
            _collider = _canvas.GetComponent<BoxCollider>();
            _raycaster = _canvas.GetComponent<GraphicRaycaster>();
            _canvas.SetActive(true);
            QMSetActive(false);

            transform.SetParent(transform.root.parent, false);
            SetMenuSize(1f);

            _defaultOpenMethod = _vrOpenMethod;
            _isInitialized = true;
        }

        // https://udonsharp.docs.vrchat.com/events/#udon-update-events
        public override void PostLateUpdate()
        {
            if (!_isInitialized) return;

            if (_isVR)
            {
                if (_vrOpenMethod == VRQuickMenuOpenMethod.Stick && !_isInputting && !_isShowing) return;
                if (_vrOpenMethod == VRQuickMenuOpenMethod.Trigger && !_vrToggleOpen && !_isShowing) return;

                var rightHandTrackingData = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
                var qmPosition = rightHandTrackingData.position + (rightHandTrackingData.rotation * _vrScreenPosition);

                // Stick
                if (_vrOpenMethod == VRQuickMenuOpenMethod.Stick)
                {
                    if (IsOpening)
                    {
                        _holdTime += Time.deltaTime;
                        _progressBar.value = _holdTime / _vrHoldTime;

                        var headTrackingData = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                        _lockedRotation = Quaternion.LookRotation(qmPosition - headTrackingData.position);
                    }
                    else if (!_isShowing)
                    {
                        _inversedLastHandRotation = Quaternion.Inverse(rightHandTrackingData.rotation);
                        ShowMenu(true);
                    }
                }
                // Trigger
                else if (_vrOpenMethod == VRQuickMenuOpenMethod.Trigger)
                {
                    if (_vrToggleOpen && !_isShowing)
                    {
                        var headTrackingData = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                        _lockedRotation = Quaternion.LookRotation(qmPosition - headTrackingData.position);
                        _inversedLastHandRotation = Quaternion.Inverse(rightHandTrackingData.rotation);
                        ShowMenu(true);
                    }
                }

                transform.SetPositionAndRotation(qmPosition, !_isShowing ? _lockedRotation : rightHandTrackingData.rotation * _inversedLastHandRotation * _lockedRotation);
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
            if (!_isVR || _vrOpenMethod != VRQuickMenuOpenMethod.Stick) return;
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

        public override void InputUse(bool state, UdonInputEventArgs args)
        {
            if (
                !_isVR ||
                !state ||
                _vrOpenMethod != VRQuickMenuOpenMethod.Trigger ||
                args.handType != HandType.RIGHT ||
                _player.GetPickupInHand(VRC_Pickup.PickupHand.Right) != null
            ) return;

            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var diff = now - _vrToggleLastInput;
            if (0 >= diff || diff > _vrToggleOpenThreshold)
            {
                _vrToggleLastInput = now;
                return;
            }

            _vrToggleOpen = !_vrToggleOpen;
            _vrToggleLastInput = 0;

            if (_isShowing) ShowMenu(false);
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

        public void ResetOpenMethod()
        {
            SetOpenMethod(_defaultOpenMethod);
        }

        public void SetOpenMethod(VRQuickMenuOpenMethod value)
        {
            _vrToggleOpen = false;
            _displayController.SetBool("showProgress", false);
            _vrOpenMethod = value;
        }

        public void ShowMenu(bool show)
        {
            if (show)
            {
                _isShowing = true;
                _cancelPostClose = true;
                QMSetActive(true);
                _uiManager.SetMenuParent(_panel.transform);
                _displayController.SetBool("showMenu", true);
            }
            else
            {
                _cancelPostClose = false;
                QMSetActive(false);
                _displayController.SetBool("showMenu", false);
                SendCustomEventDelayedSeconds(nameof(PostClose), 0.3f);
            }
        }

        public void PostClose()
        {
            if (_cancelPostClose) return;
            _uiManager.SetMenuParent(null);
            _isShowing = false;
        }

        private void QMSetActive(bool value)
        {
            _collider.enabled = value;
            _raycaster.enabled = value;
        }
    }
}
