using System;
using CardJong.InGame.Cards;
using CardJong.InGame.Presentation.Tiles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// UI 上の麻雀牌 1 個。手牌・鳴き・結果表示など、牌を平面で見せたいところで使い回す。
    /// </summary>
    /// <remarks>
    /// 絵柄は 3D の <see cref="MahjongTileView"/> と同じ <see cref="CardFaceAtlas"/> から引くので、
    /// 牌面のイラストを差し替えれば卓の牌と UI の牌が揃って変わる。
    ///
    /// 押したときの反応は Button に任せず、牌を持ち上げる形で返す。Button の
    /// 色変化だと牌の絵柄まで濁ってしまうため。
    /// </remarks>
    [AddComponentMenu("CardJong/Mahjong Tile UI View")]
    public sealed class MahjongTileUiView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        [Tooltip("持ち上げる対象。牌の見た目一式をこの下にぶら下げる。")]
        private RectTransform _visual;

        [SerializeField]
        [Tooltip("牌の胴体（象牙色の部分）。")]
        private Image _body;

        [SerializeField]
        [Tooltip("牌面の白い部分。裏向きのときは隠す。")]
        private Image _face;

        [SerializeField]
        [Tooltip("アトラスから切り出した絵柄。")]
        private RawImage _pattern;

        [SerializeField]
        [Tooltip("ドラのときだけ出す金の縁。")]
        private Image _doraRim;

        [SerializeField]
        [Tooltip("押下を受けるボタン。牌全体で受けたいので根本に置く。")]
        private Button _button;

        [SerializeField]
        [Tooltip("牌面のイラストを並べたアトラス。")]
        private CardFaceAtlas _faceAtlas;

        [SerializeField]
        [Min(0f)]
        [Tooltip("カーソルを乗せたときに持ち上げる高さ。")]
        private float _hoverLift = 20f;

        /// <summary>表示中の牌。裏向きのときは null。</summary>
        public Card Card { get; private set; }

        /// <summary>押されたときに自分を流す。</summary>
        public event Action<MahjongTileUiView> Clicked;

        /// <summary>表向きにして絵柄を差し替える。</summary>
        public void SetCard(Card card)
        {
            Card = card;
            _face.enabled = true;

            var hasAtlas = _faceAtlas != null && _faceAtlas.Texture != null;
            _pattern.enabled = hasAtlas;
            if (!hasAtlas) return;

            _pattern.texture = _faceAtlas.Texture;
            var faceRect = _faceAtlas.GetFaceRect(card);
            _pattern.uvRect = new Rect(faceRect.x, faceRect.y, faceRect.z, faceRect.w);
        }

        /// <summary>裏向きにする。中身を見せない牌に使う。</summary>
        public void ShowBack()
        {
            Card = null;
            _face.enabled = false;
            _pattern.enabled = false;
        }

        /// <summary>ドラとして縁を光らせるか。</summary>
        public void SetDora(bool isDora)
        {
            _doraRim.enabled = isDora;
        }

        /// <summary>押せる状態にするか。押せないあいだは持ち上げも戻す。</summary>
        public void SetInteractable(bool value)
        {
            _button.interactable = value;
            if (!value) Lift(0f);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            Lift(_hoverLift);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            Lift(0f);
        }

        private void Awake()
        {
            _button.onClick.AddListener(RaiseClicked);
        }

        private void OnDisable()
        {
            // 押せないまま非表示になると持ち上がったまま残るので、戻しておく。
            Lift(0f);
        }

        private void RaiseClicked()
        {
            if (Card == null) return;

            Clicked?.Invoke(this);
        }

        private void Lift(float height)
        {
            _visual.anchoredPosition = new Vector2(0f, height);
        }
    }
}
