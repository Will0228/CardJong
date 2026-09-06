using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace CardJong.OutGame.Presentation.Home
{
    /// <summary>
    /// ホーム画面。タイトルとゲームスタートのボタンだけを出す。
    /// </summary>
    /// <remarks>
    /// InGame の HUD と同じく、シーンに置くのは空の GameObject 1 つで、
    /// Canvas から下は実行時にここで作る。画面が増えて組み立てが重複してきたら、
    /// InGame の HudUiFactory と合わせて共通の生成側へ切り出す。
    /// ボタンを押せるかどうかや完了待ちといったデータは <see cref="HomeModel"/> が持ち、
    /// ここは見た目の組み立てとボタンの取り次ぎだけに専念する。
    /// </remarks>
    [AddComponentMenu("CardJong/Home View")]
    public sealed class HomeView : MonoBehaviour
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

        private static readonly Color BackgroundColor = new(0.045f, 0.07f, 0.06f, 1f);
        private static readonly Color TitleColor = new(0.96f, 0.94f, 0.86f);
        private static readonly Color CaptionColor = new(0.62f, 0.70f, 0.64f);
        private static readonly Color StartButtonColor = new(0.20f, 0.48f, 0.36f);

        private static readonly Vector2 TitleSize = new(1200f, 180f);
        private static readonly Vector2 CaptionSize = new(1200f, 48f);
        private static readonly Vector2 StartButtonSize = new(440f, 108f);

        private const int FontAtlasSize = 48;

        private readonly CompositeDisposable _subscriptions = new();

        private HomeModel _model;
        private Font _font;
        private Button _startButton;

        [Inject]
        public void Construct(HomeModel model)
        {
            _model = model;

            // OS のフォントが 1 つも見つからない環境では、英数字だけの組み込みフォントに落ちる。
            var osFont = Font.CreateDynamicFontFromOSFont(FontCandidates, FontAtlasSize);
            _font = osFont != null ? osFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildHierarchy();

            // 現在値をすぐ受け取れるので、初期状態の反映もこれで兼ねる。
            _subscriptions.Add(_model.CanStart.Subscribe(canStart => _startButton.interactable = canStart));
        }

        private void OnDestroy() => _subscriptions.Dispose();

        private void OnStartClicked() => _model.RequestStart();

        // ---- 画面の組み立て ----

        private void BuildHierarchy()
        {
            var canvas = CreateCanvas();

            // 卓が透けて見えるよう、背景は敷かない。
            var background = CreateImage("Background", canvas.transform, BackgroundColor);
            Stretch(background.rectTransform);

            var title = CreateText("Title", canvas.transform, 120, TitleColor);
            title.text = "CardJong";
            Anchor(title.rectTransform, TitleSize, new Vector2(0f, 240f));

            var caption = CreateText("Caption", canvas.transform, 30, CaptionColor);
            caption.text = "トランプで打つ麻雀";
            Anchor(caption.rectTransform, CaptionSize, new Vector2(0f, 130f));

            _startButton = CreateStartButton(canvas.transform);
            Anchor(_startButton.image.rectTransform, StartButtonSize, new Vector2(0f, -140f));
            _startButton.onClick.AddListener(OnStartClicked);
        }

        private Canvas CreateCanvas()
        {
            var rect = CreateRect("HomeCanvas", transform);
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

        private Button CreateStartButton(Transform parent)
        {
            var image = CreateImage("StartButton", parent, StartButtonColor);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText("Label", image.transform, 42, Color.white);
            label.text = "ゲームスタート";
            Stretch(label.rectTransform);

            return button;
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(string name, Transform parent, int fontSize, Color color)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>画面の中央から見た位置に、決まった大きさで留める。</summary>
        private static void Anchor(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        /// <summary>親いっぱいに広がるよう四隅を留める。</summary>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
