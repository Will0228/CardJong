namespace CardJong.InGame.Presentation.Table
{
    /// <summary>
    /// 3D の卓の窓口。何を並べるかはモデルから自分で拾うので、外から渡すものは無い。
    /// </summary>
    public interface ITablePresenter
    {
        /// <summary>卓を席数ぶん組み、モデルの購読を始める。席数が決まった時点で呼ぶ。</summary>
        void Initialize();
    }
}
