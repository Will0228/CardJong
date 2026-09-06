using CardJong.Core;
using CardJong.Core.Scenes;
using CardJong.OutGame.Presentation.Home;
using CardJong.OutGame.States;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardJong.OutGame
{
    /// <summary>
    /// アウトゲームの DI 構成。このコンポーネントを置いたシーンを再生すればホーム画面が出る。
    /// </summary>
    public sealed class OutGameLifetimeScope : LifetimeScope
    {
        [SerializeField] private HomeView _homeView;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);

            RegisterPresentation(builder);
            RegisterStateMachine(builder);

            builder.RegisterEntryPoint<OutGameBootstrapper>();
        }

        private void RegisterPresentation(IContainerBuilder builder)
        {
            builder.RegisterComponent(_homeView);
            builder.Register<IHomePresenter, HomePresenter>(Lifetime.Singleton);
        }

        private void RegisterStateMachine(IContainerBuilder builder)
        {
            builder.Register<IStateFactory<OutGameStateType>, OutGameStateFactory>(Lifetime.Singleton);

            builder.Register<StateMachine<OutGameStateType>>(Lifetime.Singleton)
                .As<IStateSwitcher<OutGameStateType>>()
                .AsSelf();

            // ステートは遷移のたびに作り直す
            builder.Register<HomeState>(Lifetime.Transient);
        }
    }
}
