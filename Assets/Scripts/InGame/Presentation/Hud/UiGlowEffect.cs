using UnityEngine;
using UnityEngine.UI;

namespace CardJong.InGame.Presentation.Hud
{
    /// <summary>
    /// uGUI の Graphic を CardJong/UI Glow で光らせる。役満の発表など、一瞬だけ強く見せたい表示に使う。
    /// </summary>
    /// <remarks>
    /// このコンポーネントが要るのは 2 つの理由から。
    ///
    /// 1. シェーダーは RectTransform の中を 0..1 に正規化した座標（TEXCOORD1）を必要とする。
    ///    Text は文字ごとにフォントアトラスの別の場所を指すので、TEXCOORD0 では
    ///    「表示全体を斜めに走る帯」のような演出が描けない。ここで uv1 に書き込む。
    /// 2. 点灯・消灯のフェードを持たせる。明滅や帯の流れはシェーダーが _Time で回すので、
    ///    毎フレーム送るのはフェード中の強さだけで済む。
    ///
    /// マテリアルは実行時に複製して自分専用にする。Graphic は MaterialPropertyBlock を
    /// 通さないため、複製しないと同じマテリアルを使う他の表示まで一緒に光ってしまう。
    /// </remarks>
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("CardJong/UI Glow Effect")]
    public sealed class UiGlowEffect : BaseMeshEffect
    {
        private static readonly int EffectIntensityId = Shader.PropertyToID("_EffectIntensity");
        private static readonly int RectAspectId = Shader.PropertyToID("_RectAspect");

        [SerializeField]
        [Min(0f)]
        [Tooltip("点灯にかける秒数。0 なら即座に点く。")]
        private float _fadeInDuration = 0.35f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("消灯にかける秒数。0 なら即座に消える。")]
        private float _fadeOutDuration = 0.25f;

        [SerializeField]
        [Tooltip("有効になった時点で点灯を始めるか。")]
        private bool _playOnEnable = true;

        private Material _materialInstance;
        private float _intensity;
        private float _target;
        private float _appliedAspect = -1f;
        private bool _rectUvChannelEnabled;

        /// <summary>いまの光の強さ。0 で完全に素の見た目。</summary>
        public float Intensity => _intensity;

        /// <summary>点灯する。<paramref name="immediate"/> が true ならフェードせず一気に点ける。</summary>
        public void Play(bool immediate = false)
        {
            _target = 1f;
            if (immediate) SetIntensity(1f);
        }

        /// <summary>消灯する。<paramref name="immediate"/> が true ならフェードせず一気に消す。</summary>
        public void Stop(bool immediate = false)
        {
            _target = 0f;
            if (immediate) SetIntensity(0f);
        }

        /// <summary>Rect の中を 0..1 に正規化した座標を uv1 に載せる。</summary>
        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive()) return;

            var rect = graphic.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            var vertex = new UIVertex();
            for (var i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                vertex.uv1 = new Vector2(
                    Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x),
                    Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y));
                vertexHelper.SetUIVertex(vertex, i);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            EnsureMaterialInstance();
            EnableRectUvChannel();

            _target = _playOnEnable ? 1f : 0f;
            SetIntensity(_playOnEnable && _fadeInDuration <= 0f ? 1f : 0f);
            ApplyAspect();

            graphic.SetVerticesDirty();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_materialInstance == null) return;

            Destroy(_materialInstance);
            _materialInstance = null;
        }

        private void Update()
        {
            // 結果発表は timeScale を落としている最中に出ることがあるので、実時間で進める。
            if (!_rectUvChannelEnabled) EnableRectUvChannel();
            ApplyAspect();

            if (Mathf.Approximately(_intensity, _target)) return;

            var duration = _target > _intensity ? _fadeInDuration : _fadeOutDuration;
            SetIntensity(duration <= 0f
                ? _target
                : Mathf.MoveTowards(_intensity, _target, Time.unscaledDeltaTime / duration));
        }

        private void SetIntensity(float value)
        {
            _intensity = Mathf.Clamp01(value);

            var material = _materialInstance;
            if (material == null) return;

            material.SetFloat(EffectIntensityId, _intensity);
        }

        /// <summary>
        /// マテリアルを自分専用に複製する。編集中に複製するとアセット側が汚れるので実行時だけ。
        /// </summary>
        private void EnsureMaterialInstance()
        {
            if (!Application.isPlaying) return;

            var source = graphic.material;
            if (source == null || source == _materialInstance) return;

            _materialInstance = new Material(source) { name = $"{source.name} (Instance)" };
            graphic.material = _materialInstance;
        }

        /// <summary>
        /// uv1 は既定では頂点ストリームに乗らないので、Canvas に流すよう頼む。
        /// Canvas が見つかるのは階層に入った後なので、取れるまで毎フレーム試す。
        /// </summary>
        private void EnableRectUvChannel()
        {
            var canvas = graphic.canvas;
            if (canvas == null) return;

            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            _rectUvChannelEnabled = true;
        }

        /// <summary>放射光が Rect の縦横比で潰れないよう、比率をシェーダーへ渡す。</summary>
        private void ApplyAspect()
        {
            var material = _materialInstance;
            if (material == null) return;

            var rect = graphic.rectTransform.rect;
            var aspect = rect.height > 0f ? rect.width / rect.height : 1f;
            if (Mathf.Approximately(aspect, _appliedAspect)) return;

            _appliedAspect = aspect;
            material.SetFloat(RectAspectId, aspect);
        }
    }
}
