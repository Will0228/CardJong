using System.Collections.Generic;

namespace CardJong.Network.Matching
{
    /// <summary>席に座っているのが誰か。</summary>
    public enum SeatOccupantKind : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>人間のプレイヤー。</summary>
        Human = 1,

        /// <summary>人数が足りない分を埋める CPU。</summary>
        Cpu = 2,
    }

    /// <summary>席 1 つ分の割り当て。</summary>
    /// <param name="Seat">席番号。</param>
    /// <param name="Kind">人間か CPU か。</param>
    /// <param name="ActorId">人間の場合の <see cref="MatchMember.ActorId"/>。CPU の場合は 0。</param>
    /// <param name="DisplayName">画面に出す名前。</param>
    public sealed record SeatAssignment(int Seat, SeatOccupantKind Kind, int ActorId, string DisplayName)
    {
        public static SeatAssignment ForHuman(int seat, MatchMember member)
            => new(seat, SeatOccupantKind.Human, member.ActorId, member.NickName);

        public static SeatAssignment ForCpu(int seat, string displayName)
            => new(seat, SeatOccupantKind.Cpu, 0, displayName);

        public override string ToString() => $"seat{Seat} {Kind} {DisplayName}";
    }

    /// <summary>
    /// 対局開始時に確定する席割り。ホストが決めて全員に配り、その対局中は変わらない。
    /// </summary>
    /// <param name="Seats">席番号順に並んだ割り当て。</param>
    public sealed record MatchSeating(IReadOnlyList<SeatAssignment> Seats)
    {
        public int PlayerCount => Seats.Count;

        /// <summary>人間が座っている席の数。</summary>
        public int HumanCount => CountKind(SeatOccupantKind.Human);

        /// <summary>CPU が埋めた席の数。</summary>
        public int CpuCount => CountKind(SeatOccupantKind.Cpu);

        /// <summary>指定したプレイヤーの席を探す。居なければ false。</summary>
        public bool TryGetSeatOf(int actorId, out int seat)
        {
            for (var i = 0; i < Seats.Count; i++)
            {
                var assignment = Seats[i];
                if (assignment.Kind != SeatOccupantKind.Human || assignment.ActorId != actorId) continue;

                seat = assignment.Seat;
                return true;
            }

            seat = -1;
            return false;
        }

        /// <summary>CPU が担当する席。<c>PlayerAgentRegistry</c> に渡して Agent を差し替える。</summary>
        public IReadOnlyList<int> GetCpuSeats()
        {
            var seats = new List<int>(Seats.Count);
            for (var i = 0; i < Seats.Count; i++)
            {
                if (Seats[i].Kind == SeatOccupantKind.Cpu) seats.Add(Seats[i].Seat);
            }

            return seats;
        }

        private int CountKind(SeatOccupantKind kind)
        {
            var count = 0;
            for (var i = 0; i < Seats.Count; i++)
            {
                if (Seats[i].Kind == kind) count++;
            }

            return count;
        }

        public override string ToString() => string.Join(" / ", Seats);
    }
}
