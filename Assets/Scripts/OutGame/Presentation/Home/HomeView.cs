using R3;
using UnityEngine;
using UnityEngine.UI;

namespace CardJong.OutGame.Presentation.Home
{
    public sealed class HomeView : MonoBehaviour
    {
        [SerializeField] private Button _startButton;

        public Observable<Unit> OnStartClicked() => _startButton.OnClickAsObservable();
    }
}
