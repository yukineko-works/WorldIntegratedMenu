
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

    public enum ConditionType
    {
        And,
        Or,
    }

    public class PFDisableGameObject : UdonSharpBehaviour
    {
        [SerializeField] private PlatformType[] _platformType = new PlatformType[0];
        [SerializeField] private DeviceType[] _deviceType = new DeviceType[0];
        [SerializeField] private ConditionType _conditionType = ConditionType.Or;

        private void Start()
        {
            // Platform check
            var isPlatformMatch = false;
            var isDeviceMatch = false;

#if UNITY_STANDALONE_WIN
            if (ArrayUtils.Contains(_platformType, PlatformType.PC))
            {
                isPlatformMatch = true;
            }
#elif UNITY_ANDROID
            if (ArrayUtils.Contains(_platformType, PlatformType.Android))
            {
                isPlatformMatch = true;
            }
#elif UNITY_IOS
            if (ArrayUtils.Contains(_platformType, PlatformType.iOS))
            {
                isPlatformMatch = true;
            }
#else
            if (ArrayUtils.Contains(_platformType, PlatformType.Unknown))
            {
                isPlatformMatch = true;
            }
#endif

            // Device check
            if (Networking.LocalPlayer.IsUserInVR())
            {
                if (ArrayUtils.Contains(_deviceType, DeviceType.VR))
                {
                    isDeviceMatch = true;
                }
            }
            else
            {
                if (ArrayUtils.Contains(_deviceType, DeviceType.NonVR))
                {
                    isDeviceMatch = true;
                }
            }

            // Condition check
            if (_conditionType == ConditionType.And)
            {
                if (isPlatformMatch && isDeviceMatch)
                {
                    gameObject.SetActive(false);
                }
            }
            else if (_conditionType == ConditionType.Or)
            {
                if (isPlatformMatch || isDeviceMatch)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
