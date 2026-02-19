
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DeselectTargetHelper : UdonSharpBehaviour
    {
        [SerializeField] private Selectable _target;
        [SerializeField] private bool _deselectOnEnable = true;

        public void OnEnable()
        {
            if (_deselectOnEnable) Deselect();
        }

        public void Deselect()
        {
            if (_target == null) return;
            _target.Select();
        }
    }
}