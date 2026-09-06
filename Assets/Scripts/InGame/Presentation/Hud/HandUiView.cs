using System;
using System.Collections.Generic;
using CardJong.InGame.Cards;
using UnityEngine;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>手牌 1 枚ぶんの表示値。ドラかどうかの判定は Presenter が済ませて渡す。</summary>
    public record HandTile(Card Card, bool IsDora);

    /// <summary>
    /// 自分の手牌を画面下に並べる。卓の上には自分の手牌を置かず、ここだけで見せる。
    /// </summary>
    /// <remarks>
    /// レイアウトグループを使わず自分で座標を決めているのは、ツモ牌だけ間を空けたいのと、
    /// カーソルで牌を持ち上げてもレイアウトが崩れないようにするため。
    /// </remarks>
    [AddComponentMenu("CardJong/Hand UI View")]
    public sealed class HandUiView : MonoBehaviour
    {
        /// <summary>牌 1 枚の大きさ。</summary>
        public static Vector2 TileSize => new(96f, 128f);

        /// <summary>手牌全体を収める帯の高さ。持ち上げたぶんが切れないよう牌より高くする。</summary>
        public static float AreaHeight => TileSize.y + 40f;

        private const float TileSpacing = 4f;

        /// <summary>ツモ牌の手前に空ける幅。</summary>
        private const float DrawnTileGap = 28f;

        private readonly List<MahjongTileUiView> _tiles = new();

        private MahjongTileUiView _tilePrefab;
        private bool _isInteractable;

        /// <summary>牌が押されたときに、その牌を流す。</summary>
        public event Action<Card> TileClicked;

        public void Build(MahjongTileUiView tilePrefab, Vector2 anchoredPosition)
        {
            _tilePrefab = tilePrefab;

            HudUiFactory.Anchor(
                (RectTransform)transform,
                new Vector2(0.5f, 0f),
                new Vector2(0f, AreaHeight),
                anchoredPosition);
        }

        /// <summary>渡された手牌で並べ直す。<paramref name="hasDrawnTile"/> なら末尾の 1 枚を離して置く。</summary>
        public void Refresh(IReadOnlyList<HandTile> tiles, bool hasDrawnTile)
        {
            EnsureTiles(tiles.Count);

            var step = TileSize.x + TileSpacing;
            var totalWidth = tiles.Count * step - TileSpacing + (hasDrawnTile ? DrawnTileGap : 0f);
            var left = -totalWidth * 0.5f + TileSize.x * 0.5f;

            for (var i = 0; i < tiles.Count; i++)
            {
                var gap = hasDrawnTile && i == tiles.Count - 1 ? DrawnTileGap : 0f;
                var tile = _tiles[i];

                ((RectTransform)tile.transform).anchoredPosition = new Vector2(left + i * step + gap, 0f);
                tile.SetCard(tiles[i].Card);
                tile.SetDora(tiles[i].IsDora);
                tile.SetInteractable(_isInteractable);
            }
        }

        /// <summary>牌を押せる状態にするか。手番でないあいだは反応させない。</summary>
        public void SetInteractable(bool value)
        {
            _isInteractable = value;

            for (var i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].SetInteractable(value);
            }
        }

        private void EnsureTiles(int count)
        {
            while (_tiles.Count < count)
            {
                var tile = Instantiate(_tilePrefab, transform);
                var rect = (RectTransform)tile.transform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = TileSize;

                tile.Clicked += OnTileClicked;
                _tiles.Add(tile);
            }

            for (var i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].gameObject.SetActive(i < count);
            }
        }

        private void OnTileClicked(MahjongTileUiView tile)
        {
            TileClicked?.Invoke(tile.Card);
        }
    }
}
