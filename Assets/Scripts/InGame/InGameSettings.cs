using UnityEngine;

namespace CardJong.InGame
{
    /// <summary>局数の設定。</summary>
    public enum RoundMode : byte
    {
        /// <summary>未設定。</summary>
        None = 0,

        /// <summary>東風戦（東 1 局 〜 東 4 局）。</summary>
        East = 1,

        /// <summary>半荘戦（東 1 局 〜 南 4 局）。</summary>
        Half = 2,
    }

    /// <summary>インゲームの各種設定。</summary>
    [CreateAssetMenu(fileName = "InGameSettings", menuName = "CardJong/InGame Settings")]
    public sealed class InGameSettings : ScriptableObject
    {
        [Header("対局")]
        [SerializeField, Range(3, 4)] private int _playerCount = 4;
        [SerializeField] private int _initialScore = 25000;
        [SerializeField] private RoundMode _roundMode = RoundMode.East;

        [Header("操作")]
        [Tooltip("人間が操作する席。それ以外は CPU が担当する。-1 にすると全席 CPU になり、UI 無しでも対局が進む。")]
        [SerializeField] private int _humanSeat = -1;

        [Tooltip("打牌の思考時間（秒）。0 以下で無制限。超過するとツモ切りになる。")]
        [SerializeField] private float _thinkTimeSeconds = 10f;

        [Tooltip("ロン・ポン・チーの待機時間（秒）。0 以下で無制限。超過するとパスになる。")]
        [SerializeField] private float _claimWaitSeconds = 5f;

        [Header("演出")]
        [Tooltip("局の開始・親決めなど、短い案内を出しておく秒数。")]
        [SerializeField, Min(0f)] private float _noticeSeconds = 1.6f;

        [Tooltip("和了・局終了・最終結果を出しておく秒数。")]
        [SerializeField, Min(0f)] private float _resultSeconds = 3.5f;

        [Header("デバッグ")]
        [Tooltip("同じ配牌を再現したいときに有効にする。")]
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _randomSeed = 12345;

        public int PlayerCount => _playerCount;

        public int InitialScore => _initialScore;

        public RoundMode RoundMode => _roundMode;

        public int HumanSeat => _humanSeat;

        public float ThinkTimeSeconds => _thinkTimeSeconds;

        public float ClaimWaitSeconds => _claimWaitSeconds;

        public float NoticeSeconds => _noticeSeconds;

        public float ResultSeconds => _resultSeconds;

        public bool UseFixedSeed => _useFixedSeed;

        public int RandomSeed => _randomSeed;

        /// <summary>この対局の総局数。</summary>
        public int TotalRoundCount => _roundMode == RoundMode.Half ? _playerCount * 2 : _playerCount;
    }
}
