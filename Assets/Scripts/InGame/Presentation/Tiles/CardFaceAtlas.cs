using CardJong.InGame.Cards;
using UnityEngine;

namespace CardJong.InGame.Presentation.Tiles
{
    /// <summary>
    /// 牌面のイラストをまとめた 1 枚のテクスチャと、その並び方。
    /// カード 1 枚がアトラスのどのセルかを UV 矩形で返す。
    /// </summary>
    /// <remarks>
    /// 208 枚の牌が同じマテリアルを共有できるよう、イラストは 1 枚のアトラスに並べ、
    /// 牌ごとの違いは <see cref="MahjongTileView"/> が MaterialPropertyBlock で渡す UV 矩形だけにしている。
    /// </remarks>
    [CreateAssetMenu(menuName = "CardJong/Card Face Atlas", fileName = "CardFaceAtlas")]
    public sealed class CardFaceAtlas : ScriptableObject
    {
        /// <summary>アトラスが無いときに使う、テクスチャ全面を指す矩形。</summary>
        public static Vector4 FullRect => new(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("牌面のイラストを並べたテクスチャ。")]
        private Texture2D _texture;

        [SerializeField]
        [Tooltip("横方向のセル数。ランク A〜K の 13。")]
        private int _columns = 13;

        [SerializeField]
        [Tooltip("縦方向のセル数。マーク 4 種。")]
        private int _rows = 4;

        [SerializeField]
        [Tooltip("上の行から順に、どのマークを割り当てるか。")]
        private Suit[] _suitRowOrder = { Suit.Spade, Suit.Heart, Suit.Diamond, Suit.Club };

        /// <summary>牌面のイラストを並べたテクスチャ。</summary>
        public Texture2D Texture => _texture;

        /// <summary>横方向のセル数。</summary>
        public int Columns => _columns;

        /// <summary>縦方向のセル数。</summary>
        public int Rows => _rows;

        /// <summary>カード 1 枚分のセルを UV 矩形（x, y, 幅, 高さ）で返す。</summary>
        public Vector4 GetFaceRect(Card card)
        {
            if (_columns <= 0 || _rows <= 0) return FullRect;

            var column = Mathf.Clamp((int)card.Rank - 1, 0, _columns - 1);
            var row = Mathf.Clamp(RowOf(card.Suit), 0, _rows - 1);
            var cellWidth = 1f / _columns;
            var cellHeight = 1f / _rows;

            // テクスチャの V は下が 0 なので、上の行から数えた row を下基準に読み替える。
            return new Vector4(column * cellWidth, 1f - (row + 1) * cellHeight, cellWidth, cellHeight);
        }

        private int RowOf(Suit suit)
        {
            if (_suitRowOrder == null) return 0;

            for (var i = 0; i < _suitRowOrder.Length; i++)
            {
                if (_suitRowOrder[i] == suit) return i;
            }

            return 0;
        }

#if UNITY_EDITOR
        /// <summary>アトラス生成ツールから中身を書き込む。エディタ専用。</summary>
        public void EditorConfigure(Texture2D texture, int columns, int rows, Suit[] suitRowOrder)
        {
            _texture = texture;
            _columns = columns;
            _rows = rows;
            _suitRowOrder = suitRowOrder;
        }
#endif
    }
}
