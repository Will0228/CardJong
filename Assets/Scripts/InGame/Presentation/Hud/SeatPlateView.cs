using UnityEngine;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>名札に出す 1 席ぶんの値。文字は Presenter が組み立てて渡す。</summary>
    public record SeatPlateState(string Name, string Score, bool IsRiichi, bool IsCurrent);

    /// <summary>
    /// 1 席ぶんの名札。渡された文字とフラグを見た目に写すだけで、誰の席かは知らない。
    /// </summary>
    public sealed class SeatPlateView : MonoBehaviour
    {
        /// <summary>名札 1 枚の大きさ。</summary>
        public static Vector2 PlateSize => new(300f, 76f);

        private static readonly Color IdleColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color CurrentColor = new(0.13f, 0.36f, 0.22f, 0.88f);
        private static readonly Color RiichiColor = new(0.96f, 0.56f, 0.34f);

        private Image _background;
        private Text _nameText;
        private Text _scoreText;

        public void Build(HudUiFactory factory, Vector2 anchor, Vector2 position)
        {
            HudUiFactory.Anchor((RectTransform)transform, anchor, PlateSize, position);

            _background = gameObject.AddComponent<Image>();
            _background.color = IdleColor;

            var layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 6, 6);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            _nameText = factory.CreateText("Name", transform, 22, TextAnchor.MiddleLeft, Color.white);
            HudUiFactory.SetFixedSize(_nameText.rectTransform, -1f, 26f);

            _scoreText = factory.CreateText("Score", transform, 28, TextAnchor.MiddleLeft, new Color(0.88f, 0.92f, 0.86f));
            HudUiFactory.SetFixedSize(_scoreText.rectTransform, -1f, 32f);
        }

        public void Refresh(SeatPlateState state)
        {
            _nameText.text = state.Name;
            _nameText.color = state.IsRiichi ? RiichiColor : Color.white;
            _scoreText.text = state.Score;
            _background.color = state.IsCurrent ? CurrentColor : IdleColor;
        }
    }
}
