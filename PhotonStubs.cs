namespace Photon.Pun
{
    public class MonoBehaviourPunCallbacks {}
    public class PhotonView { public static PhotonView Get(object obj) => null; }
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
    public class Player
    {
        public static Player Instance { get; } = new Player();
        public float scale = 1f;
    }
}

namespace UnityEngine.XR
{
    public struct InputDevice {}
    public static class InputDevices
    {
        public static void GetDevices(System.Collections.Generic.List<InputDevice> devices) {}
    }
}

public class GorillaPlayerScoreboardLine {}
