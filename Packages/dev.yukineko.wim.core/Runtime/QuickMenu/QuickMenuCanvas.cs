
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QuickMenuCanvas : UdonSharpBehaviour
    {
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private GraphicRaycaster _raycaster;
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _container;
        [SerializeField] private Slider _progressBar;

        public BoxCollider Collider => _collider;
        public GraphicRaycaster Raycaster => _raycaster;
        public GameObject Panel => _panel;
        public GameObject Container => _container;
        public Slider ProgressBar => _progressBar;
    }
}
