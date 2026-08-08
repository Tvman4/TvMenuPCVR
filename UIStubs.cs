using UnityEngine;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    public class Image : MonoBehaviour
    {
        public Color color;
    }

    public class Text : MonoBehaviour
    {
        public string text;
        public Font font;
        public int fontSize;
        public Color color;
        public TextAnchor alignment;
        public FontStyle fontStyle;
    }

    public class Button : MonoBehaviour
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick = new ButtonClickedEvent();
    }

    public class CanvasScaler : MonoBehaviour
    {
        public float dynamicPixelsPerUnit;
    }

    public class GraphicRaycaster : MonoBehaviour { }
}
