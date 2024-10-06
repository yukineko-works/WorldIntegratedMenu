
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [RequireComponent(typeof(Toggle))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ToggleSwitchHelper : UdonSharpBehaviour
    {
        [SerializeField] private Animator _animator;
        private Toggle _toggle;
        private bool _initialized;

        private void OnEnable()
        {
            if (!_initialized) return;
            _animator.SetBool("enabled", _toggle.isOn);
        }

        private void Start()
        {
            if (_animator == null) return;
            if (_toggle == null) _toggle = GetComponent<Toggle>();

            _animator.keepAnimatorStateOnDisable = true;
            _animator.SetBool("enabled", _toggle.isOn);
            _initialized = true;
        }

        public void Toggled()
        {
            if (_animator == null || _toggle == null) return;
            _animator.SetBool("enabled", _toggle.isOn);
        }
    }
}
