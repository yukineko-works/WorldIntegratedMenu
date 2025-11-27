
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

    public enum VRQuickMenuDominantHand
    {
        Right,
        Left
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QuickMenuManager : UdonSharpBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private Animator _displayController;
        [SerializeField] private QuickMenuCanvas _vrCanvas;
        [SerializeField] private QuickMenuCanvas _nonVrCanvas;
        [SerializeField] private Vector3 _vrScreenPosition = new Vector3(0f, -0.15f, 0.15f);
        [SerializeField] private float _vrQuickMenuStandardSize = 0.00016f;
        [SerializeField] private float _vrHoldTime = 0.5f;
        [SerializeField] private float _controllerInputThreshold = 0.1f;
        [SerializeField] private int _vrToggleOpenThreshold = 500;
        [SerializeField] private VRQuickMenuOpenMethod _vrOpenMethod = VRQuickMenuOpenMethod.Stick;
        [SerializeField] private VRQuickMenuDominantHand _dominantHand = VRQuickMenuDominantHand.Right;

        private VRCPlayerApi _player;
        private bool _isVR;
        private VRQuickMenuOpenMethod _defaultOpenMethod;
        private VRQuickMenuDominantHand _defaultDominantHand;
        private Vector3 _overridedVrScreenPosition;
        private Quaternion _lockedRotation = Quaternion.identity;
        private Quaternion _inversedLastHandRotation = Quaternion.identity;
        private float _holdTime = 0.0f;
        private bool _isInitialized = false;
        private bool _isShowing = false;
        private bool _isInputting = false;
        private bool _cancelPostClose = false;
        private bool _vrToggleOpen = false;
        private long _vrToggleLastInput = 0;
        private UdonSharpBehaviour[] _openMethodUpdateCallbacks = new UdonSharpBehaviour[0];

        public bool IsOpened => _holdTime >= _vrHoldTime;
        public bool IsOpening => _isInputting && !IsOpened;
        public VRQuickMenuOpenMethod DefaultOpenMethod => _defaultOpenMethod;
        public VRQuickMenuOpenMethod CurrentOpenMethod => _vrOpenMethod;
        public VRQuickMenuDominantHand DefaultDominantHand => _defaultDominantHand;
        public VRQuickMenuDominantHand DominantHand => _dominantHand;

        private QuickMenuCanvas Canvas => _isVR ? _vrCanvas : _nonVrCanvas;

        private void Start()
        {
            if (_uiManager == null || _displayController == null)
            {
                Debug.LogError("QuickMenuManager: Missing required components.");
                return;
            }
            _player = Networking.LocalPlayer;
            _isVR = _player.IsUserInVR();

            var canvas = Canvas;
            if (canvas == null || canvas.Collider == null || canvas.Raycaster == null || canvas.Panel == null || canvas.ProgressBar == null)
            {
                Debug.LogError("QuickMenuManager: Missing QuickMenuCanvas or its components.");
                return;
            }

            canvas.gameObject.SetActive(true);
            QMSetActive(false);

            transform.SetParent(transform.root.parent, false);
            SetMenuSize(1f);

            _defaultOpenMethod = _vrOpenMethod;
            _defaultDominantHand = _dominantHand;
            _overridedVrScreenPosition = _vrScreenPosition;
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

                var handTrackingData = _player.GetTrackingData(_dominantHand == VRQuickMenuDominantHand.Right ? VRCPlayerApi.TrackingDataType.RightHand : VRCPlayerApi.TrackingDataType.LeftHand);
                var qmPosition = handTrackingData.position + (handTrackingData.rotation * _overridedVrScreenPosition);

                // Stick
                if (_vrOpenMethod == VRQuickMenuOpenMethod.Stick)
                {
                    if (IsOpening)
                    {
                        _holdTime += Time.deltaTime;
                        Canvas.ProgressBar.value = _holdTime / _vrHoldTime;

                        var headTrackingData = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                        _lockedRotation = Quaternion.LookRotation(qmPosition - headTrackingData.position);
                    }
                    else if (!_isShowing)
                    {
                        _inversedLastHandRotation = Quaternion.Inverse(handTrackingData.rotation);
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
                        _inversedLastHandRotation = Quaternion.Inverse(handTrackingData.rotation);
                        ShowMenu(true);
                    }
                }

                transform.SetPositionAndRotation(qmPosition, !_isShowing ? _lockedRotation : handTrackingData.rotation * _inversedLastHandRotation * _lockedRotation);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Tab) && _player.GetPickupInHand(VRC_Pickup.PickupHand.Right) == null) ShowMenu(true);
                if (Input.GetKeyUp(KeyCode.Tab)) ShowMenu(false);
            }
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            InputStick(value, VRQuickMenuDominantHand.Right);
        }

        public override void InputMoveVertical(float value, UdonInputEventArgs args)
        {
            InputStick(value, VRQuickMenuDominantHand.Left);
        }

        private void InputStick(float value, VRQuickMenuDominantHand hand)
        {
            if (!_isVR || _vrOpenMethod != VRQuickMenuOpenMethod.Stick || _dominantHand != hand) return;
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
                _vrOpenMethod != VRQuickMenuOpenMethod.Trigger
            ) return;

            if (_dominantHand == VRQuickMenuDominantHand.Right)
            {
                if (args.handType != HandType.RIGHT || _player.GetPickupInHand(VRC_Pickup.PickupHand.Right) != null) return;
            }
            else
            {
                if (args.handType != HandType.LEFT || _player.GetPickupInHand(VRC_Pickup.PickupHand.Left) != null) return;
            }

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
            if (_isVR)
            {
                var size = _vrQuickMenuStandardSize * value;
                Canvas.transform.localScale = new Vector3(size, size, size * 100f);
            }
            else
            {
                var container = Canvas.Container;
                if (container == null)
                {
                    Debug.LogError("QuickMenuManager: Non-VR QuickMenuCanvas is missing Container reference.");
                    return;
                }

                var size = 0.5f + (value - 0.5f) * 0.5f;
                container.transform.localScale = new Vector3(size, size, size * 100f);
            }
        }

        public void SetScreenPosition(Vector3 value)
        {
            _vrScreenPosition = value;
            RecalculateVRScreenPosition();
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
            CallOpenMethodUpdateCallbacks();
        }

        public void ResetDominantHand()
        {
            SetDominantHand(_defaultDominantHand);
        }

        public void SetDominantHand(VRQuickMenuDominantHand value)
        {
            _dominantHand = value;
            RecalculateVRScreenPosition();
            CallOpenMethodUpdateCallbacks();
        }

        private void RecalculateVRScreenPosition()
        {
            if (_dominantHand == VRQuickMenuDominantHand.Right)
            {
                _overridedVrScreenPosition = _vrScreenPosition;
            }
            else
            {
                _overridedVrScreenPosition = new Vector3(_vrScreenPosition.x, -_vrScreenPosition.y, _vrScreenPosition.z);
            }

            _inversedLastHandRotation = Quaternion.Inverse(_player.GetTrackingData(_dominantHand == VRQuickMenuDominantHand.Right ? VRCPlayerApi.TrackingDataType.RightHand : VRCPlayerApi.TrackingDataType.LeftHand).rotation);
        }

        public void ShowMenu(bool show)
        {
            if (show)
            {
                _isShowing = true;
                _cancelPostClose = true;
                QMSetActive(true);
                _uiManager.SetMenuParent(Canvas.Panel.transform);
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
            Canvas.Collider.enabled = value;
            Canvas.Raycaster.enabled = value;
        }

        public void RegisterOpenMethodUpdateCallback(UdonSharpBehaviour callback)
        {
            if (callback == null) return;
            _openMethodUpdateCallbacks = ArrayUtils.Add(_openMethodUpdateCallbacks, callback);
        }

        private void CallOpenMethodUpdateCallbacks()
        {
            for (int i = 0; i < _openMethodUpdateCallbacks.Length; i++)
            {
                if (_openMethodUpdateCallbacks[i] == null) continue;
                _openMethodUpdateCallbacks[i].SendCustomEvent("OnQuickMenuOpenMethodUpdated");
            }
        }
    }
}
