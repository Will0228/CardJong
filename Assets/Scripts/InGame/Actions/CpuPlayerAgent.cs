using System;
using System.Threading;
using CardJong.Core;
using CardJong.InGame.Rules;
using Cysharp.Threading.Tasks;

namespace CardJong.InGame.Actions
{
    /// <summary>
    /// CPU の意思決定。
    /// </summary>
    /// <remarks>
    /// TODO: 現状は「上がれるなら上がる、それ以外は無作為に切る / 鳴かない」だけの仮実装。
    /// 手牌評価・危険牌判定・鳴き判断はここに実装する。
    /// </remarks>
    public sealed class CpuPlayerAgent : IPlayerAgent
    {
        private const int MinThinkMilliseconds = 400;
        private const int MaxThinkMilliseconds = 1200;

        private readonly IRandomService _random;

        public int Seat { get; }

        public CpuPlayerAgent(int seat, IRandomService random)
        {
            Seat = seat;
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public async UniTask<TurnAction> DecideTurnActionAsync(
            TurnDecisionContext context,
            CancellationToken cancellationToken)
        {
            await DelayThinkTimeAsync(cancellationToken);

            if (context.CanDeclareTsumo) return TurnAction.Tsumo();

            var hand = context.Model.GetPlayer(Seat).ConcealedCards;
            return TurnAction.Discard(hand[_random.Next(hand.Count)]);
        }

        public async UniTask<ClaimDeclaration> DecideClaimAsync(
            ClaimDecisionContext context,
            CancellationToken cancellationToken)
        {
            await DelayThinkTimeAsync(cancellationToken);

            for (var i = 0; i < context.Options.Count; i++)
            {
                if (context.Options[i].Type == ClaimType.Ron)
                {
                    return ClaimDeclaration.From(Seat, context.Options[i]);
                }
            }

            return ClaimDeclaration.Pass(Seat);
        }

        private UniTask DelayThinkTimeAsync(CancellationToken cancellationToken)
        {
            var milliseconds = MinThinkMilliseconds + _random.Next(MaxThinkMilliseconds - MinThinkMilliseconds);
            return UniTask.Delay(milliseconds, cancellationToken: cancellationToken);
        }
    }
}
