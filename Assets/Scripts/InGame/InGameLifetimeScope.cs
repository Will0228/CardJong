using CardJong.Core;
using CardJong.Core.Commands;
using CardJong.Core.Scenes;
using CardJong.InGame.Actions;
using CardJong.InGame.Commands;
using CardJong.InGame.Model;
using CardJong.InGame.Presentation;
using CardJong.InGame.Presentation.Hud;
using CardJong.InGame.Presentation.Table;
using CardJong.InGame.Rules;
using CardJong.InGame.States;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace CardJong.InGame
{
    /// <summary>
    /// インゲームの DI 構成。このコンポーネントを置いたシーンを再生すれば対局が 1 局まわる。
    /// </summary>
    public sealed class InGameLifetimeScope : LifetimeScope
    {
        [Tooltip("未設定の場合は既定値で動く。")]
        [SerializeField] private InGameSettings _settings;

        protected override void Configure(IContainerBuilder builder)
        {
            var settings = _settings != null ? _settings : ScriptableObject.CreateInstance<InGameSettings>();
            builder.RegisterInstance(settings);

            RegisterCommonServices(builder, settings);
            RegisterRules(builder);
            RegisterCommands(builder);
            RegisterPlayerInput(builder);
            RegisterPresentation(builder);
            RegisterStateMachine(builder);

            builder.RegisterEntryPoint<InGameBootstrapper>();
        }

        private void RegisterCommonServices(IContainerBuilder builder, InGameSettings settings)
        {
            builder.Register<IRandomService>(
                _ => settings.UseFixedSeed
                    ? new SystemRandomService(settings.RandomSeed)
                    : new SystemRandomService(),
                Lifetime.Singleton);

            builder.Register<InGameModel>(Lifetime.Singleton);
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
        }

        private void RegisterRules(IContainerBuilder builder)
        {
            builder.Register<IHandAnalyzer, HandAnalyzer>(Lifetime.Singleton);
            builder.Register<IClaimResolver, ClaimResolver>(Lifetime.Singleton);
            builder.Register<IScoreCalculator, ScoreCalculator>(Lifetime.Singleton);
        }

        private void RegisterCommands(IContainerBuilder builder)
        {
            // インゲーム中ずっと持ち回るものではなく、処理が込み入ってきたステートが
            // その場で使う道具なので、要求のたびに作り直す。
            builder.Register<IGameCommandInvoker, GameCommandInvoker>(Lifetime.Transient);
            builder.Register<GameCommandFactory>(Lifetime.Singleton);
        }

        private void RegisterPlayerInput(IContainerBuilder builder)
        {
            // UI 側は IPlayerInputPort、Agent 側は IPlayerInputRequester として同じ実体を見る。
            builder.Register<PlayerInputPort>(Lifetime.Singleton)
                .As<IPlayerInputPort>()
                .As<IPlayerInputRequester>();

            builder.Register<IPlayerAgentRegistry, PlayerAgentRegistry>(Lifetime.Singleton);
        }

        private void RegisterPresentation(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MahjongTableView>();
            builder.RegisterComponentInHierarchy<InGameHudView>().As<IInGamePresentation>();
        }

        private void RegisterStateMachine(IContainerBuilder builder)
        {
            builder.Register<IStateFactory<InGameStateType>, InGameStateFactory>(Lifetime.Singleton);

            builder.Register<StateMachine<InGameStateType>>(Lifetime.Singleton)
                .As<IStateSwitcher<InGameStateType>>()
                .AsSelf();

            // ステートは遷移のたびに作り直す
            builder.Register<GameStartState>(Lifetime.Transient);
            builder.Register<DecideDealerState>(Lifetime.Transient);
            builder.Register<RoundStartState>(Lifetime.Transient);
            builder.Register<DrawState>(Lifetime.Transient);
            builder.Register<PlayerActionState>(Lifetime.Transient);
            builder.Register<ClaimWaitState>(Lifetime.Transient);
            builder.Register<WinState>(Lifetime.Transient);
            builder.Register<RoundEndState>(Lifetime.Transient);
            builder.Register<GameEndState>(Lifetime.Transient);
        }
    }
}
