
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [RequireComponent(typeof(Slider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SliderValueHelper : UdonSharpBehaviour
    {
        [SerializeField] private Text _valueText;
        private Slider _slider;

        private void Start()
        {
            _slider = GetComponent<Slider>();
            if (_valueText == null || _slider == null) return;
            UpdateValue();
        }

        public void UpdateValue()
        {
            if (_valueText == null || _slider == null) return;
            _valueText.text = _slider.value.ToString("P0");
        }
    }
}