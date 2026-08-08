using BepInEx;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TvMenu
{
    [BepInPlugin("org.tv.gorillatag.tvmenu", "TvMenu Beta", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool colorBlueEnabled = true;
        public static int currentCategory = 0;

        public static float speedBoostMultiplier = 1.75f;
        public static float flySpeed = 14f;
        public static float armLengthMultiplier = 1.4f;
        public static float lowGravityMultiplier = 0.35f;

        // Mod states
        public static bool[] movementStates = new bool[13];
        public static bool[] visualStates = new bool[10];
        public static bool[] gunStates = new bool[6];
        public static bool[] miscSafetyStates = new bool[12];

        // Mod definitions featuring Working [W] and Not Working [NW] items
        public static string[] movementMods = {
            "Speed Boost [W]", "Fly [W]", "Trigger Fly [W]", "Joystick Fly [W]", "WASD Fly [W]",
            "Long Arms [W]", "Air Jump [W]", "Low Gravity [W]", "Bunny Hop [W]", "Fast Slide [W]",
            "Zero Friction [W]", "Platform Balls [W]", "Noclip [NW]"
        };

        public static string[] visualMods = {
            "Player ESP [W]", "Fullbright [W]", "FPS Counter [W]", "Name Tags [W]",
            "FOV Changer [W]", "Ghost Mode [W]", "Chams [NW]", "Bone ESP [NW]", "Third Person [NW]", "Custom Skybox [NW]"
        };

        public static string[] gunMods = {
            "Kick Gun [W]", "Lag Gun [NW]", "Tag Gun [W]", "Auto Tag [NW]", "Soundboard Spam [NW]", "Invisibility [W]"
        };

        public static string[] miscSafetyMods = {
            "Anti-Report [W]", "Head Spin [W]", "Speedometer [W]", "Bouncing Surfaces [W]",
            "Sticky Hands [NW]", "Fast Load [W]", "FPS Booster [W]", "Vibration Control [W]",
            "Position Logger [W]", "Config Save [W]", "Auto Report Deter [NW]", "Disconnect Protect [W]"
        };

        private MenuStub menuStub;
        private bool yWasPressed = false;

        private float blueThemeTimer = 0f;
        private Vector3 originalGravity;
        private bool gravityModified = false;

        private GameObject leftBall;
        private GameObject rightBall;
        private float ballLifetime = 0.45f;
        private float leftBallTimer = 0f;
        private float rightBallTimer = 0f;

        private void Awake()
        {
            originalGravity = Physics.gravity;
            menuStub = new MenuStub();
            Debug.Log("[TvMenu] TvMenu Beta 1.0.0 initialized with MenuStub.");
        }

        private void Update()
        {
            HandleYToggle();
            if (menuStub.IsOpen())
            {
                menuStub.UpdatePosition(GetLeftHand());
            }
            RunMods();
            HandleBlueTheme();
            HandlePlatformBalls();
        }

        private void HandleYToggle()
        {
            bool yPressed = false;
            try
            {
                var devices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
                if (devices.Count > 0)
                    devices[0].TryGetFeatureValue(CommonUsages.secondaryButton, out yPressed);
            }
            catch { }

            if (yPressed && !yWasPressed)
            {
                if (menuStub.IsOpen())
                {
                    menuStub.DestroyMenu();
                }
                else
                {
                    menuStub.BuildMenu(
                        cat => { currentCategory = cat; menuStub.RefreshMods(OnModToggled); },
                        idx => OnModToggled(idx),
                        () => { colorBlueEnabled = !colorBlueEnabled; menuStub.BuildMenu(cat => { currentCategory = cat; menuStub.RefreshMods(OnModToggled); }, idx => OnModToggled(idx), () => {}); }
                    );
                }
            }

            yWasPressed = yPressed;
        }

        private void OnModToggled(int index)
        {
            if (currentCategory == 0) movementStates[index] = !movementStates[index];
            else if (currentCategory == 1) visualStates[index] = !visualStates[index];
            else if (currentCategory == 2) gunStates[index] = !gunStates[index];
            else if (currentCategory == 3) miscSafetyStates[index] = !miscSafetyStates[index];

            menuStub.RefreshMods(OnModToggled);
        }

        private Transform GetLeftHand()
        {
            try
            {
                if (GorillaTagger.Instance != null && GorillaTagger.Instance.leftHandTransform != null)
                    return GorillaTagger.Instance.leftHandTransform;
            }
            catch { }
            return null;
        }

        private Transform GetHead()
        {
            try
            {
                if (GorillaTagger.Instance != null && GorillaTagger.Instance.headCollider != null)
                    return GorillaTagger.Instance.headCollider.transform;
            }
            catch { }
            return Camera.main != null ? Camera.main.transform : null;
        }

        private void HandleBlueTheme()
        {
            if (!colorBlueEnabled) return;

            blueThemeTimer += Time.deltaTime;
            if (blueThemeTimer < 1.2f) return;
            blueThemeTimer = 0f;

            try
            {
                Color blue = new Color(0.05f, 0.35f, 0.95f, 1f);
                Color emission = new Color(0.1f, 0.5f, 1.2f);

                string[] keywords = { "board", "scoreboard", "leaderboard", "computer", "terminal", "screen", "sign", "monitor", "display", "console" };

                foreach (Renderer r in UnityEngine.Object.FindObjectsOfType<Renderer>())
                {
                    if (r == null) continue;
                    string name = r.gameObject.name.ToLower();
                    string rootName = r.transform.root != null ? r.transform.root.name.ToLower() : "";

                    bool shouldColor = false;
                    foreach (string key in keywords)
                    {
                        if (name.Contains(key) || rootName.Contains(key)) { shouldColor = true; break; }
                    }

                    if (!shouldColor) continue;

                    if (r.material != null)
                    {
                        r.material.color = blue;
                        if (r.material.HasProperty("_EmissionColor"))
                        {
                            r.material.EnableKeyword("_EMISSION");
                            r.material.SetColor("_EmissionColor", emission);
                        }
                    }
                }

                // Force MOTD override with white text
                foreach (Text txt in UnityEngine.Object.FindObjectsOfType<Text>())
                {
                    if (txt != null && (txt.text.Contains("BANANA BEARD") || txt.text.Contains("MESSAGE OF THE DAY")))
                    {
                        txt.text = "Hey, Welcome to TvMenu go crazy!";
                        txt.color = Color.white;
                    }
                }
            }
            catch { }
        }

        private GameObject CreateBall(Vector3 pos)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "TvMenu_PlatformBall";
            ball.transform.position = pos;
            ball.transform.localScale = Vector3.one * 0.28f;

            var rend = ball.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.1f, 0.55f, 1f, 0.92f);
                if (rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", new Color(0.2f, 0.7f, 1f) * 1.4f);
                }
            }

            var rb = ball.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return ball;
        }

        private void HandlePlatformBalls()
        {
            if (!movementStates[11])
            {
                if (leftBall != null) { Destroy(leftBall); leftBall = null; }
                if (rightBall != null) { Destroy(rightBall); rightBall = null; }
                return;
            }

            bool leftGrip = false, rightGrip = false;
            try
            {
                var lDevices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, lDevices);
                if (lDevices.Count > 0) lDevices[0].TryGetFeatureValue(CommonUsages.gripButton, out leftGrip);

                var rDevices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rDevices);
                if (rDevices.Count > 0) rDevices[0].TryGetFeatureValue(CommonUsages.gripButton, out rightGrip);
            }
            catch { }

            Transform left = GetLeftHand();
            Transform right = GetHead(); // fallback reference

            if (leftGrip && left != null)
            {
                Vector3 pos = left.position - Vector3.up * 0.08f;
                if (leftBall == null) leftBall = CreateBall(pos);
                else leftBall.transform.position = pos;
                leftBallTimer = ballLifetime;
            }
            else
            {
                leftBallTimer -= Time.deltaTime;
                if (leftBallTimer <= 0f && leftBall != null) { Destroy(leftBall); leftBall = null; }
            }
        }

        private void RunMods()
        {
            Rigidbody rb = null;
            try
            {
                if (GorillaLocomotion.GTPlayer.Instance != null)
                    rb = GorillaLocomotion.GTPlayer.Instance.GetComponent<Rigidbody>();
            }
            catch { }

            // Speed Boost
            if (movementStates[0])
            {
                try
                {
                    if (GorillaLocomotion.GTPlayer.Instance != null)
                    {
                        GorillaLocomotion.GTPlayer.Instance.maxJumpSpeed = 6.5f * speedBoostMultiplier;
                        GorillaLocomotion.GTPlayer.Instance.jumpMultiplier = 1.15f * speedBoostMultiplier;
                    }
                }
                catch { }
            }

            // Flight
            bool anyFly = movementStates[1] || movementStates[2] || movementStates[3] || movementStates[4];
            if (anyFly)
            {
                if (rb != null) { rb.velocity = Vector3.zero; rb.useGravity = false; }

                Vector3 dir = Vector3.zero;
                Transform cam = GetHead();
                if (cam != null)
                {
                    if (Input.GetKey(KeyCode.W)) dir += cam.forward;
                    if (Input.GetKey(KeyCode.S)) dir -= cam.forward;
                    if (Input.GetKey(KeyCode.A)) dir -= cam.right;
                    if (Input.GetKey(KeyCode.D)) dir += cam.right;
                    if (Input.GetKey(KeyCode.Space)) dir += Vector3.up;
                }

                if (dir.sqrMagnitude > 0.01f)
                {
                    try
                    {
                        if (GorillaTagger.Instance != null)
                            GorillaTagger.Instance.transform.position += dir.normalized * flySpeed * Time.deltaTime;
                    }
                    catch { }
                }
            }
            else if (rb != null)
            {
                rb.useGravity = true;
            }

            // Low Gravity
            if (movementStates[7])
            {
                Physics.gravity = originalGravity * lowGravityMultiplier;
                gravityModified = true;
            }
            else if (gravityModified)
            {
                Physics.gravity = originalGravity;
                gravityModified = false;
            }
        }
    }
}
