using CardJong.InGame.Model;
using UnityEngine;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// 1 席ぶんの名札。持ち点と、親・リーチ・手番かどうかを出す。
    /// 手牌や河は 3D の卓が見せるので、ここは文字情報だけを持つ。
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
        private string _relationLabel;

        public int Seat { get; private set; }

        public void Build(HudUiFactory factory, int seat, string relationLabel, Vector2 anchor, Vector2 position)
        {
            Seat = seat;
            _relationLabel = relationLabel;

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

        public void Refresh(PlayerModel player, bool isDealer, bool isCurrent)
        {
            var dealerMark = isDealer ? "  【親】" : string.Empty;
            var riichiMark = player.Status.IsRiichi ? "  リーチ" : string.Empty;

            _nameText.text = $"{_relationLabel}  seat{player.Seat}{dealerMark}{riichiMark}";
            _nameText.color = player.Status.IsRiichi ? RiichiColor : Color.white;
            _scoreText.text = $"{player.Score.Points.CurrentValue:N0} 点";
            _background.color = isCurrent ? CurrentColor : IdleColor;
        }
    }
}
