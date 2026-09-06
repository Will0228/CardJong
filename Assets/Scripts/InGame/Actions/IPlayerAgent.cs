using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Actions
{
    /// <summary>
    /// 席ごとの意思決定者。人間の操作でも CPU でも、State から見た扱いは同じ。
    /// </summary>
    public interface IPlayerAgent
    {
        int Seat { get; }

        /// <summary>自分の手番での行動を決める（行動パターン）。</summary>
        UniTask<TurnAction> DecideTurnActionAsync(TurnDecisionContext context, CancellationToken cancellationToken);

        /// <summary>他家の捨て札に対する宣言を決める（待機パターン）。</summary>
        UniTask<ClaimDeclaration> DecideClaimAsync(ClaimDecisionContext context, CancellationToken cancellationToken);
    }

    /// <summary>席番号から <see cref="IPlayerAgent"/> を引く。</summary>
    public interface IPlayerAgentRegistry
    {
        /// <summary>対局開始時に人数分の Agent を用意する。</summary>
        void Setup(int playerCount, int humanSeat);

        IPlayerAgent Get(int seat);
    }
}
