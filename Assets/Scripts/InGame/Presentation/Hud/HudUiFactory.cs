using UnityEngine;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// HUD の uGUI 階層を組み立てる。シーンに置くのは空の GameObject 1 つだけにして、
    /// Canvas から下は実行時にここで作る。
    /// </summary>
    /// <remarks>
    /// レイアウトを手で組んだシーンにすると、席数やカード枚数が変わるたびに
    /// シーン側を触ることになる。表示物の数がモデル次第で決まる HUD なので、
    /// 階層ごとコードに寄せて 1 か所で完結させている。
    /// </remarks>
    public sealed class HudUiFactory
    {
        /// <summary>日本語のラベルを出すために借りる OS フォント。前から順に探す。</summary>
        private static readonly string[] FontCandidates =
        {
            "Yu Gothic UI",
            "Yu Gothic",
            "Meiryo",
            "MS Gothic",
            "Hiragino Sans",
            "Noto Sans CJK JP",
        };

        private const int FontAtlasSize = 32;

        private readonly Font _font;

        public HudUiFactory()
        {
            // OS のフォントが 1 つも見つからない環境では、英数字だけの組み込みフォントに落ちる。
            var osFont = Font.CreateDynamicFontFromOSFont(FontCandidates, FontAtlasSize);
            _font = osFont != null ? osFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>親いっぱいに広がるよう四隅を留める。</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// レイアウトグループに対して希望サイズを伝える。
        /// 最小サイズも同じ値にして、並べる枚数が増えても 1 枚あたりが潰れないようにする。
        /// </summary>
        public static LayoutElement SetFixedSize(RectTransform rect, float width, float height)
        {
            var element = rect.GetComponent<LayoutElement>();
            if (element == null) element = rect.gameObject.AddComponent<LayoutElement>();

            element.minWidth = width;
            element.minHeight = height;
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
            return element;
        }

        /// <summary>画面の一点に、決まった大きさで留める。レイアウトグループの外に置く箱に使う。</summary>
        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        /// <summary>高さだけ固定し、横は親の余りいっぱいに広げる。</summary>
        public static LayoutElement SetFlexibleWidth(RectTransform rect, float height)
        {
            var element = SetFixedSize(rect, -1f, height);
            element.minWidth = -1f;
            element.preferredWidth = -1f;
            element.flexibleWidth = 1f;
            return element;
        }

        public Canvas CreateCanvas(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            rect.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public Image CreateImage(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment, Color color)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// ラベルの長さに合わせて横幅が決まるボタンを作る。
        /// 「チー[♠3 ♠4]」のように選択肢によって文字数が変わるため、幅は指定しない。
        /// </summary>
        public Button CreateButton(string name, Transform parent, string label, Color color, int fontSize, float height)
        {
            var image = CreateImage(name, parent, color);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            // 横幅は、この HorizontalLayoutGroup が中の Text から算出した希望幅がそのまま使われる。
            var layout = image.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var element = image.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            var text = CreateText("Label", image.transform, fontSize, TextAnchor.MiddleCenter, Color.white);
            text.text = label;
            return button;
        }

        /// <summary>子を横に並べる箱を作る。</summary>
        public RectTransform CreateRow(string name, Transform parent, float spacing, TextAnchor alignment)
        {
            var rect = CreateRect(name, parent);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return rect;
        }

        /// <summary>子を縦に並べる箱を作る。</summary>
        public RectTransform CreateColumn(string name, Transform parent, float spacing, TextAnchor alignment)
        {
            var rect = CreateRect(name, parent);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return rect;
        }

    }
}
