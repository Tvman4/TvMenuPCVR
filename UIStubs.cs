using UnityEngine;
using UnityEngine.UI;
using System;

namespace TvMenu
{
    public static class UIStub
    {
        public static GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            Image img = panel.AddComponent<Image>();
            img.color = color;

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;

            return panel;
        }

        public static Text CreateLabel(Transform parent, string content, int fontSize, Vector2 pos, Color textColor)
        {
            GameObject obj = new GameObject("UIStub_Label");
            obj.transform.SetParent(parent, false);

            Text txt = obj.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.color = textColor;
            txt.alignment = TextAnchor.MiddleCenter;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700, 50);
            rt.anchoredPosition = pos;

            return txt;
        }

        public static Button CreateButton(Transform parent, string labelText, Vector2 pos, Vector2 size, Action onClick, Color normalColor)
        {
            GameObject btnObj = new GameObject("UIStub_Button_" + labelText);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = normalColor;

            Button btn = btnObj.AddComponent<Button>();
            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick());
            }

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            Text txt = textObj.AddComponent<Text>();
            txt.text = labelText;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 18;
            txt.color = Color.white; // Strictly enforced white text
            txt.alignment = TextAnchor.MiddleCenter;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
