using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System;

namespace TvMenu
{
    public class MenuStub
    {
        private GameObject menuObject;
        private Canvas menuCanvas;

        public void BuildMenu(Action<int> onCategoryChanged, Action<int> onModToggled, Action onThemeToggled)
        {
            DestroyMenu();

            menuObject = new GameObject("TvMenu_WristMenu");
            menuCanvas = menuObject.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.WorldSpace;

            var scaler = menuObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            menuObject.AddComponent<GraphicRaycaster>();

            RectTransform rt = menuObject.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, 1300);
            menuObject.transform.localScale = Vector3.one * 0.0007f;

            // Background Panel
            UIStub.CreatePanel(menuObject.transform, "Background", new Vector2(800, 1300), Vector2.zero, new Color(0.02f, 0.06f, 0.15f, 0.95f));

            // Title & Version (White text)
            UIStub.CreateLabel(menuObject.transform, "TvMenu Beta", 30, new Vector2(0, 560), Color.white);
            UIStub.CreateLabel(menuObject.transform, "Version Beta 1.0.0", 18, new Vector2(0, 510), Color.white);

            // Category Buttons
            UIStub.CreateButton(menuObject.transform, "MOVEMENT", new Vector2(-180, 430), new Vector2(170, 46), () => onCategoryChanged(0), new Color(0.08f, 0.2f, 0.45f));
            UIStub.CreateButton(menuObject.transform, "VISUAL", new Vector2(0, 430), new Vector2(170, 46), () => onCategoryChanged(1), new Color(0.08f, 0.2f, 0.45f));
            UIStub.CreateButton(menuObject.transform, "GUNS", new Vector2(180, 430), new Vector2(170, 46), () => onCategoryChanged(2), new Color(0.08f, 0.2f, 0.45f));
            UIStub.CreateButton(menuObject.transform, "MISC", new Vector2(0, 360), new Vector2(360, 46), () => onCategoryChanged(3), new Color(0.08f, 0.2f, 0.45f));

            // Blue Theme Toggle Button
            string blueLabel = Plugin.colorBlueEnabled ? "BLUE THEME: ON" : "BLUE THEME: OFF";
            UIStub.CreateButton(menuObject.transform, blueLabel, new Vector2(0, 290), new Vector2(360, 46), () => onThemeToggled(), new Color(0.05f, 0.3f, 0.7f));

            // Disconnect & Server Hop
            UIStub.CreateButton(menuObject.transform, "DISCONNECT", new Vector2(-130, -560), new Vector2(170, 46), () => {
                if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
            }, new Color(0.5f, 0.1f, 0.1f));

            UIStub.CreateButton(menuObject.transform, "SERVER HOP", new Vector2(130, -560), new Vector2(170, 46), () => {
                if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
            }, new Color(0.1f, 0.4f, 0.1f));

            RefreshMods(onModToggled);
        }

        public void RefreshMods(Action<int> onModToggled)
        {
            if (menuObject == null) return;

            foreach (Transform child in menuObject.transform)
            {
                if (child.name.StartsWith("ModBtn_"))
                    GameObject.Destroy(child.gameObject);
            }

            string[] mods = null;
            bool[] states = null;

            if (Plugin.currentCategory == 0) { mods = Plugin.movementMods; states = Plugin.movementStates; }
            else if (Plugin.currentCategory == 1) { mods = Plugin.visualMods; states = Plugin.visualStates; }
            else if (Plugin.currentCategory == 2) { mods = Plugin.gunMods; states = Plugin.gunStates; }
            else { mods = Plugin.miscSafetyMods; states = Plugin.miscSafetyStates; }

            float startY = 210f;
            for (int i = 0; i < mods.Length; i++)
            {
                int index = i;
                string modName = mods[i];
                bool isNW = modName.Contains("[NW]");
                bool isOn = states[i];

                string label = modName + (isNW ? "  [LOCKED]" : (isOn ? "  [ON]" : "  [OFF]"));
                Color col;

                if (isNW) col = new Color(0.25f, 0.25f, 0.25f);
                else if (isOn) col = new Color(0.1f, 0.6f, 0.3f);
                else col = new Color(0.15f, 0.25f, 0.45f);

                UIStub.CreateButton(menuObject.transform, label, new Vector2(0, startY - (i * 52)), new Vector2(360, 46), () =>
                {
                    if (mods[index].Contains("[NW]")) return;
                    onModToggled(index);
                }, col);
                
                // Rename object for tracking cleanup
                Transform lastChild = menuObject.transform.GetChild(menuObject.transform.childCount - 1);
                lastChild.name = "ModBtn_" + i;
            }
        }

        public void UpdatePosition(Transform leftHand)
        {
            if (menuObject != null && leftHand != null)
            {
                menuObject.transform.position = leftHand.position + leftHand.up * 0.12f + leftHand.forward * 0.08f;
                menuObject.transform.rotation = leftHand.rotation * Quaternion.Euler(0, 180f, 0);
            }
        }

        public void DestroyMenu()
        {
            if (menuObject != null)
            {
                GameObject.Destroy(menuObject);
                menuObject = null;
            }
        }

        public bool IsOpen() => menuObject != null;
    }
}
