using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun
{
    public class MonoBehaviourPunCallbacks : MonoBehaviour {}
    public class PhotonView 
    { 
        public static PhotonView Get(object obj) => null; 
    }
    public class PhotonNetwork 
    { 
        public static bool InRoom => false; 
        public static void ConnectUsingSettings() {} 
        public static void Disconnect() {} 
    }
}

namespace Photon.Realtime
{
    public class Player {}
}

namespace GorillaLocomotion
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }
        public float scale = 1f;
        public float maxJumpSpeed = 6.5f;
        public float jumpMultiplier = 1f;
    }
}

public class GorillaPlayerScoreboardLine : MonoBehaviour
{
    public GameObject reportButton;
}

namespace UnityEngine.XR
{
    public struct InputDevice
    {
        public bool IsPressed(object feature, out bool value) { value = false; return false; }
    }

    public static class InputDevices
    {
        public static void GetDevicesWithCharacteristics(uint desiredCharacteristics, List<InputDevice> devices) {}
    }

    public static class InputFeatureUsages
    {
        public static object primaryButton;
        public static object secondaryButton;
        public static object gripButton;
        public static object triggerButton;
    }
}
