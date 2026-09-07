using CardJong.InGame.Cards;
using R3;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// 画面下に並ぶ自分の手牌の窓口。並べる中身はモデルから自分で拾うので、
    /// 外から見えるのは「選べる状態にするか」と「どの牌が選ばれたか」だけ。
    /// </summary>
    public interface IHandPresenter
    {
        /// <summary>牌が選ばれた。選べる状態のあいだだけ流れる。</summary>
        Observable<Card> TileSelected { get; }

        /// <summary>手牌の購読を始め、今のモデルの内容で並べる。対局が始まる時点で呼ぶ。</summary>
        void Initialize();

        /// <summary>牌を選べる状態にするか。手番でないあいだは反応させない。</summary>
        void SetSelectable(bool value);
    }
}
