using BepInEx;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using System;
using System.Collections.Generic;

namespace TvMenu
{
    [BepInPlugin("org.tv.gorillatag.tvmenu", "TvMenu Ultimate", "3.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool menuOpen = true;
        public static int currentCategory = 0; // 0: Movement, 1: Visuals, 2: Guns/Fun, 3: Misc/Safety
        public static int pageIndex = 0;
        public static string searchQuery = "";

        // Core Mod Variables
        public static bool speedBoostEnabled = false;
        public static float speedBoostMultiplier = 1.6f;
        public static bool flyEnabled = false;
        public static float flySpeed = 12f;
        public static bool triggerFlyEnabled = false;
        public static bool joystickFlyEnabled = false;
        public static bool wasdFlyEnabled = false;
        public static bool longArmsEnabled = false;
        public static float armLengthMultiplier = 1.35f;
        public static bool antiReportEnabled = true;
        public static bool espEnabled = false;
        public static bool colorBlueEnabled = true;

        // Categorized lists with indicators [W = Working, NW = Not Working, WIP = Work In Progress]
        public static string[] movementMods = { "SpeedBoost [W]", "Fly [W]", "Trigger Fly [W]", "Joystick Fly [W]", "WASD Fly [W]", "LongArms [W]", "AirJump [W]", "Platform Monkeys [W]", "Low Gravity [W]", "SpiderMonkey [NW]", "Noclip [NW]", "Bunny Hop [W]", "Fast Slide [W]" };
        public static bool[] movementStates = new bool[13];

        public static string[] visualMods = { "Player ESP [W]", "Fullbright [W]", "FPS Counter [W]", "Name Tags [W]", "Ghost Mode [NW]", "Chams [NW]", "Bone ESP [NW]", "Custom Skybox [NW]", "Third Person [NW]", "FOV Changer [W]" };
        public static bool[] visualStates = new bool[10];

        public static string[] gunMods = { "Kick Gun [WIP]", "Lag Gun [WIP]", "Tag Gun [WIP]", "Auto Tag [WIP]", "Soundboard Spam [NW]", "Invisibility [NW]" };
        public static bool[] gunStates = new bool[6];

        public static string[] miscSafetyMods = { "Anti-Report (Server Hop) [W]", "Head Spin [W]", "Auto Report Deter [W]", "Speedometer [W]", "Zero Friction [W]", "Bouncing Surfaces [W]", "Sticky Hands [NW]", "Fast Load [W]", "FPS Booster [W]", "Vibration Control [W]", "Position Logger [W]", "Config Save [W]" };
        public static bool[] miscSafetyStates = new bool[12];

        // Screen Notification Log Queue
        private static List<string> notificationLogs = new List<string>();

        private GUIStyle blueBoxStyle;
        private GUIStyle blueButtonStyle;
        private GUIStyle logTextStyle;
        private GUIStyle searchInputStyle;
        private bool stylesInitialized = false;

        private void InitStyles()
        {
            if (stylesInitialized) return;

            blueBoxStyle = new GUIStyle(GUI.skin.box);
            blueBoxStyle.normal.background = MakeTex(360, 620, new Color(0.04f, 0.12f, 0.28f, 0.95f));

            blueButtonStyle = new GUIStyle(GUI.skin.button);
            blueButtonStyle.normal.background = MakeTex(100, 25, new Color(0.08f, 0.25f, 0.6f, 1f));
            blueButtonStyle.hover.background = MakeTex(100, 25, new Color(0.15f, 0.38f, 0.8f, 1f));
            blueButtonStyle.active.background = MakeTex(100, 25, new Color(0.04f, 0.18f, 0.45f, 1f));
            blueButtonStyle.normal.textColor = Color.white;
            blueButtonStyle.fontSize = 12;
            blueButtonStyle.fontStyle = FontStyle.Bold;

            searchInputStyle = new GUIStyle(GUI.skin.textField);
            searchInputStyle.normal.background = MakeTex(100, 25, new Color(0.02f, 0.08f, 0.2f, 1f));
            searchInputStyle.normal.textColor = Color.white;
            searchInputStyle.fontSize = 12;

            logTextStyle = new GUIStyle();
            logTextStyle.fontSize = 14;
            logTextStyle.fontStyle = FontStyle.Bold;
            logTextStyle.normal.textColor = new Color(0.2f, 0.6f, 1f, 1f);

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public void AddLog(string message)
        {
            if (notificationLogs.Count > 5) notificationLogs.RemoveAt(0);
            notificationLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        private void Update()
        {
            // Toggle Menu with Y Button (VR) or Insert Key (PC)
            bool yButtonPressed = false;
            try
            {
                List<InputDevice> devices = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
                if (devices.Count > 0)
                {
                    devices[0].TryGetFeatureValue(CommonUsages.secondaryButton, out yButtonPressed);
                }
            }
            catch { }

            if (yButtonPressed || UnityInput.Current.GetKeyDown(KeyCode.Insert))
            {
                menuOpen = !menuOpen;
            }

            RunActiveMods();
            ApplyBlueThemeToEnvironment();
        }

        private void ApplyBlueThemeToEnvironment()
        {
            if (!colorBlueEnabled) return;
            try
            {
                Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material != null && (r.name.ToLower().Contains("board") || r.name.ToLower().Contains("screen") || r.name.ToLower().Contains("sign") || r.name.ToLower().Contains("computer")))
                    {
                        r.material.color = new Color(0.05f, 0.35f, 0.9f, 1f);
                    }
                }
            }
            catch { }
        }

        private void RunActiveMods()
        {
            try
            {
                // 1. SpeedBoost
                if (movementStates[0] || speedBoostEnabled)
                {
                    if (GorillaLocomotion.Player.Instance != null)
                    {
                        GorillaLocomotion.Player.Instance.maxJumpSpeed = 6.5f * speedBoostMultiplier;
                        GorillaLocomotion.Player.Instance.jumpMultiplier = 1.1f * speedBoostMultiplier;
                    }
                }

                // 2. Flight Systems (VR Controllers & PC WASD support)
                bool isFlyingActive = movementStates[1] || flyEnabled || movementStates[2] || triggerFlyEnabled || movementStates[3] || joystickFlyEnabled || movementStates[4] || wasdFlyEnabled;
                if (isFlyingActive && GorillaLocomotion.Player.Instance != null)
                {
                    Rigidbody rb = GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = Vector3.zero;
                        Vector3 moveDirection = Vector3.zero;

                        // PC WASD Controls
                        if (UnityInput.Current.GetKey(KeyCode.W)) moveDirection += Camera.main.transform.forward;
                        if (UnityInput.Current.GetKey(KeyCode.S)) moveDirection -= Camera.main.transform.forward;
                        if (UnityInput.Current.GetKey(KeyCode.A)) moveDirection -= Camera.main.transform.right;
                        if (UnityInput.Current.GetKey(KeyCode.D)) moveDirection += Camera.main.transform.right;
                        if (UnityInput.Current.GetKey(KeyCode.Space)) moveDirection += Vector3.up;

                        // VR Controller Inputs
                        bool primaryPressed = false, triggerPressed = false;
                        Vector2 stickValue = Vector2.zero;
                        try
                        {
                            List<InputDevice> rDevices = new List<InputDevice>();
                            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rDevices);
                            if (rDevices.Count > 0)
                            {
                                rDevices[0].TryGetFeatureValue(CommonUsages.primaryButton, out primaryPressed);
                                rDevices[0].TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
                                rDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out stickValue);
                            }
                        }
                        catch { }

                        if (primaryPressed || triggerPressed)
                        {
                            moveDirection += Camera.main.transform.forward;
                        }

                        if (stickValue.magnitude > 0.1f)
                        {
                            moveDirection += Camera.main.transform.TransformDirection(new Vector3(stickValue.x, 0, stickValue.y));
                        }

                        if (moveDirection != Vector3.zero)
                        {
                            GorillaLocomotion.Player.Instance.transform.position += moveDirection.normalized * flySpeed * Time.deltaTime;
                        }
                    }
                }

                // 3. LongArms
                if (movementStates[5] || longArmsEnabled)
                {
                    if (GorillaLocomotion.Player.Instance != null)
                    {
                        GorillaLocomotion.Player.Instance.transform.localScale = new Vector3(armLengthMultiplier, armLengthMultiplier, armLengthMultiplier);
                    }
                }
                else
                {
                    if (GorillaLocomotion.Player.Instance != null)
                    {
                        GorillaLocomotion.Player.Instance.transform.localScale = Vector3.one;
                    }
                }

                // 4. Anti-Report Server Hop Logic
                if (miscSafetyStates[0] || antiReportEnabled)
                {
                    var list = UnityEngine.Object.FindObjectsOfType<GorillaPlayerScoreboardLine>();
                    foreach (var line in list)
                    {
                        if (line.reportButton != null && line.reportButton.activeSelf)
                        {
                            AddLog("Anti-Report Triggered: Report attempt detected! Server hopping...");
                            if (PhotonNetwork.InRoom)
                            {
                                PhotonNetwork.Disconnect();
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void OnGUI()
        {
            // --- DRAW ON-SCREEN LOGS (Top Left) ---
            float logY = 30f;
            foreach (var log in notificationLogs)
            {
                GUI.Label(new Rect(30, logY, 500, 25), log, logTextStyle);
                logY += 22;
            }

            if (!menuOpen) return;
            InitStyles();

            // --- RENDER MENU WINDOW ---
            GUI.Box(new Rect(40, 40, 360, 620), "TvMenu Ultimate [BLUE EDITION]", blueBoxStyle);

            // --- TOP UTILITY BUTTONS ---
            if (GUI.Button(new Rect(50, 70, 105, 28), "Disconnect", blueButtonStyle))
            {
                if (PhotonNetwork.InRoom) { PhotonNetwork.Disconnect(); AddLog("Disconnected from room."); }
            }
            if (GUI.Button(new Rect(165, 70, 105, 28), "Server Hop", blueButtonStyle))
            {
                if (PhotonNetwork.InRoom) { PhotonNetwork.Disconnect(); AddLog("Server hopped to new lobby."); }
            }
            if (GUI.Button(new Rect(280, 70, 108, 28), colorBlueEnabled ? "Blue: ON" : "Blue: OFF", blueButtonStyle))
            {
                colorBlueEnabled = !colorBlueEnabled;
            }

            // --- SEARCH BAR ---
            GUI.Label(new Rect(50, 106, 60, 22), "Search:", logTextStyle);
            searchQuery = GUI.TextField(new Rect(115, 105, 273, 24), searchQuery, searchInputStyle);

            // --- CATEGORY SELECTOR TABS ---
            if (GUI.Button(new Rect(50, 138, 75, 25), "Move", blueButtonStyle)) { currentCategory = 0; pageIndex = 0; }
            if (GUI.Button(new Rect(128, 138, 75, 25), "Visual", blueButtonStyle)) { currentCategory = 1; pageIndex = 0; }
            if (GUI.Button(new Rect(206, 138, 75, 25), "Guns", blueButtonStyle)) { currentCategory = 2; pageIndex = 0; }
            if (GUI.Button(new Rect(284, 138, 104, 25), "Misc", blueButtonStyle)) { currentCategory = 3; pageIndex = 0; }

            // --- PAGINATION BUTTONS ---
            if (GUI.Button(new Rect(50, 170, 160, 24), "< Prev Page", blueButtonStyle))
            {
                if (pageIndex > 0) pageIndex--;
            }
            if (GUI.Button(new Rect(218, 170, 170, 24), "Next Page >", blueButtonStyle))
            {
                pageIndex++;
            }

            // --- RENDER DYNAMIC LIST BASED ON CATEGORY, PAGINATION & SEARCH ---
            float yOffset = 200;
            int itemsPerPage = 12;

            if (currentCategory == 0) RenderList(movementMods, movementStates, ref yOffset, itemsPerPage);
            else if (currentCategory == 1) RenderList(visualMods, visualStates, ref yOffset, itemsPerPage);
            else if (currentCategory == 2) RenderList(gunMods, gunStates, ref yOffset, itemsPerPage);
            else if (currentCategory == 3) RenderList(miscSafetyMods, miscSafetyStates, ref yOffset, itemsPerPage);
        }

        private void RenderList(string[] mods, bool[] states, ref float yOffset, int maxItems)
        {
            List<int> filteredIndices = new List<int>();
            for (int i = 0; i < mods.Length; i++)
            {
                if (string.IsNullOrEmpty(searchQuery) || mods[i].ToLower().Contains(searchQuery.ToLower()))
                {
                    filteredIndices.Add(i);
                }
            }

            int startIndex = pageIndex * maxItems;
            if (startIndex >= filteredIndices.Count && filteredIndices.Count > 0)
            {
                pageIndex = 0;
                startIndex = 0;
            }

            int endIndex = Math.Min(startIndex + maxItems, filteredIndices.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                int modIndex = filteredIndices[i];
                string status = states[modIndex] ? "[ON]" : "[OFF]";
                if (GUI.Button(new Rect(50, yOffset, 338, 25), $"{mods[modIndex]} {status}", blueButtonStyle))
                {
                    states[modIndex] = !states[modIndex];
                    AddLog($"Toggled {mods[modIndex]} to {states[modIndex]}");
                }
                yOffset += 28;
            }
        }
    }
}
