using CardJong.Core;
using CardJong.Core.Scenes;
using CardJong.OutGame.Presentation;
using CardJong.OutGame.Presentation.Home;
using CardJong.OutGame.States;
using VContainer;
using VContainer.Unity;

namespace CardJong.OutGame
{
    /// <summary>
    /// アウトゲームの DI 構成。このコンポーネントを置いたシーンを再生すればホーム画面が出る。
    /// </summary>
    public sealed class OutGameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);

            RegisterPresentation(builder);
            RegisterStateMachine(builder);

            builder.RegisterEntryPoint<OutGameBootstrapper>();
        }

        private void RegisterPresentation(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<HomeView>().As<IHomePresentation>();
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
