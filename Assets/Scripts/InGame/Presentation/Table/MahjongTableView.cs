using System.Collections.Generic;
using CardJong.InGame.Cards;
using CardJong.InGame.Presentation.Tiles;
using UnityEngine;

namespace CardJong.InGame.Presentation.Table
{
    /// <summary>卓に置く牌 1 枚ぶんの表示値。ドラかどうかの判定は Presenter が済ませて渡す。</summary>
    public record TableTile(Card Card, bool IsDora);

    /// <summary>
    /// 卓の 3D 表示。渡された牌を卓の位置ごとに並べるだけで、誰の何かは知らない。
    /// </summary>
    /// <remarks>
    /// 席ではなく卓の位置（slot）で受け取る。誰が手前に座るかを決めるのは Presenter の仕事で、
    /// ここは「手前の河に 6 枚」といった見た目の並べ方だけを持つ。
    ///
    /// 牌は位置ごとに使い回す。局をまたぐたびに Instantiate し直すと 200 個近い生成が走るため、
    /// 一度作ったものは非表示にして残し、必要な枚数だけ表示する。
    /// </remarks>
    [AddComponentMenu("CardJong/Mahjong Table View")]
    public sealed class MahjongTableView : MonoBehaviour
    {
        /// <summary>卓の 1 か所ぶんの牌置き場。</summary>
        private sealed class SlotTiles
        {
            public SlotTiles(int slot, Transform root)
            {
                Slot = slot;
                Root = root;
            }

            /// <summary>卓のどの位置か。0 が手前。</summary>
            public int Slot { get; }

            public Transform Root { get; }

            public List<MahjongTileView> Hand { get; } = new();

            public List<MahjongTileView> Discards { get; } = new();

            public List<MahjongTileView> Melds { get; } = new();
        }

        [SerializeField]
        [Tooltip("並べる牌のプレハブ。")]
        private MahjongTileView _tilePrefab;

        private readonly TableLayout _layout = TableLayout.Standard;
        private readonly List<SlotTiles> _slots = new();
        private readonly List<MahjongTileView> _doraTiles = new();

        /// <summary>席数が決まったあとに呼ぶ。ここで位置ぶんの置き場を作る。</summary>
        public void Initialize(int slotCount)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                Destroy(_slots[i].Root.gameObject);
            }

            _slots.Clear();

            for (var slot = 0; slot < slotCount; slot++)
            {
                var root = new GameObject($"Slot{slot}").transform;
                root.SetParent(transform, false);
                _slots.Add(new SlotTiles(slot, root));
            }
        }

        /// <summary>
        /// 立てた手牌を並べる。牌の面は座っている側を向くので、卓の外からは背しか見えない。
        /// 中身は見えないため枚数だけ受け取る。
        /// </summary>
        public void SetHand(int slot, int count)
        {
            var tiles = _slots[slot];
            EnsureTiles(tiles.Hand, tiles.Root, count);

            for (var i = 0; i < count; i++)
            {
                Place(tiles.Hand[i], _layout.HandTile(slot, i, count));
            }
        }

        /// <summary>河を並べる。捨てられた順に渡す。</summary>
        public void SetDiscards(int slot, IReadOnlyList<TableTile> discards)
        {
            var tiles = _slots[slot];
            EnsureTiles(tiles.Discards, tiles.Root, discards.Count);

            for (var i = 0; i < discards.Count; i++)
            {
                var tile = tiles.Discards[i];
                tile.SetCard(discards[i].Card);
                tile.SetDora(discards[i].IsDora);
                Place(tile, _layout.DiscardTile(slot, i));
            }
        }

        /// <summary>
        /// 鳴いた組を並べる。<paramref name="melds"/> は組の順につないだ牌で、
        /// <paramref name="groupSizes"/> はその組ごとの枚数。組の切れ目に間を空けるのに使う。
        /// </summary>
        public void SetMelds(int slot, IReadOnlyList<TableTile> melds, IReadOnlyList<int> groupSizes)
        {
            var tiles = _slots[slot];
            EnsureTiles(tiles.Melds, tiles.Root, melds.Count);

            var groupIndex = 0;
            var placedInGroup = 0;

            for (var i = 0; i < melds.Count; i++)
            {
                // 組の枚数ぶん置き終わったら次の組へ移る。
                while (groupIndex < groupSizes.Count && placedInGroup >= groupSizes[groupIndex])
                {
                    groupIndex++;
                    placedInGroup = 0;
                }

                var tile = tiles.Melds[i];
                tile.SetCard(melds[i].Card);
                tile.SetDora(melds[i].IsDora);
                Place(tile, _layout.MeldTile(slot, groupIndex, i));
                placedInGroup++;
            }
        }

        /// <summary>卓の中央に置くドラ表示札。</summary>
        public void SetDoraIndicators(IReadOnlyList<Card> indicators)
        {
            EnsureTiles(_doraTiles, transform, indicators.Count);

            for (var i = 0; i < indicators.Count; i++)
            {
                _doraTiles[i].SetCard(indicators[i]);
                Place(_doraTiles[i], _layout.DoraTile(i, indicators.Count));
            }
        }

        private void EnsureTiles(List<MahjongTileView> tiles, Transform parent, int count)
        {
            while (tiles.Count < count)
            {
                tiles.Add(Instantiate(_tilePrefab, parent));
            }

            for (var i = 0; i < tiles.Count; i++)
            {
                tiles[i].gameObject.SetActive(i < count);
            }
        }

        private void Place(MahjongTileView tile, Pose pose)
        {
            tile.transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
            tile.transform.localScale = Vector3.one * _layout.TileScale;
        }
    }
}
