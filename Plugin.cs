using BepInEx;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using System;
using System.Collections.Generic;

namespace TvMenu
{
    [BepInPlugin("org.tv.gorillatag.tvmenu", "TvMenu Advanced", "2.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool menuOpen = true;
        public static int currentCategory = 0; // 0: Movement, 1: Visuals, 2: Guns/Fun, 3: Misc/Safety
        public static int pageIndex = 0; // Pagination tracker

        // Core Mod Variables
        public static bool speedBoostEnabled = false;
        public static float speedBoostMultiplier = 1.6f;
        public static bool flyEnabled = false;
        public static float flySpeed = 12f;
        public static bool triggerFlyEnabled = false;
        public static bool joystickFlyEnabled = false;
        public static bool longArmsEnabled = false;
        public static float armLengthMultiplier = 1.35f;
        public static bool antiReportEnabled = true;
        public static bool espEnabled = false;
        public static bool colorBlueEnabled = true;

        // Categorized lists
        public static string[] movementMods = { "SpeedBoost", "Fly", "Trigger Fly", "Joystick Fly", "LongArms", "AirJump", "Platform Monkeys", "Low Gravity", "SpiderMonkey", "Noclip", "Bunny Hop", "Fast Slide" };
        public static bool[] movementStates = new bool[12];

        public static string[] visualMods = { "Player ESP", "Fullbright", "FPS Counter", "Name Tags", "Ghost Mode", "Chams", "Bone ESP", "Custom Skybox", "Third Person", "FOV Changer" };
        public static bool[] visualStates = new bool[10];

        public static string[] gunMods = { "Kick Gun", "Lag Gun", "Tag Gun", "Auto Tag", "Soundboard Spam", "Invisibility" };
        public static bool[] gunStates = new bool[6];

        public static string[] miscSafetyMods = { "Anti-Report (Server Hop)", "Head Spin", "Auto Report Deter", "Speedometer", "Zero Friction", "Bouncing Surfaces", "Sticky Hands", "Fast Load", "FPS Booster", "Vibration Control", "Position Logger", "Config Save" };
        public static bool[] miscSafetyStates = new bool[12];

        private GUIStyle blueBoxStyle;
        private GUIStyle blueButtonStyle;
        private GUIStyle blueToggleStyle;
        private bool stylesInitialized = false;

        private void InitStyles()
        {
            if (stylesInitialized) return;

            blueBoxStyle = new GUIStyle(GUI.skin.box);
            blueBoxStyle.normal.background = MakeTex(340, 550, new Color(0.05f, 0.15f, 0.35f, 0.92f));

            blueButtonStyle = new GUIStyle(GUI.skin.button);
            blueButtonStyle.normal.background = MakeTex(100, 25, new Color(0.1f, 0.3f, 0.7f, 1f));
            blueButtonStyle.hover.background = MakeTex(100, 25, new Color(0.2f, 0.45f, 0.85f, 1f));
            blueButtonStyle.active.background = MakeTex(100, 25, new Color(0.05f, 0.2f, 0.5f, 1f));
            blueButtonStyle.normal.textColor = Color.white;
            blueButtonStyle.fontSize = 12;

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

        private void Update()
        {
            // Toggle Menu with Y Button or Insert
            bool yButtonPressed = false;
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
            if (devices.Count > 0)
            {
                devices[0].TryGetFeatureValue(CommonUsages.secondaryButton, out yButtonPressed);
            }

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
                // Turn leaderboards, computer screens, and signs blue
                Renderer[] renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.material != null && (r.name.ToLower().Contains("board") || r.name.ToLower().Contains("screen") || r.name.ToLower().Contains("sign") || r.name.ToLower().Contains("computer")))
                    {
                        r.material.color = new Color(0.1f, 0.4f, 0.9f);
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

                // 2. Fly (Space or Right Primary)
                if (movementStates[1] || flyEnabled)
                {
                    if (GorillaLocomotion.Player.Instance != null)
                    {
                        Rigidbody rb = GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.velocity = Vector3.zero;
                            bool primaryPressed = false;
                            List<InputDevice> devices = new List<InputDevice>();
                            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                            if (devices.Count > 0) devices[0].TryGetFeatureValue(CommonUsages.primaryButton, out primaryPressed);

                            if (primaryPressed || UnityInput.Current.GetKey(KeyCode.Space))
                            {
                                GorillaLocomotion.Player.Instance.transform.position += Camera.main.transform.forward * flySpeed * Time.deltaTime;
                            }
                        }
                    }
                }

                // 3. Trigger Fly
                if (movementStates[2] || triggerFlyEnabled)
                {
                    if (GorillaLocomotion.Player.Instance != null)
                    {
                        bool triggerPressed = false;
                        List<InputDevice> devices = new List<InputDevice>();
                        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                        if (devices.Count > 0) devices[0].TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);

                        if (triggerPressed)
                        {
                            GorillaLocomotion.Player.Instance.transform.position += Camera.main.transform.forward * flySpeed * Time.deltaTime;
                        }
                    }
                }

                // 4. Joystick Fly
                if (movementStates[3] || joystickFlyEnabled)
                {
                    if (GorillaLocomotion.Player.Instance != null)
                    {
                        Vector2 stickValue = Vector2.zero;
                        List<InputDevice> devices = new List<InputDevice>();
                        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                        if (devices.Count > 0) devices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out stickValue);

                        if (stickValue.magnitude > 0.1f)
                        {
                            Vector3 moveDir = new Vector3(stickValue.x, 0, stickValue.y);
                            GorillaLocomotion.Player.Instance.transform.position += Camera.main.transform.TransformDirection(moveDir) * flySpeed * Time.deltaTime;
                        }
                    }
                }

                // 5. LongArms
                if (movementStates[4] || longArmsEnabled)
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

                // Anti-Report Server Hop Logic
                if (miscSafetyStates[0] || antiReportEnabled)
                {
                    var list = UnityEngine.Object.FindObjectsOfType<GorillaPlayerScoreboardLine>();
                    foreach (var line in list)
                    {
                        if (line.reportButton != null)
                        {
                            // If report interaction occurs or is active against local player
                            bool reportActive = line.reportButton.activeSelf; 
                            if (reportActive)
                            {
                                if (PhotonNetwork.InRoom)
                                {
                                    PhotonNetwork.Disconnect(); // Server hop instantly to avoid bans
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void OnGUI()
        {
            if (!menuOpen) return;
            InitStyles();

            GUI.Box(new Rect(50, 50, 340, 580), "TvMenu Advanced [BLUE EDITION]", blueBoxStyle);

            // --- TOP UTILITY BUTTONS ---
            if (GUI.Button(new Rect(60, 80, 105, 28), "Disconnect", blueButtonStyle))
            {
                if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
            }
            if (GUI.Button(new Rect(170, 80, 105, 28), "Server Hop", blueButtonStyle))
            {
                if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
            }
            if (GUI.Button(new Rect(280, 80, 98, 28), colorBlueEnabled ? "Blue: ON" : "Blue: OFF", blueButtonStyle))
            {
                colorBlueEnabled = !colorBlueEnabled;
            }

            // --- CATEGORY SELECTOR TABS ---
            if (GUI.Button(new Rect(60, 115, 75, 25), "Move", blueButtonStyle)) { currentCategory = 0; pageIndex = 0; }
            if (GUI.Button(new Rect(138, 115, 75, 25), "Visual", blueButtonStyle)) { currentCategory = 1; pageIndex = 0; }
            if (GUI.Button(new Rect(216, 115, 75, 25), "Guns", blueButtonStyle)) { currentCategory = 2; pageIndex = 0; }
            if (GUI.Button(new Rect(294, 115, 84, 25), "Misc", blueButtonStyle)) { currentCategory = 3; pageIndex = 0; }

            // --- PAGINATION BUTTONS ---
            if (GUI.Button(new Rect(60, 148, 155, 22), "< Prev Page", blueButtonStyle))
            {
                if (pageIndex > 0) pageIndex--;
            }
            if (GUI.Button(new Rect(223, 148, 155, 22), "Next Page >", blueButtonStyle))
            {
                pageIndex++;
            }

            // --- RENDER DYNAMIC LIST BASED ON CATEGORY & PAGE ---
            float yOffset = 175;
            int itemsPerPage = 11;

            if (currentCategory == 0)
            {
                RenderList(movementMods, movementStates, ref yOffset, itemsPerPage);
            }
            else if (currentCategory == 1)
            {
                RenderList(visualMods, visualStates, ref yOffset, itemsPerPage);
            }
            else if (currentCategory == 2)
            {
                RenderList(gunMods, gunStates, ref yOffset, itemsPerPage);
            }
            else if (currentCategory == 3)
            {
                RenderList(miscSafetyMods, miscSafetyStates, ref yOffset, itemsPerPage);
            }
        }

        private void RenderList(string[] mods, bool[] states, ref float yOffset, int maxItems)
        {
            int startIndex = pageIndex * maxItems;
            if (startIndex >= mods.Length)
            {
                pageIndex = 0;
                startIndex = 0;
            }

            int endIndex = Math.Min(startIndex + maxItems, mods.Length);

            for (int i = startIndex; i < endIndex; i++)
            {
                string status = states[i] ? "[ON]" : "[OFF]";
                if (GUI.Button(new Rect(60, yOffset, 318, 24), $"{mods[i]} {status}", blueButtonStyle))
                {
                    states[i] = !states[i];
                    if (currentCategory == 0 && i == 0) speedBoostMultiplier = (speedBoostMultiplier == 1.6f) ? 2.5f : 1.6f;
                    if (currentCategory == 0 && i == 1) flySpeed = (flySpeed == 12f) ? 25f : 12f;
                }
                yOffset += 27;
            }
        }
    }
}
