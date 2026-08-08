using BepInEx;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TvMenu
{
    [BepInPlugin("org.tv.gorillatag.tvmenu", "TvMenu Ultimate", "3.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool menuOpen = true;
        public static int currentCategory = 0;
        public static int pageIndex = 0;
        public static string searchQuery = "";

        // Core values
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
            "Zero Friction [W]", "Platforms [WIP]", "Noclip [NW]"
        };
        public static bool[] movementStates = new bool[13];

        public static string[] visualMods = {
            "Player ESP [W]", "Fullbright [W]", "FPS Counter [W]", "Name Tags [W]",
            "FOV Changer [W]", "Ghost Mode [NW]", "Chams [NW]", "Bone ESP [NW]", "Third Person [NW]", "Custom Skybox [NW]"
        };
        public static bool[] visualStates = new bool[10];

        public static string[] gunMods = {
            "Kick Gun [WIP]", "Lag Gun [WIP]", "Tag Gun [WIP]", "Auto Tag [WIP]", "Soundboard Spam [NW]", "Invisibility [NW]"
        };
        public static bool[] gunStates = new bool[6];

        public static string[] miscSafetyMods = {
            "Anti-Report [W]", "Head Spin [W]", "Speedometer [W]", "Bouncing Surfaces [W]",
            "Sticky Hands [NW]", "Fast Load [W]", "FPS Booster [W]", "Vibration Control [W]",
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

        // Styles
        private GUIStyle boxStyle, buttonStyle, buttonOnStyle, buttonOffStyle, titleStyle, labelStyle, searchStyle, logStyle;
        private bool stylesReady = false;
        private Texture2D boxTex, btnTex, btnHoverTex, btnOnTex;

        private void Awake()
        {
            originalGravity = Physics.gravity;
            AddLog("TvMenu Ultimate 3.1.0 loaded — Blue Edition");
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
            // Toggle (Y / Insert)
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
            if (blueThemeTimer < 1.8f) return; // Throttled
            blueThemeTimer = 0f;

            try
            {
                var blue = new Color(0.08f, 0.42f, 0.95f, 1f);
                var brightBlue = new Color(0.15f, 0.55f, 1f, 1f);

                // Broad name matching for signs, boards, computers, leaderboards
                foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>())
                {
                    if (r == null || r.material == null) continue;
                    string n = r.gameObject.name.ToLower();
                    string path = r.transform.root != null ? r.transform.root.name.ToLower() : "";

                    bool isTarget =
                        n.Contains("board") || n.Contains("scoreboard") || n.Contains("leaderboard") ||
                        n.Contains("computer") || n.Contains("terminal") || n.Contains("screen") ||
                        n.Contains("sign") || n.Contains("monitor") || n.Contains("display") ||
                        path.Contains("computer") || path.Contains("scoreboard");

                    if (isTarget)
                    {
                        r.material.color = blue;
                        if (r.material.HasProperty("_EmissionColor"))
                            r.material.SetColor("_EmissionColor", brightBlue * 0.6f);
                    }
                }

                // Scoreboard lines specifically
                foreach (var line in UnityEngine.Object.FindObjectsOfType<GorillaPlayerScoreboardLine>())
                {
                    if (line != null)
                    {
                        var renderers = line.GetComponentsInChildren<Renderer>(true);
                        foreach (var r in renderers)
                            if (r != null && r.material != null)
                                r.material.color = blue;
                    }
                }
            }
            catch { }
        }

        private void RunMods()
        {
            var player = GorillaLocomotion.Player.Instance;
            if (player == null) return;

            var rb = player.GetComponent<Rigidbody>();

            // ===== MOVEMENT =====
            // Speed Boost
            if (movementStates[0])
            {
                player.maxJumpSpeed = 6.5f * speedBoostMultiplier;
                player.jumpMultiplier = 1.15f * speedBoostMultiplier;
            }

            // Unified Flight (Fly / Trigger / Joystick / WASD)
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

                // WASD + vertical (works even if only WASD Fly is on)
                if (movementStates[4] || movementStates[1])
                {
                    if (Input.GetKey(KeyCode.W)) dir += cam.forward;
                    if (Input.GetKey(KeyCode.S)) dir -= cam.forward;
                    if (Input.GetKey(KeyCode.A)) dir -= cam.right;
                    if (Input.GetKey(KeyCode.D)) dir += cam.right;
                    if (Input.GetKey(KeyCode.Space)) dir += Vector3.up;
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) dir -= Vector3.up;
                }

                // VR inputs
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
            if (movementStates[6] && Input.GetKeyDown(KeyCode.Space))
            {
                if (rb != null) rb.velocity = new Vector3(rb.velocity.x, 6.5f, rb.velocity.z);
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

            // Bunny Hop (simple)
            if (movementStates[8] && player.IsHandTouching(true) || player.IsHandTouching(false))
            {
                if (rb != null && rb.velocity.y < 0.1f)
                    rb.velocity = new Vector3(rb.velocity.x, 5.8f, rb.velocity.z);
            }

            // Zero Friction-ish
            if (movementStates[10] && rb != null)
            {
                // Soft approach - reduces drag feel
                rb.drag = 0f;
                rb.angularDrag = 0f;
            }

            // ===== VISUALS =====
            if (visualStates[1]) // Fullbright
            {
                RenderSettings.ambientLight = Color.white * 1.4f;
                RenderSettings.ambientIntensity = 1.6f;
            }

            if (visualStates[4]) // FOV
            {
                if (Camera.main != null)
                {
                    if (!fovModified) originalFov = Camera.main.fieldOfView;
                    Camera.main.fieldOfView = fovValue;
                    fovModified = true;
                }
            }
            else if (fovModified && Camera.main != null)
            {
                Camera.main.fieldOfView = originalFov;
                fovModified = false;
            }

            // ===== MISC =====
            // Anti-Report
            if (miscSafetyStates[0])
            {
                try
                {
                    foreach (var line in UnityEngine.Object.FindObjectsOfType<GorillaPlayerScoreboardLine>())
                    {
                        if (line != null && line.reportButton != null && line.reportButton.activeSelf)
                        {
                            AddLog("Anti-Report: Report detected → Disconnecting!");
                            if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
                            break;
                        }
                    }
                }
                catch { }
            }

            // Head Spin
            if (miscSafetyStates[1] && player.headCollider != null)
            {
                player.headCollider.transform.Rotate(0f, 420f * Time.deltaTime, 0f, SpaceAnchor.Self);
            }
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

            // Always-on FPS if enabled
            if (visualStates[2])
            {
                GUI.Label(new Rect(Screen.width - 110, 12, 100, 24), $"FPS: {currentFps}", labelStyle);
            }

            if (!menuOpen) return;
            InitStyles();

            // Main window
            GUI.Box(new Rect(30, 30, 380, 640), "", boxStyle);
            GUI.Label(new Rect(30, 38, 380, 28), "TvMenu Ultimate  •  BLUE EDITION", titleStyle);

            // Top row
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
                AddLog(colorBlueEnabled ? "Blue theme enabled" : "Blue theme disabled");
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

            // List
            float y = 210f;
            int perPage = 12;

            if (currentCategory == 0) DrawList(movementMods, movementStates, ref y, perPage);
            else if (currentCategory == 1) DrawList(visualMods, visualStates, ref y, perPage);
            else if (currentCategory == 2) DrawList(gunMods, gunStates, ref y, perPage);
            else DrawList(miscSafetyMods, miscSafetyStates, ref y, perPage);

            // Footer info
            GUI.Label(new Rect(42, 630, 350, 20), $"Page {pageIndex + 1}  •  Insert / Y to toggle  •  v3.1.0", labelStyle);
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
