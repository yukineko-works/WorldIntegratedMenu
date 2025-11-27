
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ModalWindowHelper : UdonSharpBehaviour
    {
        [SerializeField] private Animator _animator;

        public Text title;
        public Text content;
        public Text closeButton;

        public void Close()
        {
            _animator.SetTrigger("Close");
            SendCustomEventDelayedSeconds(nameof(PostClose), 0.25f);
        }

        public void PostClose()
        {
            Destroy(gameObject);
        }
    }
}
