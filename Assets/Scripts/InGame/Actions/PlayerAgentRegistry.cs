using System;
using System.Collections.Generic;
using CardJong.Core;
using VContainer;

namespace CardJong.InGame.Actions
{
    /// <summary>
    /// 席ごとの Agent を作って保持する。1 席だけ人間、残りは CPU という構成。
    /// </summary>
    public sealed class PlayerAgentRegistry : IPlayerAgentRegistry
    {
        private readonly IPlayerInputRequester _inputRequester;
        private readonly IRandomService _random;
        private readonly List<IPlayerAgent> _agents = new();

        [Inject]
        public PlayerAgentRegistry(IPlayerInputRequester inputRequester, IRandomService random)
        {
            _inputRequester = inputRequester ?? throw new ArgumentNullException(nameof(inputRequester));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public void Setup(int playerCount, int humanSeat)
        {
            _agents.Clear();

            for (var seat = 0; seat < playerCount; seat++)
            {
                _agents.Add(seat == humanSeat
                    ? new HumanPlayerAgent(seat, _inputRequester)
                    : (IPlayerAgent)new CpuPlayerAgent(seat, _random));
            }
        }

        public IPlayerAgent Get(int seat)
        {
            if (seat < 0 || seat >= _agents.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(seat), seat, "Agent が用意されていません。");
            }

            return _agents[seat];
        }
    }
}
