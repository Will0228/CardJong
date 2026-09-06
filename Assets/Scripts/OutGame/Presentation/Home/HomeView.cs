using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace CardJong.OutGame.Presentation.Home
{
    /// <summary>
    /// ホーム画面。タイトルやボタンの配置・見た目はシーン側で組んであり、
    /// ここではボタンの取り次ぎと、日本語ラベルへのフォント割り当てだけを行う。
    /// </summary>
    [AddComponentMenu("CardJong/Home View")]
    public sealed class HomeView : MonoBehaviour
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

        private readonly CompositeDisposable _subscriptions = new();

        [Inject]
        public void Construct(HomeModel model)
        {
            AssignJapaneseFont();

            _subscriptions.Add(_startButton.OnClickAsObservable().Subscribe(_ => model.RequestStart()));
            _subscriptions.Add(model.CanStart.SubscribeToInteractable(_startButton));
        }

        private void OnDestroy() => _subscriptions.Dispose();

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
