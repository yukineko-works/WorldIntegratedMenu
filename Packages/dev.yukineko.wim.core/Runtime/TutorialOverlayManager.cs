
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TutorialOverlayManager : UdonSharpBehaviour
    {
        [SerializeField] private GameObject _canvas;
        [SerializeField] private Animator _displayController;
        [SerializeField] private Slider _percentageSlider;
        [SerializeField] private ApplyI18n _tutorialTextI18n;
        [SerializeField] private QuickMenuManager _quickMenuManager;
        [SerializeField] private float _displayTime = 5.0f;
        [SerializeField] private Vector3 _desktopScreenPosition = new Vector3(0f, -0.1f, 0.3f);
        [SerializeField] private Vector3 _vrScreenPosition = new Vector3(0f, -0.05f, 0.3f);
        private readonly float _rotateSpeed = 0.05f;
        private readonly float _angleRange = 80.0f;
        private readonly float _rotateThreshold = 30.0f;
        private bool _isStopping = true;
        private bool _isDestroying = false;
        private float _currentDisplayTime;
        private VRCPlayerApi _player;
        private bool _isVR;

        private void Start()
        {
            if (_canvas == null || _displayController == null || _percentageSlider == null || _tutorialTextI18n == null)
            {
                Debug.LogError("TutorialOverlayManager: Missing required components.");
                DestroySelf();
                return;
            }

            _canvas.SetActive(true);

            _player = Networking.LocalPlayer;
            _isVR = _player.IsUserInVR();
            _currentDisplayTime = _displayTime;

            if (_isVR && _quickMenuManager != null)
            {
                switch(_quickMenuManager.CurrentOpenMethod)
                {
                    case VRQuickMenuOpenMethod.Stick:
                        _tutorialTextI18n.key = "tutorialTextInVRStick";
                        break;
                    case VRQuickMenuOpenMethod.Trigger:
                        _tutorialTextI18n.key = "tutorialTextInVRTrigger";
                        break;
                }
            }
            else
            {
                _tutorialTextI18n.key = "tutorialTextInDesktop";
            }

            _tutorialTextI18n.Apply();

            transform.SetParent(transform.root.parent);
            _displayController.SetBool("show", true);
        }

        // https://udonsharp.docs.vrchat.com/events/#udon-update-events
        public override void PostLateUpdate()
        {
            var trackingData = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            if (_isVR)
            {
                var rotation = GetRotation(trackingData.rotation);
                transform.SetPositionAndRotation(trackingData.position + (rotation * _vrScreenPosition), rotation);
            }
            else
            {
                transform.SetPositionAndRotation(trackingData.position + (trackingData.rotation * _desktopScreenPosition), trackingData.rotation);
            }

            if (_isDestroying) return;
            _currentDisplayTime -= Time.deltaTime;
            _percentageSlider.value = _currentDisplayTime / _displayTime;

            if (_currentDisplayTime <= 0)
            {
                _displayController.SetBool("show", false);
                SendCustomEventDelayedSeconds(nameof(DestroySelf), 3.0f);
                _isDestroying = true;
            }
        }

        private Quaternion GetRotation(Quaternion targetRotation)
        {
            var rotation = transform.rotation;
            var angle = Quaternion.Angle(rotation, targetRotation);

            if (angle >= _rotateThreshold)
            {
                _isStopping = false;
            }
            else if (angle < _rotateThreshold * 0.05f)
            {
                _isStopping = true;
            }

            if (!_isStopping)
            {
                if (angle > _angleRange)
                {
                    rotation = Quaternion.Lerp(rotation, targetRotation, _rotateSpeed * 4);
                }
                else
                {
                    rotation = Quaternion.Lerp(rotation, targetRotation, _rotateSpeed);
                }
            }

            return rotation;
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }
    }
}
