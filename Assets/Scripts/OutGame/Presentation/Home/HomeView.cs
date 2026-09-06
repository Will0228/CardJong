using R3;
using UnityEngine;
using UnityEngine.UI;

namespace CardJong.OutGame.Presentation.Home
{
    /// <summary>
    /// ホーム画面。タイトルやボタンの配置・見た目はシーン側で組んである。
    /// ここは見た目の操作とユーザー操作の通知に専念し、進行の判断は Presenter に任せる。
    /// </summary>
    [AddComponentMenu("CardJong/Home View")]
    public sealed class HomeView : MonoBehaviour, IHomeView
    {
        /// <summary>日本語のラベルを出すために借りる OS フォント。前から順に探す。</summary>
        private static readonly string[] FontCandidates =
        {
            "Yu Gothic UI",
            "Yu Gothic",
            "Meiryo",
            "MS Gothic",
            "Hiragino Sans",
            "Noto Sans CJK JP",
        };

        private const int FontAtlasSize = 48;

        [SerializeField] private Button _startButton;

        [Tooltip("日本語を表示するテキスト。実行環境の OS フォントをここへ割り当てる。")]
        [SerializeField] private Text[] _japaneseTexts;

        public Observable<Unit> OnStartClicked => _startButton.OnClickAsObservable();

        public bool CanStart
        {
            set => _startButton.interactable = value;
        }

        private void Awake() => AssignJapaneseFont();

        /// <summary>
        /// OS のフォントが 1 つも見つからない環境では、Text にあらかじめ設定してある
        /// 組み込みフォントのままにする（英数字だけになる）。
        /// </summary>
        private void AssignJapaneseFont()
        {
            var osFont = Font.CreateDynamicFontFromOSFont(FontCandidates, FontAtlasSize);
            if (osFont == null) return;

            foreach (var text in _japaneseTexts)
            {
                text.font = osFont;
            }
        }
    }
}
