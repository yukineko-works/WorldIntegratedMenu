
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HomeAppLinkHelper : UdonSharpBehaviour
    {
        public Image icon;
        public Text title;
        public ApplyI18n titleI18n;
        public ModuleExecutor moduleExecutor;
        public GameObject permissionIconOwner;
        public GameObject permissionIconAllowedUser;
    }
}