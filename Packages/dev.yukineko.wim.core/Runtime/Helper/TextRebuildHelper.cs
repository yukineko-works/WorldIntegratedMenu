
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TextRebuildHelper : UdonSharpBehaviour
    {
        private void Start()
        {
            var text = GetComponent<Text>();
            if (text != null)
            {
                text.FontTextureChanged();
            }
        }
    }
}