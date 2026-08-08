using BepInEx;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using Photon.Pun;
using System;
using System.Collections.Generic;

namespace TvMenu
{
    [BepInPlugin("org.tv.gorillatag.tvmenu", "TvMenu Beta", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool menuOpen = false;
        public static int currentCategory = 0;
        public static bool colorBlueEnabled = true;

        public static float speedBoostMultiplier = 1.75f;
        public static float flySpeed = 14f;
        public static float armLengthMultiplier = 1.4f;
        public static float lowGravityMultiplier = 0.35f;

        // Mod states
        public static bool[] movementStates = new bool[13];
        public static bool[] visualStates = new bool[10];
        public static bool[] gunStates = new bool[6];
        public static bool[] miscSafetyStates = new bool[12];

        // [W] = Working, [WIP] = Work In Progress (usable), [NW] = Not Working (locked)
        public static string[] movementMods = {
            "Speed Boost [W]", "Fly [W]", "Trigger Fly [W]", "Joystick Fly [W]", "WASD Fly [W]",
            "Long Arms [W]", "Air Jump [W]", "Low Gravity [W]", "Bunny Hop [W]", "Fast Slide [W]",
            "Zero Friction [W]", "Platform Balls [W]", "Noclip [W]"
        };

        public static string[] visualMods = {
            "Player ESP [W]", "Fullbright [W]", "FPS Counter [W]", "Name Tags [W]",
            "FOV Changer [W]", "Ghost Mode [W]", "Chams [WIP]", "Bone ESP [WIP]", "Third Person [WIP]", "Custom Skybox [WIP]"
        };

        public static string[] gunMods = {
            "Kick Gun [W]", "Lag Gun [WIP]", "Tag Gun [W]", "Auto Tag [WIP]", "Soundboard Spam [WIP]", "Invisibility [W]"
        };

        public static string[] miscSafetyMods = {
            "Anti-Report [W]", "Head Spin [W]", "Speedometer [W]", "Bouncing Surfaces [W]",
            "Sticky Hands [WIP]", "Fast Load [W]", "FPS Booster [W]", "Vibration Control [W]",
            "Position Logger [W]", "Config Save [W]", "Auto Report Deter [W]", "Disconnect Protect [W]"
        };

        // VR Menu
        private GameObject menuObject;
        private Canvas menuCanvas;
        private Transform leftHand;
        private bool yWasPressed = false;

        // Runtime
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
            Debug.Log("[TvMenu] TvMenu Beta 1.0.0 loaded - VR Wrist Menu");
        }

        private void Update()
        {
            HandleYToggle();
            UpdateMenuPosition();
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

            // Toggle only on button down
            if (yPressed && !yWasPressed)
            {
                menuOpen = !menuOpen;

                if (menuOpen)
                    CreateWristMenu();
                else
                    DestroyWristMenu();
            }

            yWasPressed = yPressed;
        }

        private void CreateWristMenu()
        {
            DestroyWristMenu();

            menuObject = new GameObject("TvMenu_WristMenu");
            menuCanvas = menuObject.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.WorldSpace;

            var scaler = menuObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            menuObject.AddComponent<GraphicRaycaster>();

            RectTransform rt = menuObject.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, 1300);
            menuObject.transform.localScale = Vector3.one * 0.0007f;

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(menuObject.transform, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.02f, 0.06f, 0.15f, 0.95f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Title + Version
            CreateText(menuObject.transform, "TvMenu Beta", 30, new Vector2(0, 560), new Color(0.3f, 0.85f, 1f));
            CreateText(menuObject.transform, "Version Beta 1.0.0", 18, new Vector2(0, 510), new Color(0.5f, 0.75f, 1f));

            // Category buttons
            CreateMenuButton(menuObject.transform, "MOVEMENT", new Vector2(-180, 430), () => { currentCategory = 0; RefreshModButtons(); });
            CreateMenuButton(menuObject.transform, "VISUAL", new Vector2(0, 430), () => { currentCategory = 1; RefreshModButtons(); });
            CreateMenuButton(menuObject.transform, "GUNS", new Vector2(180, 430), () => { currentCategory = 2; RefreshModButtons(); });
            CreateMenuButton(menuObject.transform, "MISC", new Vector2(0, 360), () => { currentCategory = 3; RefreshModButtons(); });

            // Blue theme toggle
            CreateMenuButton(menuObject.transform, colorBlueEnabled ? "BLUE: ON" : "BLUE: OFF", new Vector2(0, 290), () =>
            {
                colorBlueEnabled = !colorBlueEnabled;
                DestroyWristMenu();
                CreateWristMenu();
            });

            // Disconnect / Server Hop
            CreateMenuButton(menuObject.transform, "DISCONNECT", new Vector2(-130, -560), () =>
            {
                if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
            });

            CreateMenuButton(menuObject.transform, "SERVER HOP", new Vector2(130, -560), () =>
            {
                if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
            });

            RefreshModButtons();
        }

        private void RefreshModButtons()
        {
            // Remove old mod buttons
            foreach (Transform child in menuObject.transform)
            {
                if (child.name.StartsWith("ModBtn_"))
                    Destroy(child.gameObject);
            }

            string[] mods = null;
            bool[] states = null;

            if (currentCategory == 0) { mods = movementMods; states = movementStates; }
            else if (currentCategory == 1) { mods = visualMods; states = visualStates; }
            else if (currentCategory == 2) { mods = gunMods; states = gunStates; }
            else { mods = miscSafetyMods; states = miscSafetyStates; }

            float startY = 210f;
            for (int i = 0; i < mods.Length; i++)
            {
                int index = i;
                string modName = mods[i];
                bool isNW = modName.Contains("[NW]");
                bool isOn = states[i];

                string label = modName + (isOn ? "  [ON]" : "  [OFF]");
                Color col;

                if (isNW)
                {
                    // Locked - greyed out
                    col = new Color(0.25f, 0.25f, 0.25f);
                    label = modName + "  [LOCKED]";
                }
                else if (isOn)
                {
                    col = new Color(0.1f, 0.6f, 0.3f);
                }
                else
                {
                    col = new Color(0.15f, 0.25f, 0.45f);
                }

                CreateMenuButton(menuObject.transform, label, new Vector2(0, startY - (i * 52)), () =>
                {
                    // Block [NW] mods from being toggled
                    if (mods[index].Contains("[NW]")) return;

                    states[index] = !states[index];
                    DestroyWristMenu();
                    CreateWristMenu();
                }, col, "ModBtn_" + i);
            }
        }

        private void CreateMenuButton(Transform parent, string text, Vector2 pos, Action onClick, Color? customColor = null, string objName = "Btn")
        {
            GameObject btnObj = new GameObject(objName);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = customColor ?? new Color(0.08f, 0.2f, 0.45f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360, 46);
            rt.anchoredPosition = pos;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 18;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        private void CreateText(Transform parent, string content, int fontSize, Vector2 pos, Color color)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            Text txt = obj.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700, 50);
            rt.anchoredPosition = pos;
        }

        private void DestroyWristMenu()
        {
            if (menuObject != null)
            {
                Destroy(menuObject);
                menuObject = null;
            }
        }

        private void UpdateMenuPosition()
        {
            if (!menuOpen || menuObject == null) return;

            leftHand = GetLeftHand();
            if (leftHand == null) return;

            menuObject.transform.position = leftHand.position + leftHand.up * 0.12f + leftHand.forward * 0.08f;
            menuObject.transform.rotation = leftHand.rotation * Quaternion.Euler(0, 180f, 0);
        }

        private Transform GetLeftHand()
        {
            try
            {
                if (GorillaTagger.Instance != null && GorillaTagger.Instance.leftHandTransform != null)
                    return GorillaTagger.Instance.leftHandTransform;
                if (GorillaLocomotion.Player.Instance != null && GorillaLocomotion.Player.Instance.leftHandTransform != null)
                    return GorillaLocomotion.Player.Instance.leftHandTransform;
            }
            catch { }
            return null;
        }

        private Transform GetRightHand()
        {
            try
            {
                if (GorillaTagger.Instance != null && GorillaTagger.Instance.rightHandTransform != null)
                    return GorillaTagger.Instance.rightHandTransform;
                if (GorillaLocomotion.Player.Instance != null && GorillaLocomotion.Player.Instance.rightHandTransform != null)
                    return GorillaLocomotion.Player.Instance.rightHandTransform;
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
                if (GorillaLocomotion.Player.Instance != null && GorillaLocomotion.Player.Instance.headCollider != null)
                    return GorillaLocomotion.Player.Instance.headCollider.transform;
            }
            catch { }
            return Camera.main != null ? Camera.main.transform : null;
        }

        private bool IsLeftHandTouching()
        {
            try
            {
                if (GorillaLocomotion.GTPlayer.Instance != null)
                    return GorillaLocomotion.GTPlayer.Instance.IsHandTouching(true);
                if (GorillaLocomotion.Player.Instance != null)
                    return GorillaLocomotion.Player.Instance.IsHandTouching(true);
            }
            catch { }
            return false;
        }

        private bool IsRightHandTouching()
        {
            try
            {
                if (GorillaLocomotion.GTPlayer.Instance != null)
                    return GorillaLocomotion.GTPlayer.Instance.IsHandTouching(false);
                if (GorillaLocomotion.Player.Instance != null)
                    return GorillaLocomotion.Player.Instance.IsHandTouching(false);
            }
            catch { }
            return false;
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

                string[] keywords = {
                    "board", "scoreboard", "leaderboard", "computer", "terminal",
                    "screen", "sign", "monitor", "display", "console", "keyboard"
                };

                foreach (Renderer r in UnityEngine.Object.FindObjectsOfType<Renderer>())
                {
                    if (r == null) continue;

                    string name = r.gameObject.name.ToLower();
                    string rootName = r.transform.root != null ? r.transform.root.name.ToLower() : "";

                    bool shouldColor = false;
                    foreach (string key in keywords)
                    {
                        if (name.Contains(key) || rootName.Contains(key))
                        {
                            shouldColor = true;
                            break;
                        }
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

                    if (r.sharedMaterial != null)
                    {
                        r.sharedMaterial.color = blue;
                        if (r.sharedMaterial.HasProperty("_EmissionColor"))
                        {
                            r.sharedMaterial.EnableKeyword("_EMISSION");
                            r.sharedMaterial.SetColor("_EmissionColor", emission);
                        }
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
            Transform right = GetRightHand();

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

            if (rightGrip && right != null)
            {
                Vector3 pos = right.position - Vector3.up * 0.08f;
                if (rightBall == null) rightBall = CreateBall(pos);
                else rightBall.transform.position = pos;
                rightBallTimer = ballLifetime;
            }
            else
            {
                rightBallTimer -= Time.deltaTime;
                if (rightBallTimer <= 0f && rightBall != null) { Destroy(rightBall); rightBall = null; }
            }
        }

        private void RunMods()
        {
            Rigidbody rb = null;
            try
            {
                if (GorillaLocomotion.GTPlayer.Instance != null)
                    rb = GorillaLocomotion.GTPlayer.Instance.GetComponent<Rigidbody>();
                else if (GorillaLocomotion.Player.Instance != null)
                    rb = GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>();
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
                    else if (GorillaLocomotion.Player.Instance != null)
                    {
                        GorillaLocomotion.Player.Instance.maxJumpSpeed = 6.5f * speedBoostMultiplier;
                        GorillaLocomotion.Player.Instance.jumpMultiplier = 1.15f * speedBoostMultiplier;
                    }
                }
                catch { }
            }

            // Flight
            bool anyFly = movementStates[1] || movementStates[2] || movementStates[3] || movementStates[4];
            if (anyFly)
            {
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.useGravity = false;
                }

                Vector3 dir = Vector3.zero;
                Transform cam = GetHead();
                if (cam == null) return;

                if (movementStates[4] || movementStates[1])
                {
                    if (Input.GetKey(KeyCode.W)) dir += cam.forward;
                    if (Input.GetKey(KeyCode.S)) dir -= cam.forward;
                    if (Input.GetKey(KeyCode.A)) dir -= cam.right;
                    if (Input.GetKey(KeyCode.D)) dir += cam.right;
                    if (Input.GetKey(KeyCode.Space)) dir += Vector3.up;
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) dir -= Vector3.up;
                }

                try
                {
                    var rDevices = new List<InputDevice>();
                    InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rDevices);
                    if (rDevices.Count > 0)
                    {
                        bool primary = false, trigger = false;
                        Vector2 stick = Vector2.zero;
                        rDevices[0].TryGetFeatureValue(CommonUsages.primaryButton, out primary);
                        rDevices[0].TryGetFeatureValue(CommonUsages.triggerButton, out trigger);
                        rDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out stick);

                        if (primary || (movementStates[2] && trigger)) dir += cam.forward;
                        if (movementStates[3] && stick.magnitude > 0.12f)
                            dir += cam.TransformDirection(new Vector3(stick.x, 0f, stick.y));
                    }
                }
                catch { }

                if (dir.sqrMagnitude > 0.01f)
                {
                    try
                    {
                        if (GorillaTagger.Instance != null)
                            GorillaTagger.Instance.transform.position += dir.normalized * flySpeed * Time.deltaTime;
                        else if (GorillaLocomotion.Player.Instance != null)
                            GorillaLocomotion.Player.Instance.transform.position += dir.normalized * flySpeed * Time.deltaTime;
                    }
                    catch { }
                }
            }
            else if (rb != null)
            {
                rb.useGravity = true;
            }

            // Long Arms
            try
            {
                if (movementStates[5])
                {
                    if (GorillaTagger.Instance != null)
                        GorillaTagger.Instance.transform.localScale = Vector3.one * armLengthMultiplier;
                    else if (GorillaLocomotion.Player.Instance != null)
                        GorillaLocomotion.Player.Instance.transform.localScale = Vector3.one * armLengthMultiplier;
                }
                else
                {
                    if (GorillaTagger.Instance != null)
                        GorillaTagger.Instance.transform.localScale = Vector3.one;
                    else if (GorillaLocomotion.Player.Instance != null)
                        GorillaLocomotion.Player.Instance.transform.localScale = Vector3.one;
                }
            }
            catch { }

            // Air Jump
            if (movementStates[6] && Input.GetKeyDown(KeyCode.Space) && rb != null)
                rb.velocity = new Vector3(rb.velocity.x, 6.8f, rb.velocity.z);

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

            // Bunny Hop
            if (movementStates[8] && rb != null)
            {
                if ((IsLeftHandTouching() || IsRightHandTouching()) && rb.velocity.y < 0.15f)
                    rb.velocity = new Vector3(rb.velocity.x, 5.9f, rb.velocity.z);
            }

            // Zero Friction
            if (movementStates[10] && rb != null)
            {
                rb.drag = 0f;
                rb.angularDrag = 0.05f;
            }

            // Noclip
            if (rb != null)
                rb.detectCollisions = !movementStates[12];

            // Anti-Report
            if (miscSafetyStates[0])
            {
                try
                {
                    foreach (var line in UnityEngine.Object.FindObjectsOfType<GorillaPlayerScoreboardLine>())
                    {
                        if (line != null && line.reportButton != null && line.reportButton.activeSelf)
                        {
                            if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
                            break;
                        }
                    }
                }
                catch { }
            }

            // Head Spin
            if (miscSafetyStates[1])
            {
                Transform head = GetHead();
                if (head != null)
                    head.Rotate(0f, 480f * Time.deltaTime, 0f, Space.Self);
            }
        }
    }
}
