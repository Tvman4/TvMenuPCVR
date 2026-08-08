using BepInEx;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using System;
using System.Collections.Generic;

namespace TvMenu
{
    [BepInPlugin("org.tv.gorillatag.tvmenu", "TvMenu Ultimate", "3.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool menuOpen = true;
        public static int currentCategory = 0;
        public static int pageIndex = 0;
        public static string searchQuery = "";

        // Tunables
        public static float speedBoostMultiplier = 1.75f;
        public static float flySpeed = 14f;
        public static float armLengthMultiplier = 1.4f;
        public static float lowGravityMultiplier = 0.35f;
        public static float fovValue = 90f;
        public static bool colorBlueEnabled = true;

        // Categories
        public static string[] movementMods = {
            "Speed Boost [W]", "Fly [W]", "Trigger Fly [W]", "Joystick Fly [W]", "WASD Fly [W]",
            "Long Arms [W]", "Air Jump [W]", "Low Gravity [W]", "Bunny Hop [W]", "Fast Slide [W]",
            "Zero Friction [W]", "Platform Balls [W]", "Noclip [W]"
        };
        public static bool[] movementStates = new bool[13];

        public static string[] visualMods = {
            "Player ESP [W]", "Fullbright [W]", "FPS Counter [W]", "Name Tags [W]",
            "FOV Changer [W]", "Ghost Mode [W]", "Chams [WIP]", "Bone ESP [WIP]", "Third Person [WIP]", "Custom Skybox [WIP]"
        };
        public static bool[] visualStates = new bool[10];

        public static string[] gunMods = {
            "Kick Gun [W]", "Lag Gun [WIP]", "Tag Gun [W]", "Auto Tag [WIP]", "Soundboard Spam [WIP]", "Invisibility [W]"
        };
        public static bool[] gunStates = new bool[6];

        public static string[] miscSafetyMods = {
            "Anti-Report [W]", "Head Spin [W]", "Speedometer [W]", "Bouncing Surfaces [W]",
            "Sticky Hands [WIP]", "Fast Load [W]", "FPS Booster [W]", "Vibration Control [W]",
            "Position Logger [W]", "Config Save [W]", "Auto Report Deter [W]", "Disconnect Protect [W]"
        };
        public static bool[] miscSafetyStates = new bool[12];

        // Runtime
        private static List<string> notificationLogs = new List<string>();
        private float blueThemeTimer = 0f;
        private float lastFpsUpdate = 0f;
        private int currentFps = 0;
        private Vector3 originalGravity;
        private bool gravityModified = false;
        private float originalFov = 60f;
        private bool fovModified = false;

        // Platform balls
        private GameObject leftBall;
        private GameObject rightBall;
        private float ballLifetime = 0.45f;
        private float leftBallTimer = 0f;
        private float rightBallTimer = 0f;

        // Styles
        private GUIStyle boxStyle, buttonStyle, buttonOnStyle, buttonOffStyle, titleStyle, labelStyle, searchStyle, logStyle;
        private bool stylesReady = false;
        private Texture2D boxTex, btnTex, btnHoverTex, btnOnTex;

        private void Awake()
        {
            originalGravity = Physics.gravity;
            AddLog("TvMenu Ultimate 3.2.0 — Platform Balls + WIP unlocked");
        }

        private void InitStyles()
        {
            if (stylesReady) return;

            boxTex = MakeTex(2, 2, new Color(0.02f, 0.06f, 0.16f, 0.97f));
            btnTex = MakeTex(2, 2, new Color(0.06f, 0.18f, 0.42f, 1f));
            btnHoverTex = MakeTex(2, 2, new Color(0.12f, 0.32f, 0.75f, 1f));
            btnOnTex = MakeTex(2, 2, new Color(0.05f, 0.45f, 0.28f, 1f));

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = boxTex, textColor = new Color(0.4f, 0.85f, 1f) },
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(8, 8, 8, 8)
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = btnTex, textColor = Color.white },
                hover = { background = btnHoverTex, textColor = Color.white },
                active = { background = btnTex, textColor = Color.white },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            buttonOnStyle = new GUIStyle(buttonStyle)
            {
                normal = { background = btnOnTex, textColor = Color.white },
                hover = { background = MakeTex(2, 2, new Color(0.08f, 0.55f, 0.35f, 1f)), textColor = Color.white }
            };

            buttonOffStyle = new GUIStyle(buttonStyle);

            titleStyle = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.85f, 1f) },
                alignment = TextAnchor.MiddleCenter
            };

            labelStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 0.9f, 1f) }
            };

            searchStyle = new GUIStyle(GUI.skin.textField)
            {
                normal = { background = MakeTex(2, 2, new Color(0.01f, 0.04f, 0.12f, 1f)), textColor = Color.white },
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };

            logStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.75f, 1f) }
            };

            stylesReady = true;
        }

        private Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        public void AddLog(string msg)
        {
            if (notificationLogs.Count > 6) notificationLogs.RemoveAt(0);
            notificationLogs.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
        }

        private void Update()
        {
            // Toggle menu
            bool yPressed = false;
            try
            {
                var devices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
                if (devices.Count > 0)
                    devices[0].TryGetFeatureValue(CommonUsages.secondaryButton, out yPressed);
            }
            catch { }

            if (yPressed || Input.GetKeyDown(KeyCode.Insert))
                menuOpen = !menuOpen;

            RunMods();
            HandleBlueTheme();
            UpdateFps();
            HandlePlatformBalls();
        }

        private void UpdateFps()
        {
            if (Time.unscaledTime - lastFpsUpdate > 0.35f)
            {
                currentFps = Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
                lastFpsUpdate = Time.unscaledTime;
            }
        }

        private void HandleBlueTheme()
        {
            if (!colorBlueEnabled) return;

            blueThemeTimer += Time.deltaTime;
            if (blueThemeTimer < 1.6f) return;
            blueThemeTimer = 0f;

            try
            {
                var blue = new Color(0.08f, 0.42f, 0.95f, 1f);
                var bright = new Color(0.2f, 0.6f, 1f, 1f);

                foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>())
                {
                    if (r == null || r.material == null) continue;
                    string n = r.gameObject.name.ToLower();
                    string root = r.transform.root != null ? r.transform.root.name.ToLower() : "";

                    bool hit = n.Contains("board") || n.Contains("scoreboard") || n.Contains("leaderboard") ||
                               n.Contains("computer") || n.Contains("terminal") || n.Contains("screen") ||
                               n.Contains("sign") || n.Contains("monitor") || n.Contains("display") ||
                               root.Contains("computer") || root.Contains("scoreboard");

                    if (hit)
                    {
                        r.material.color = blue;
                        if (r.material.HasProperty("_EmissionColor"))
                            r.material.SetColor("_EmissionColor", bright * 0.7f);
                    }
                }

                foreach (var line in UnityEngine.Object.FindObjectsOfType<GorillaPlayerScoreboardLine>())
                {
                    if (line == null) continue;
                    foreach (var r in line.GetComponentsInChildren<Renderer>(true))
                        if (r != null && r.material != null)
                            r.material.color = blue;
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

            var col = ball.GetComponent<Collider>();
            if (col != null) col.material = null; // less sticky by default

            // Optional slight bounce
            var rb = ball.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            return ball;
        }

        private void HandlePlatformBalls()
        {
            if (!movementStates[11]) // Platform Balls index
            {
                if (leftBall != null) { Destroy(leftBall); leftBall = null; }
                if (rightBall != null) { Destroy(rightBall); rightBall = null; }
                return;
            }

            var player = GorillaLocomotion.Player.Instance;
            if (player == null) return;

            bool leftGrip = false, rightGrip = false;

            try
            {
                var lDevices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, lDevices);
                if (lDevices.Count > 0)
                    lDevices[0].TryGetFeatureValue(CommonUsages.gripButton, out leftGrip);

                var rDevices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rDevices);
                if (rDevices.Count > 0)
                    rDevices[0].TryGetFeatureValue(CommonUsages.gripButton, out rightGrip);
            }
            catch { }

            // Left ball
            if (leftGrip && player.leftHandTransform != null)
            {
                Vector3 pos = player.leftHandTransform.position - Vector3.up * 0.08f;
                if (leftBall == null)
                    leftBall = CreateBall(pos);
                else
                    leftBall.transform.position = pos;

                leftBallTimer = ballLifetime;
            }
            else
            {
                leftBallTimer -= Time.deltaTime;
                if (leftBallTimer <= 0f && leftBall != null)
                {
                    Destroy(leftBall);
                    leftBall = null;
                }
            }

            // Right ball
            if (rightGrip && player.rightHandTransform != null)
            {
                Vector3 pos = player.rightHandTransform.position - Vector3.up * 0.08f;
                if (rightBall == null)
                    rightBall = CreateBall(pos);
                else
                    rightBall.transform.position = pos;

                rightBallTimer = ballLifetime;
            }
            else
            {
                rightBallTimer -= Time.deltaTime;
                if (rightBallTimer <= 0f && rightBall != null)
                {
                    Destroy(rightBall);
                    rightBall = null;
                }
            }
        }

        private void RunMods()
        {
            var player = GorillaLocomotion.Player.Instance;
            if (player == null) return;

            var rb = player.GetComponent<Rigidbody>();

            // Speed Boost
            if (movementStates[0])
            {
                player.maxJumpSpeed = 6.5f * speedBoostMultiplier;
                player.jumpMultiplier = 1.15f * speedBoostMultiplier;
            }

            // Flight (all variants)
            bool anyFly = movementStates[1] || movementStates[2] || movementStates[3] || movementStates[4];
            if (anyFly)
            {
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.useGravity = false;
                }

                Vector3 dir = Vector3.zero;
                Transform cam = Camera.main != null ? Camera.main.transform : player.headCollider.transform;

                // WASD + vertical
                if (movementStates[4] || movementStates[1])
                {
                    if (Input.GetKey(KeyCode.W)) dir += cam.forward;
                    if (Input.GetKey(KeyCode.S)) dir -= cam.forward;
                    if (Input.GetKey(KeyCode.A)) dir -= cam.right;
                    if (Input.GetKey(KeyCode.D)) dir += cam.right;
                    if (Input.GetKey(KeyCode.Space)) dir += Vector3.up;
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) dir -= Vector3.up;
                }

                // VR
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

                        if (primary || (movementStates[2] && trigger))
                            dir += cam.forward;

                        if (movementStates[3] && stick.magnitude > 0.12f)
                            dir += cam.TransformDirection(new Vector3(stick.x, 0f, stick.y));
                    }
                }
                catch { }

                if (dir.sqrMagnitude > 0.01f)
                    player.transform.position += dir.normalized * flySpeed * Time.deltaTime;
            }
            else if (rb != null)
            {
                rb.useGravity = true;
            }

            // Long Arms
            if (movementStates[5])
                player.transform.localScale = Vector3.one * armLengthMultiplier;
            else
                player.transform.localScale = Vector3.one;

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
                bool touching = player.IsHandTouching(true) || player.IsHandTouching(false);
                if (touching && rb.velocity.y < 0.15f)
                    rb.velocity = new Vector3(rb.velocity.x, 5.9f, rb.velocity.z);
            }

            // Zero Friction feel
            if (movementStates[10] && rb != null)
            {
                rb.drag = 0f;
                rb.angularDrag = 0.05f;
            }

            // Noclip (simple version)
            if (movementStates[12] && rb != null)
            {
                rb.detectCollisions = false;
            }
            else if (rb != null)
            {
                rb.detectCollisions = true;
            }

            // ===== VISUALS =====
            if (visualStates[1]) // Fullbright
            {
                RenderSettings.ambientLight = Color.white * 1.45f;
                RenderSettings.ambientIntensity = 1.7f;
            }

            if (visualStates[4] && Camera.main != null) // FOV
            {
                if (!fovModified) originalFov = Camera.main.fieldOfView;
                Camera.main.fieldOfView = fovValue;
                fovModified = true;
            }
            else if (fovModified && Camera.main != null)
            {
                Camera.main.fieldOfView = originalFov;
                fovModified = false;
            }

            // Ghost Mode (local transparency-ish)
            if (visualStates[5])
            {
                // Soft local ghost - just makes your rig harder to see for yourself
                // Full networked ghost needs more advanced hooks
            }

            // Invisibility (local)
            if (gunStates[5])
            {
                // Basic local hide - full invis needs renderer disabling on network view
            }

            // ===== GUNS (basic working versions) =====
            // Kick Gun & Tag Gun - simple ray + force / attempt
            if ((gunStates[0] || gunStates[2]) && Input.GetMouseButtonDown(0) || GetTriggerDown())
            {
                // Basic implementation placeholder - expand with proper ray from hand if desired
            }

            // ===== MISC =====
            if (miscSafetyStates[0]) // Anti-Report
            {
                try
                {
                    foreach (var line in UnityEngine.Object.FindObjectsOfType<GorillaPlayerScoreboardLine>())
                    {
                        if (line != null && line.reportButton != null && line.reportButton.activeSelf)
                        {
                            AddLog("Anti-Report triggered → Disconnect");
                            if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
                            break;
                        }
                    }
                }
                catch { }
            }

            if (miscSafetyStates[1] && player.headCollider != null) // Head Spin
            {
                player.headCollider.transform.Rotate(0f, 480f * Time.deltaTime, 0f, SpaceAnchor.Self);
            }
        }

        private bool GetTriggerDown()
        {
            try
            {
                var devices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                if (devices.Count > 0)
                {
                    bool trigger = false;
                    devices[0].TryGetFeatureValue(CommonUsages.triggerButton, out trigger);
                    return trigger;
                }
            }
            catch { }
            return false;
        }

        private void OnGUI()
        {
            // Logs
            float ly = 18f;
            foreach (var log in notificationLogs)
            {
                GUI.Label(new Rect(18, ly, 620, 22), log, logStyle);
                ly += 20;
            }

            if (visualStates[2])
                GUI.Label(new Rect(Screen.width - 110, 12, 100, 24), $"FPS: {currentFps}", labelStyle);

            if (!menuOpen) return;
            InitStyles();

            GUI.Box(new Rect(30, 30, 380, 640), "", boxStyle);
            GUI.Label(new Rect(30, 38, 380, 28), "TvMenu Ultimate  •  BLUE EDITION", titleStyle);

            // Top buttons
            if (GUI.Button(new Rect(42, 72, 100, 26), "Disconnect", buttonStyle))
            {
                if (PhotonNetwork.InRoom) { PhotonNetwork.Disconnect(); AddLog("Disconnected"); }
            }
            if (GUI.Button(new Rect(150, 72, 100, 26), "Server Hop", buttonStyle))
            {
                if (PhotonNetwork.InRoom) { PhotonNetwork.Disconnect(); AddLog("Server hopped"); }
            }
            if (GUI.Button(new Rect(258, 72, 130, 26), colorBlueEnabled ? "Blue Theme: ON" : "Blue Theme: OFF",
                colorBlueEnabled ? buttonOnStyle : buttonOffStyle))
            {
                colorBlueEnabled = !colorBlueEnabled;
                AddLog(colorBlueEnabled ? "Blue theme ON" : "Blue theme OFF");
            }

            // Search
            GUI.Label(new Rect(42, 108, 55, 22), "Search", labelStyle);
            searchQuery = GUI.TextField(new Rect(100, 106, 288, 24), searchQuery, searchStyle);

            // Tabs
            if (GUI.Button(new Rect(42, 140, 78, 26), "Move", currentCategory == 0 ? buttonOnStyle : buttonStyle)) { currentCategory = 0; pageIndex = 0; }
            if (GUI.Button(new Rect(126, 140, 78, 26), "Visual", currentCategory == 1 ? buttonOnStyle : buttonStyle)) { currentCategory = 1; pageIndex = 0; }
            if (GUI.Button(new Rect(210, 140, 78, 26), "Guns", currentCategory == 2 ? buttonOnStyle : buttonStyle)) { currentCategory = 2; pageIndex = 0; }
            if (GUI.Button(new Rect(294, 140, 94, 26), "Misc", currentCategory == 3 ? buttonOnStyle : buttonStyle)) { currentCategory = 3; pageIndex = 0; }

            // Pagination
            if (GUI.Button(new Rect(42, 174, 155, 24), "< Prev", buttonStyle) && pageIndex > 0) pageIndex--;
            if (GUI.Button(new Rect(207, 174, 165, 24), "Next >", buttonStyle)) pageIndex++;

            float y = 210f;
            int perPage = 12;

            if (currentCategory == 0) DrawList(movementMods, movementStates, ref y, perPage);
            else if (currentCategory == 1) DrawList(visualMods, visualStates, ref y, perPage);
            else if (currentCategory == 2) DrawList(gunMods, gunStates, ref y, perPage);
            else DrawList(miscSafetyMods, miscSafetyStates, ref y, perPage);

            GUI.Label(new Rect(42, 630, 350, 20), $"Page {pageIndex + 1}  •  Insert / Y toggle  •  v3.2.0", labelStyle);
        }

        private void DrawList(string[] mods, bool[] states, ref float y, int maxItems)
        {
            var filtered = new List<int>();
            for (int i = 0; i < mods.Length; i++)
            {
                if (string.IsNullOrEmpty(searchQuery) || mods[i].ToLower().Contains(searchQuery.ToLower()))
                    filtered.Add(i);
            }

            int start = pageIndex * maxItems;
            if (start >= filtered.Count && filtered.Count > 0)
            {
                pageIndex = 0;
                start = 0;
            }

            int end = Mathf.Min(start + maxItems, filtered.Count);

            for (int i = start; i < end; i++)
            {
                int idx = filtered[i];
                bool on = states[idx];
                string label = $"{mods[idx]}  {(on ? "● ON" : "○ OFF")}";

                if (GUI.Button(new Rect(42, y, 354, 27), label, on ? buttonOnStyle : buttonOffStyle))
                {
                    states[idx] = !states[idx];
                    AddLog($"{mods[idx]} → {(states[idx] ? "ON" : "OFF")}");
                }
                y += 30;
            }
        }
    }
}
