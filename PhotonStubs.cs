using UnityEngine;

namespace Photon.Pun
{
    public class MonoBehaviourPunCallbacks : MonoBehaviour { }
    public class PhotonView
    {
        public static PhotonView Get(object obj) => null;
    }
    public class PhotonNetwork
    {
        public static bool InRoom => false;
        public static void ConnectUsingSettings() { }
        public static void Disconnect() { }
    }
}

namespace Photon.Realtime
{
    public class Player { }
}

namespace GorillaLocomotion
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }

        public float scale = 1f;
        public float maxJumpSpeed = 6.5f;
        public float jumpMultiplier = 1f;

        // Added so the menu compiles
        public Transform leftHandTransform;
        public Transform rightHandTransform;
        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;

        public bool IsHandTouching(bool forLeftHand) => false;
    }

    // Modern name used by newer game versions
    public class GTPlayer : MonoBehaviour
    {
        public static GTPlayer Instance { get; private set; }

        public float maxJumpSpeed = 6.5f;
        public float jumpMultiplier = 1f;
        public SphereCollider headCollider;

        public bool IsHandTouching(bool isLeftHand) => false;
    }
}

// Commonly used by almost every mod
public class GorillaTagger : MonoBehaviour
{
    public static GorillaTagger Instance { get; private set; }

    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public SphereCollider headCollider;
    public Transform transform; // just in case
}

public class GorillaPlayerScoreboardLine : MonoBehaviour
{
    public GameObject reportButton;
}
