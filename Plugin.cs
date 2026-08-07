using BepInEx;
using UnityEngine;
using UnityEngine.XR;
using Photon.Pun;
using System;
using System.Collections.Generic;

namespace TvMenu
{
    [BepInPlugin("org.tv.gorillatag.tvmenu", "TvMenu", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static bool menuOpen = true;
        public static int currentCategory = 0; // 0: Movement, 1: Visuals, 2: Guns/Fun, 3: Misc/Safety

        // Core Mod Variables
        public static bool speedBoostEnabled = false;
        public static float speedBoostMultiplier = 1.5f;
        public static bool flyEnabled = false;
        public static float flySpeed = 10f;
        public static bool longArmsEnabled = false;
        public static float armLengthMultiplier = 1.3f;
        public static bool antiReportEnabled = true;

        // Categorized lists with status tags
        public static string[] movementMods = { "SpeedBoost [W]", "Change SpeedBoost Speed [W]", "Fly [W]", "Change Fly Speed [W]", "LongArms [W]", "Noclip [X]", "AirJump [W]", "Platform Monkeys [W]", "SpiderMonkey [X]", "Low Gravity [W]" };
        public static bool[] movementStates = new bool[10];

        public static string[] visualMods = { "Fullbright [W]", "FPS Counter [W]", "Third Person [X]", "Custom Skybox [X]", "No Textures [X]", "Ghost Mode [X]", "Player ESP [W]", "Name Tags [W]", "Bone ESP [X]", "Chams [X]" };
        public static bool[] visualStates = new bool[10];

        public static string[] gunMods = { "Kick Gun [WIP]", "Lag Gun [WIP]", "Tag Gun [WIP]", "Auto Tag [WIP]", "Invisibility [X]", "Soundboard Spam [X]" };
        public static bool[] gunStates = new bool[6];

        public static string[] miscSafetyMods = { "Anti-Report [W]", "Disable VR Rig [X]", "Head Spin [W]", "Auto Report Deter [W]", "Swim Anywhere [X]", "Phase Shift [X]", "Fov Changer [W]", "Vibration Control [W]", "FPS Booster [W]", "Fast Load [W]", "Checkpoint Teleport [X]", "Trackball [X]", "Infinite Stutter [X]", "No Clip Fly [X]", "Auto Wall Climb [X]", "Slide Control [W]", "Sticky Hands [X]", "Bouncing Surfaces [W]", "Zero Friction [W]", "Speedometer [W]", "Position Logger [W]", "Server Hopper [W]", "Config Save [W]", "Config Load [W]" };
        public static bool[] miscSafetyStates = new bool[24];

        private void Update()
        {
            // Toggle Menu with Y Button (Left Controller Secondary) or Insert Key
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

                // 3. Fly
                if (movementStates[2] || flyEnabled)
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
                            if (devices.Count > 0)
                            {
                                devices[0].TryGetFeatureValue(CommonUsages.primaryButton, out primaryPressed);
                            }

                            if (primaryPressed || UnityInput.Current.GetKey(KeyCode.Space))
                            {
                                GorillaLocomotion.Player.Instance.transform.position += Camera.main.transform.forward * flySpeed * Time.deltaTime;
                            }
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

                // Anti-Report Logic
                if (miscSafetyStates[0] || antiReportEnabled)
                {
                    try
                    {
                        var list = UnityEngine.Object.FindObjectsOfType<GorillaPlayerScoreboardLine>();
                        foreach (var line in list)
                        {
                            if (line.reportButton != null)
                            {
                                line.reportButton.SetActive(false);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception) { }
        }

        private void OnGUI()
        {
            if (!menuOpen) return;

            GUI.Box(new Rect(50, 50, 340, 550), "TvMenu - Categorized (Y Toggle)");

            // --- PERMANENT TOP UTILITY BUTTONS ---
            if (GUI.Button(new Rect(60, 80, 155, 30), "Disconnect"))
            {
                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.Disconnect();
                }
            }
            if (GUI.Button(new Rect(223, 80, 155, 30), "Server Hop"))
            {
                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.Disconnect();
                }
            }

            // --- CATEGORY SELECTOR TABS ---
            if (GUI.Button(new Rect(60, 120, 75, 25), "Move")) currentCategory = 0;
            if (GUI.Button(new Rect(138, 120, 75, 25), "Visual")) currentCategory = 1;
            if (GUI.Button(new Rect(216, 120, 75, 25), "Guns")) currentCategory = 2;
            if (GUI.Button(new Rect(294, 120, 84, 25), "Misc")) currentCategory = 3;

            // --- RENDER DYNAMIC CATEGORY LIST ---
            float yOffset = 155;

            if (currentCategory == 0)
            {
                for (int i = 0; i < movementMods.Length; i++)
                {
                    string status = movementStates[i] ? "[ON]" : "[OFF]";
                    if (GUI.Button(new Rect(60, yOffset, 318, 24), $"{movementMods[i]} {status}"))
                    {
                        movementStates[i] = !movementStates[i];
                        if (i == 1) speedBoostMultiplier = (speedBoostMultiplier == 1.5f) ? 2.5f : 1.5f;
                        if (i == 3) flySpeed = (flySpeed == 10f) ? 25f : 10f;
                    }
                    yOffset += 27;
                }
            }
            else if (currentCategory == 1)
            {
                for (int i = 0; i < visualMods.Length; i++)
                {
                    string status = visualStates[i] ? "[ON]" : "[OFF]";
                    if (GUI.Button(new Rect(60, yOffset, 318, 24), $"{visualMods[i]} {status}"))
                    {
                        visualStates[i] = !visualStates[i];
                    }
                    yOffset += 27;
                }
            }
            else if (currentCategory == 2)
            {
                for (int i = 0; i < gunMods.Length; i++)
                {
                    string status = gunStates[i] ? "[ON]" : "[OFF]";
                    if (GUI.Button(new Rect(60, yOffset, 318, 24), $"{gunMods[i]} {status}"))
                    {
                        gunStates[i] = !gunStates[i];
                    }
                    yOffset += 27;
                }
            }
            else if (currentCategory == 3)
            {
                for (int i = 0; i < miscSafetyMods.Length; i++)
                {
                    string status = miscSafetyStates[i] ? "[ON]" : "[OFF]";
                    if (GUI.Button(new Rect(60, yOffset, 318, 24), $"{miscSafetyMods[i]} {status}"))
                    {
                        miscSafetyStates[i] = !miscSafetyStates[i];
                    }
                    yOffset += 27;
                    if (yOffset > 530) break;
                }
            }
        }
    }
}
