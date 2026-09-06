using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation.Tiles;
using UnityEngine;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// 1 席ぶんの表示。持ち点・鳴き・河をまとめて出す。
    /// 自分の手札だけは大きく出したいので、ここではなく <see cref="InGameHudView"/> が持つ。
    /// </summary>
    public sealed class PlayerAreaView : MonoBehaviour
    {
        /// <summary>河と鳴きに使うカードの大きさ。</summary>
        public static Vector2 SmallCardSize => new(38f, 53f);

        private static readonly Color IdleColor = new(0.10f, 0.14f, 0.12f, 0.72f);
        private static readonly Color CurrentColor = new(0.14f, 0.30f, 0.20f, 0.90f);
        private static readonly Color RiichiColor = new(0.90f, 0.42f, 0.30f);

        private const float AreaHeight = 108f;
        private const float InfoWidth = 200f;
        private const float MeldsWidth = 340f;

        private readonly List<CardView> _meldCards = new();
        private readonly List<CardView> _discardCards = new();

        private HudUiFactory _factory;
        private CardFaceAtlas _atlas;
        private Image _background;
        private Text _headerText;
        private Text _scoreText;
        private RectTransform _meldsRoot;
        private RectTransform _discardsRoot;
        private string _relationLabel;

        public int Seat { get; private set; }

        public void Build(HudUiFactory factory, CardFaceAtlas atlas, int seat, string relationLabel)
        {
            _factory = factory;
            _atlas = atlas;
            Seat = seat;
            _relationLabel = relationLabel;

            HudUiFactory.SetFlexibleWidth((RectTransform)transform, AreaHeight);

            _background = gameObject.AddComponent<Image>();
            _background.color = IdleColor;

            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            BuildInfo();
            _meldsRoot = BuildCardRow("Melds", MeldsWidth);
            _discardsRoot = BuildCardRow("Discards", -1f);
        }

        /// <summary>モデルの現在値で表示を作り直す。</summary>
        public void Refresh(PlayerModel player, bool isDealer, bool isCurrent)
        {
            var dealerMark = isDealer ? "  【親】" : string.Empty;
            var riichiMark = player.Status.IsRiichi ? "  リーチ" : string.Empty;

            _headerText.text = $"{_relationLabel}  seat{player.Seat}{dealerMark}{riichiMark}";
            _headerText.color = player.Status.IsRiichi ? RiichiColor : Color.white;
            _scoreText.text = $"{player.Score.Points.CurrentValue:N0} 点   手牌 {player.Cards.ConcealedCards.Count}";
            _background.color = isCurrent ? CurrentColor : IdleColor;

            RefreshMelds(player.Cards.Melds);
            RefreshCards(_discardCards, _discardsRoot, player.Cards.Discards);
        }

        private void BuildInfo()
        {
            var info = _factory.CreateColumn("Info", transform, 4f, TextAnchor.MiddleLeft);
            HudUiFactory.SetFixedSize(info, InfoWidth, AreaHeight - 12f);

            _headerText = _factory.CreateText("Header", info, 20, TextAnchor.MiddleLeft, Color.white);
            HudUiFactory.SetFixedSize(_headerText.rectTransform, InfoWidth, 26f);

            _scoreText = _factory.CreateText("Score", info, 18, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.82f));
            HudUiFactory.SetFixedSize(_scoreText.rectTransform, InfoWidth, 24f);
        }

        private RectTransform BuildCardRow(string name, float width)
        {
            var row = _factory.CreateRow(name, transform, 3f, TextAnchor.MiddleLeft);
            if (width > 0f)
            {
                HudUiFactory.SetFixedSize(row, width, SmallCardSize.y);
            }
            else
            {
                HudUiFactory.SetFlexibleWidth(row, SmallCardSize.y);
            }

            return row;
        }

        /// <summary>鳴いた組を、組の区切りが分かるよう順に並べる。</summary>
        private void RefreshMelds(IReadOnlyList<Meld> melds)
        {
            var cards = new List<Card>();
            for (var i = 0; i < melds.Count; i++)
            {
                cards.AddRange(melds[i].Cards);
            }

            RefreshCards(_meldCards, _meldsRoot, cards);
        }

        private void RefreshCards(List<CardView> views, RectTransform root, IReadOnlyList<Card> cards)
        {
            _factory.EnsureCardViews(views, root, _atlas, cards.Count, SmallCardSize);

            for (var i = 0; i < cards.Count; i++)
            {
                views[i].ShowFace(cards[i]);
            }
        }
    }
}
