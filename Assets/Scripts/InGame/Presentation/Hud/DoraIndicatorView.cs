using System.Collections.Generic;
using CardJong.InGame.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// ドラ表示札を画面の左上に出す。同じ札は卓の中央にも置いてあるが、卓を引きで写す画角では
    /// 何が出ているのか読めないので、雀魂と同じく手前の UI にも常に出しておく。
    /// </summary>
    /// <remarks>
    /// 表示札がまだ 1 枚もめくられていない局の開始前でも枠は見せたいので、
    /// 足りないぶんは裏向きの牌で埋める。
    /// </remarks>
    [AddComponentMenu("CardJong/Dora Indicator View")]
    public sealed class DoraIndicatorView : MonoBehaviour
    {
        /// <summary>牌 1 枚の大きさ。手牌より小さいが、絵柄は読める程度に取る。</summary>
        public static Vector2 TileSize => new(64f, 86f);

        private static readonly Color PanelColor = new(0f, 0f, 0f, 0.55f);
        private static readonly Color LabelColor = new(0.98f, 0.86f, 0.52f);

        /// <summary>表示札がめくられていなくても見せる枠の数。</summary>
        private const int MinimumSlotCount = 1;

        private const float TileSpacing = 6f;

        private readonly List<MahjongTileUiView> _tiles = new();

        private MahjongTileUiView _tilePrefab;
        private RectTransform _tileRoot;

        /// <summary>左上に留める。<paramref name="anchoredPosition"/> は画面左上からの距離。</summary>
        public void Build(HudUiFactory factory, MahjongTileUiView tilePrefab, Vector2 anchoredPosition)
        {
            _tilePrefab = tilePrefab;

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;

            gameObject.AddComponent<Image>().color = PanelColor;

            var layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // 表示札が増えても左上の角は動かないよう、幅は中身に合わせて広げる。
            var fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var label = factory.CreateText("Label", transform, 22, TextAnchor.MiddleCenter, LabelColor);
            label.text = "ドラ";
            HudUiFactory.SetFixedSize(label.rectTransform, 44f, TileSize.y);

            _tileRoot = factory.CreateRow("Tiles", transform, TileSpacing, TextAnchor.MiddleLeft);
        }

        /// <summary>めくられている表示札で並べ直す。</summary>
        public void Refresh(IReadOnlyList<Card> indicators)
        {
            EnsureTiles(Mathf.Max(indicators.Count, MinimumSlotCount));

            for (var i = 0; i < indicators.Count; i++)
            {
                _tiles[i].SetCard(indicators[i]);
            }

            for (var i = indicators.Count; i < _tiles.Count; i++)
            {
                _tiles[i].ShowBack();
            }
        }

        private void EnsureTiles(int count)
        {
            while (_tiles.Count < count)
            {
                var tile = Instantiate(_tilePrefab, _tileRoot);
                HudUiFactory.SetFixedSize((RectTransform)tile.transform, TileSize.x, TileSize.y);

                // 見せるだけの牌なので、押しても反応しないようにしておく。
                tile.SetInteractable(false);
                tile.SetDora(false);
                _tiles.Add(tile);
            }

            for (var i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].gameObject.SetActive(i < count);
            }
        }
    }
}
