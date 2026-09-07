using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// 卓に重ねる HUD の窓口。局数・山の残り・ドラ・名札はモデルから自分で拾うので、
    /// 外から渡すのは「今なにを選ばせるか」と「画面いっぱいの案内」だけ。
    /// </summary>
    public interface IHudPresenter
    {
        /// <summary>名札を席数ぶん組み、モデルの購読を始める。席数が決まった時点で呼ぶ。</summary>
        void Initialize();

        /// <summary>
        /// 選択を促す表示を出す。出し直すと促し文とボタンだけが差し替わり、
        /// 制限時間のバーは動いたままになる。
        /// </summary>
        void ShowDecision(string prompt, IReadOnlyList<ActionButtonSpec> actions);

        /// <summary>制限時間のバーを動かし始める。0 以下なら出さない。</summary>
        void StartTimer(float seconds);

        /// <summary>選択の表示を消す。</summary>
        void CloseDecision();

        /// <summary>画面いっぱいの案内を出し、<paramref name="seconds"/> だけ見せてから消す。</summary>
        UniTask ShowNoticeAsync(string message, float seconds, CancellationToken cancellationToken);
    }
}
