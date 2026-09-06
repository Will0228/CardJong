using System;
using System.Collections.Generic;
using CardJong.Core;
using VContainer;

namespace CardJong.Network.Matching
{
    /// <summary>部屋の参加者から席割りを作る。</summary>
    public interface ISeatingArranger
    {
        /// <summary>参加者を席に配り、空いた席を CPU で埋める。</summary>
        MatchSeating Arrange(MatchRoom room);
    }

    /// <summary>
    /// 席順を毎回シャッフルする既定実装。
    /// </summary>
    /// <remarks>
    /// 入室順をそのまま席順にすると部屋を建てた人が必ず席 0 になり、
    /// 上家・下家の関係が固定される。チーは上家からしかできないので、
    /// 席の並びは有利不利に直結する。
    /// </remarks>
    public sealed class SeatingArranger : ISeatingArranger
    {
        private readonly IRandomService _random;

        [Inject]
        public SeatingArranger(IRandomService random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public MatchSeating Arrange(MatchRoom room)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));

            var seatCount = room.Criteria.PlayerCount;
            if (room.Members.Count > seatCount)
            {
                throw new ArgumentException(
                    $"参加者が席数を超えています: {room.Members.Count} 人 / {seatCount} 席", nameof(room));
            }

            // null が CPU の席。人間と混ぜてから席番号を振ることで、CPU の位置も散らばる。
            var occupants = new List<MatchMember>(seatCount);
            occupants.AddRange(room.Members);
            while (occupants.Count < seatCount)
            {
                occupants.Add(null);
            }

            Shuffle(occupants);

            var assignments = new List<SeatAssignment>(seatCount);
            var cpuNumber = 0;
            for (var seat = 0; seat < seatCount; seat++)
            {
                var member = occupants[seat];
                assignments.Add(member != null
                    ? SeatAssignment.ForHuman(seat, member)
                    : SeatAssignment.ForCpu(seat, $"CPU {++cpuNumber}"));
            }

            return new MatchSeating(assignments);
        }

        private void Shuffle(IList<MatchMember> occupants)
        {
            for (var i = occupants.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (occupants[i], occupants[j]) = (occupants[j], occupants[i]);
            }
        }
    }
}
