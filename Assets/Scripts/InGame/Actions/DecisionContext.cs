using System.Collections.Generic;
using CardJong.InGame.Model;
using CardJong.InGame.Rules;

namespace CardJong.InGame.Actions
{
    /// <summary>自分の手番での選択に必要な情報。</summary>
    public sealed class TurnDecisionContext
    {
        public InGameModel Model { get; }

        public int Seat { get; }

        /// <summary>ツモ上がりを宣言できるか。</summary>
        public bool CanDeclareTsumo { get; }

        /// <summary>リーチを宣言できるか。</summary>
        public bool CanDeclareRiichi { get; }

        /// <summary>思考時間の上限（秒）。超過すると既定の行動が採用される。</summary>
        public float TimeLimitSeconds { get; }

        public TurnDecisionContext(
            InGameModel model,
            int seat,
            bool canDeclareTsumo,
            bool canDeclareRiichi,
            float timeLimitSeconds)
        {
            Model = model;
            Seat = seat;
            CanDeclareTsumo = canDeclareTsumo;
            CanDeclareRiichi = canDeclareRiichi;
            TimeLimitSeconds = timeLimitSeconds;
        }
    }

    /// <summary>他家の捨て札に対する選択に必要な情報。</summary>
    public sealed class ClaimDecisionContext
    {
        public InGameModel Model { get; }

        public int Seat { get; }

        public DiscardInfo Discard { get; }

        /// <summary>宣言できる選択肢。空でないことが保証される。</summary>
        public IReadOnlyList<ClaimOption> Options { get; }

        /// <summary>待機時間の上限（秒）。超過するとパス扱いになる。</summary>
        public float TimeLimitSeconds { get; }

        public ClaimDecisionContext(
            InGameModel model,
            int seat,
            DiscardInfo discard,
            IReadOnlyList<ClaimOption> options,
            float timeLimitSeconds)
        {
            Model = model;
            Seat = seat;
            Discard = discard;
            Options = options;
            TimeLimitSeconds = timeLimitSeconds;
        }
    }
}
