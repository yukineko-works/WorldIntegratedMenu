
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace yukineko.WorldIntegratedMenu
{
    public enum PlatformType
    {
        Unknown,
        PC,
        Android,
        iOS,
    }

    public enum DeviceType
    {
        VR,
        NonVR,
    }

    public class PFDisableGameObject : UdonSharpBehaviour
    {
        [SerializeField] private PlatformType[] _platformType = new PlatformType[0];
        [SerializeField] private DeviceType[] _deviceType = new DeviceType[0];

        private void Start()
        {
            // Platform check
#if UNITY_STANDALONE_WIN
            if (ArrayUtils.Contains(_platformType, PlatformType.PC))
            {
                gameObject.SetActive(false);
                return;
            }
#elif UNITY_ANDROID
            if (ArrayUtils.Contains(_platformType, PlatformType.Android))
            {
                gameObject.SetActive(false);
                return;
            }
#elif UNITY_IOS
            if (ArrayUtils.Contains(_platformType, PlatformType.iOS))
            {
                gameObject.SetActive(false);
                return;
            }
#else
            if (ArrayUtils.Contains(_platformType, PlatformType.Unknown))
            {
                gameObject.SetActive(false);
                return;
            }
#endif

            // Device check
            if (Networking.LocalPlayer.IsUserInVR())
            {
                if (ArrayUtils.Contains(_deviceType, DeviceType.VR))
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                if (ArrayUtils.Contains(_deviceType, DeviceType.NonVR))
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
