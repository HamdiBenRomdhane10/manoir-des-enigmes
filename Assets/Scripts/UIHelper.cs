using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Petites fonctions utilitaires pour créer l'interface (UI) par code.
public static class UIHelper
{
    static Font font;
    public static Font DefaultFont
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    public static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Responsive : l'interface s'adapte à la taille de l'écran
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
        return canvas;
    }

    public static RectTransform CreateFullScreenPanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    public static RectTransform CreatePanel(Transform parent, string name, Color color, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return rt;
    }

    public static Text CreateText(Transform parent, string content, int fontSize, TextAnchor align,
                                  Vector2 anchor, Vector2 pos, Vector2 boxSize, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.sizeDelta = boxSize;
        rt.anchoredPosition = pos;

        var t = go.GetComponent<Text>();
        t.font = DefaultFont;
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 pos,
                                      UnityAction onClick, Color? color = null, Vector2? anchor = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = a;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.color = color ?? new Color(0.25f, 0.45f, 0.85f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => AudioManager.Get().PlayClick());   // son de clic
        btn.onClick.AddListener(onClick);

        CreateText(go.transform, label, 40, TextAnchor.MiddleCenter,
                   new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white);
        return btn;
    }

    public static InputField CreateInputField(Transform parent, Vector2 size, Vector2 pos)
    {
        var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.color = Color.white;

        var textBox = new Vector2(size.x - 30f, size.y);
        var text = CreateText(go.transform, "", 40, TextAnchor.MiddleLeft,
                              new Vector2(0.5f, 0.5f), Vector2.zero, textBox, Color.black);
        var placeholder = CreateText(go.transform, "Votre réponse...", 40, TextAnchor.MiddleLeft,
                                     new Vector2(0.5f, 0.5f), Vector2.zero, textBox, new Color(0.45f, 0.45f, 0.45f));
        placeholder.fontStyle = FontStyle.Italic;

        var input = go.GetComponent<InputField>();
        input.targetGraphic = img;
        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }
}
