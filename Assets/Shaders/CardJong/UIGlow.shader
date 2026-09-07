// uGUI の Graphic（Image / RawImage / Text）を光らせるシェーダー。
//
// Canvas が Screen Space - Overlay だと URP のポストプロセスを通らず Bloom が乗らないので、
// 光はこのシェーダー自身が描く。Screen Space - Camera で Bloom を足せばさらに強く光る。
//
// 座標は 2 系統を使い分ける。
//   TEXCOORD0 : スプライト／フォントアトラスの UV。元テクスチャの読み取りとハローの膨張に使う
//   TEXCOORD1 : RectTransform の中を 0..1 に正規化した座標。グラデーション・帯・放射光に使う
// Text は文字ごとにアトラスの別々の場所を指すため TEXCOORD0 では全体にかかる演出が描けない。
// TEXCOORD1 は UIGlowEffect が書き込む。
Shader "CardJong/UI Glow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)

        _EffectIntensity("演出の強さ", Range(0.0, 1.0)) = 1.0
        _FillOpacity("下地の塗りの濃さ（背景板は 0）", Range(0.0, 1.0)) = 1.0
        _UseRectUv("Rect の座標を使う（UIGlowEffect が必要）", Range(0.0, 1.0)) = 1.0
        _RectAspect("Rect の縦横比", Float) = 1.0

        [Header(Fill)]
        [Space(4)]
        [HDR] _TopColor("上の色", Color) = (1.90, 1.42, 0.52, 1)
        [HDR] _BottomColor("下の色", Color) = (1.00, 0.52, 0.10, 1)
        _GradientAngle("グラデーションの角度", Range(0.0, 360.0)) = 90.0
        _GradientPower("グラデーションの寄り", Range(0.1, 4.0)) = 1.0

        [Header(Shine)]
        [Space(4)]
        [HDR] _ShineColor("帯の色", Color) = (2.40, 2.20, 1.70, 1)
        _ShineAngle("帯の角度", Range(0.0, 360.0)) = 65.0
        _ShineWidth("帯の幅", Range(0.01, 0.6)) = 0.12
        _ShineSpeed("帯の速さ", Range(0.0, 4.0)) = 0.7
        _ShineInterval("帯の間隔（1 で連続）", Range(1.0, 10.0)) = 3.0
        _ShineStrength("帯の強さ", Range(0.0, 4.0)) = 1.0

        [Header(Outer Glow)]
        [Space(4)]
        [Toggle(_OUTERGLOW_ON)] _OuterGlow("縁のハローを出す（Image 用）", Float) = 0
        [HDR] _GlowColor("ハローの色", Color) = (1.80, 1.15, 0.35, 1)
        _GlowRadius("ハローの太さ（元テクスチャの px）", Range(0.0, 64.0)) = 16.0
        _GlowPower("ハローの締まり", Range(0.2, 6.0)) = 1.2
        _GlowStrength("ハローの強さ", Range(0.0, 8.0)) = 3.0

        [Header(Radial Burst)]
        [Space(4)]
        [Toggle(_RADIALBURST_ON)] _RadialBurst("放射光を出す（背景板用）", Float) = 0
        [HDR] _BurstColor("放射光の色", Color) = (1.60, 1.20, 0.45, 1)
        _BurstCount("光条の本数", Range(2.0, 48.0)) = 16.0
        _BurstSharpness("光条の細さ", Range(0.5, 12.0)) = 3.0
        _BurstSpeed("回転の速さ", Range(-2.0, 2.0)) = 0.12
        _BurstFalloff("中心からの減衰", Range(0.2, 6.0)) = 2.0
        _BurstStrength("放射光の強さ", Range(0.0, 4.0)) = 1.0

        [Header(Pulse)]
        [Space(4)]
        _PulseSpeed("明滅の速さ", Range(0.0, 4.0)) = 0.9
        _PulseDepth("明滅の深さ", Range(0.0, 1.0)) = 0.30

        // ここから下は uGUI が Mask / RectMask2D のために書き換える定型。
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        // 乗算済みアルファ。本体は通常の合成、光は alpha 0 で出して加算になるようにする。
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local _OUTERGLOW_ON
            #pragma shader_feature_local _RADIALBURST_ON

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 rectUv   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float2 rectUv        : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            half _EffectIntensity;
            half _FillOpacity;
            half _UseRectUv;
            half _RectAspect;

            half4 _TopColor;
            half4 _BottomColor;
            half _GradientAngle;
            half _GradientPower;

            half4 _ShineColor;
            half _ShineAngle;
            half _ShineWidth;
            half _ShineSpeed;
            half _ShineInterval;
            half _ShineStrength;

            half4 _GlowColor;
            half _GlowRadius;
            half _GlowPower;
            half _GlowStrength;

            half4 _BurstColor;
            half _BurstCount;
            half _BurstSharpness;
            half _BurstSpeed;
            half _BurstFalloff;
            half _BurstStrength;

            half _PulseSpeed;
            half _PulseDepth;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                // UIGlowEffect が無い場合は TEXCOORD1 が 0 のままなので、スプライトの UV に落とす。
                // 絵が Rect いっぱいに入った Image ならこれでもほぼ同じ結果になる。
                OUT.rectUv = lerp(v.texcoord, v.rectUv, _UseRectUv);
                OUT.color = v.color * _Color;
                return OUT;
            }

#ifdef _OUTERGLOW_ON
            static const float2 kGlowDirections[12] =
            {
                float2( 1.000,  0.000), float2( 0.866,  0.500), float2( 0.500,  0.866),
                float2( 0.000,  1.000), float2(-0.500,  0.866), float2(-0.866,  0.500),
                float2(-1.000,  0.000), float2(-0.866, -0.500), float2(-0.500, -0.866),
                float2( 0.000, -1.000), float2( 0.500, -0.866), float2( 0.866, -0.500)
            };

            // 元テクスチャのアルファを外側へ膨らませて、輪郭の外に出る光の下地を作る。
            // アトラスに詰めたスプライトやフォントアトラスでは隣の絵を拾ってしまうので、
            // 周囲に余白のある単体テクスチャに対して使う。
            half DilatedAlpha(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy * _GlowRadius;

                // 遠いリングほど軽い重みでならす。max で取ると縁が段になって光らないので平均にする。
                half accumulated = tex2D(_MainTex, uv).a;
                half weightSum = 1.0h;

                UNITY_UNROLL
                for (int ring = 1; ring <= 4; ring++)
                {
                    half distance = ring / 4.0h;
                    half weight = (1.0h - distance) * (1.0h - distance);

                    UNITY_UNROLL
                    for (int direction = 0; direction < 12; direction++)
                    {
                        accumulated += tex2D(_MainTex, uv + kGlowDirections[direction] * texel * distance).a * weight;
                        weightSum += weight;
                    }
                }

                return accumulated / max(weightSum, 1e-4h);
            }
#endif

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 source = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                half intensity = saturate(_EffectIntensity);
                half2 rectUv = IN.rectUv;

                half wave = 0.5h + 0.5h * sin(_Time.y * _PulseSpeed * UNITY_TWO_PI);
                half pulse = lerp(1.0h - _PulseDepth, 1.0h, wave);

                // 下地のグラデーション。役満なら上を明るい金、下を橙にする。
                float gradientRadian = radians(_GradientAngle);
                float2 gradientAxis = float2(cos(gradientRadian), sin(gradientRadian));
                half gradient = saturate(dot(rectUv - 0.5h, gradientAxis) + 0.5h);
                gradient = pow(gradient, max(_GradientPower, 0.01h));
                half3 fill = lerp(_BottomColor.rgb, _TopColor.rgb, gradient);

                half3 body = lerp(source.rgb, source.rgb * fill * pulse, intensity);

                // 塗りだけを消せるようにしておく。放射光だけ出す背景板は _FillOpacity を 0 にする。
                half alpha = source.a * _FillOpacity;
                half3 additive = half3(0.0h, 0.0h, 0.0h);

                // 斜めに流れる光の帯。_ShineInterval のぶんだけ間を空けて繰り返す。
                float shineRadian = radians(_ShineAngle);
                float2 shineAxis = float2(cos(shineRadian), sin(shineRadian));
                half cycle = frac(_Time.y * _ShineSpeed / max(_ShineInterval, 1.0h));
                half phase = saturate(cycle * _ShineInterval);
                half shineCenter = lerp(-0.3h, 1.3h, phase);
                half shinePosition = dot(rectUv - 0.5h, shineAxis) + 0.5h;
                half shineOffset = (shinePosition - shineCenter) / max(_ShineWidth, 1e-3h);
                half shine = exp(-shineOffset * shineOffset) * _ShineStrength;
                additive += _ShineColor.rgb * shine * source.a * intensity;

#ifdef _OUTERGLOW_ON
                half halo = pow(saturate(DilatedAlpha(IN.texcoord)), max(_GlowPower, 0.01h));

                // 本体の内側は塗りに任せて、外へはみ出した分だけを光として足す。
                halo *= saturate(1.0h - source.a) * _GlowStrength * pulse * intensity * IN.color.a;
                additive += _GlowColor.rgb * halo;
#endif

#ifdef _RADIALBURST_ON
                float2 centered = (rectUv - 0.5h) * float2(_RectAspect, 1.0h);
                half radius = length(centered) * 2.0h;
                half angle = atan2(centered.y, centered.x) + _Time.y * _BurstSpeed;
                half rays = pow(0.5h + 0.5h * cos(angle * _BurstCount), max(_BurstSharpness, 0.01h));
                half falloff = pow(saturate(1.0h - radius), max(_BurstFalloff, 0.01h));
                additive += _BurstColor.rgb * rays * falloff * _BurstStrength * pulse * intensity * IN.color.a;
#endif

#ifdef UNITY_UI_CLIP_RECT
                half clipping = UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                alpha *= clipping;
                additive *= clipping;
#endif

#ifdef UNITY_UI_ALPHACLIP
                clip(max(alpha, max(additive.r, max(additive.g, additive.b))) - 0.001h);
#endif

                half4 result;
                result.rgb = body * alpha + additive;
                result.a = alpha;
                return result;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
