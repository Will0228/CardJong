using System;
using CardJong.InGame.Cards;
using CardJong.InGame.Presentation.Tiles;
using UnityEngine;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// HUD 上のカード 1 枚。牌面のアトラスから該当するセルだけを切り出して貼る。
    /// </summary>
    /// <remarks>
    /// 3D の <see cref="MahjongTileView"/> と同じ <see cref="CardFaceAtlas"/> を見るので、
    /// 牌面のイラストを差し替えれば HUD の表示もそのまま追従する。
    /// </remarks>
    public sealed class CardView : MonoBehaviour
    {
        private static readonly Color FrameColor = new(0.93f, 0.92f, 0.88f);
        private static readonly Color SelectableFrameColor = new(1f, 0.87f, 0.42f);
        private static readonly Color BackColor = new(0.16f, 0.30f, 0.24f);

        private const float FrameThickness = 3f;

        private CardFaceAtlas _atlas;
        private Image _frame;
        private RawImage _face;
        private Button _button;
        private Action<Card> _onClicked;

        /// <summary>表示中のカード。裏向きのときは null。</summary>
        public Card Card { get; private set; }

        public void Build(HudUiFactory factory, CardFaceAtlas atlas, Vector2 size)
        {
            _atlas = atlas;

            HudUiFactory.SetFixedSize((RectTransform)transform, size.x, size.y);

            _frame = gameObject.AddComponent<Image>();
            _frame.color = FrameColor;

            var faceRect = factory.CreateRect("Face", transform);
            HudUiFactory.Stretch(faceRect);
            faceRect.offsetMin = new Vector2(FrameThickness, FrameThickness);
            faceRect.offsetMax = new Vector2(-FrameThickness, -FrameThickness);

            _face = faceRect.gameObject.AddComponent<RawImage>();
            _face.texture = atlas != null ? atlas.Texture : null;
            _face.raycastTarget = false;

            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = _frame;
            _button.transition = Selectable.Transition.None;
            _button.interactable = false;
            _button.onClick.AddListener(RaiseClicked);
        }

        /// <summary>表向きにして中身を差し替える。</summary>
        public void ShowFace(Card card)
        {
            Card = card;
            _face.enabled = _atlas != null && _atlas.Texture != null;
            _frame.color = FrameColor;

            if (_atlas == null) return;

            var faceRect = _atlas.GetFaceRect(card);
            _face.uvRect = new Rect(faceRect.x, faceRect.y, faceRect.z, faceRect.w);
        }

        /// <summary>裏向きにする。他家の手札のように中身を見せない札に使う。</summary>
        public void ShowBack()
        {
            Card = null;
            _face.enabled = false;
            _frame.color = BackColor;
        }

        /// <summary>
        /// クリックを受け付けるかを切り替える。<paramref name="onClicked"/> が null なら受け付けない。
        /// </summary>
        public void SetClickHandler(Action<Card> onClicked)
        {
            _onClicked = onClicked;
            _button.interactable = onClicked != null;
            _frame.color = onClicked != null ? SelectableFrameColor : FrameColor;
        }

        private void RaiseClicked()
        {
            if (Card == null) return;

            _onClicked?.Invoke(Card);
        }
    }
}
