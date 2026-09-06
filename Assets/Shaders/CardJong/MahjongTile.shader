// 麻雀牌のシェーダー。牌面（アトラスからイラストを引く）と本体（頂点カラーで白と象牙を塗り分ける）を
// 1 本で兼ねる。ドラの発光はどちらのマテリアルでも同じパラメーターで効く。
Shader "CardJong/Mahjong Tile"
{
    Properties
    {
        [MainTexture] _BaseMap("テクスチャ（牌面はアトラス）", 2D) = "white" {}
        [MainColor] _BaseColor("牌面側の白", Color) = (0.965, 0.957, 0.925, 1)
        _BackColor("胴体の象牙色", Color) = (0.886, 0.741, 0.400, 1)
        _Smoothness("なめらかさ", Range(0.0, 1.0)) = 0.55
        _Metallic("金属質", Range(0.0, 1.0)) = 0.0
        _Cutoff("アルファカットオフ", Range(0.0, 1.0)) = 0.5

        [Toggle(MAHJONG_TILE_FACE)] _FaceMode("牌面モード（アトラスから引く）", Float) = 0
        _FaceRect("牌面の UV 矩形 (x, y, 幅, 高さ)", Vector) = (0, 0, 1, 1)

        [Header(Dora)]
        [Space(4)]
        [HDR] _DoraColor("ドラの色", Color) = (1.0, 0.72, 0.24, 1)
        _DoraIntensity("ドラの強さ", Range(0.0, 1.0)) = 0.0
        _DoraCoreStrength("全体の底上げ", Range(0.0, 2.0)) = 0.18
        _DoraRimPower("縁の鋭さ", Range(0.5, 8.0)) = 2.5
        _DoraRimStrength("縁の強さ", Range(0.0, 4.0)) = 1.6
        _DoraPulseSpeed("明滅の速さ", Range(0.0, 4.0)) = 0.7
        _DoraPulseDepth("明滅の深さ", Range(0.0, 1.0)) = 0.45
        _DoraSweepSpeed("走る光の速さ", Range(0.0, 4.0)) = 0.45
        _DoraSweepWidth("走る光の幅", Range(0.01, 0.5)) = 0.12
        _DoraSweepStrength("走る光の強さ", Range(0.0, 4.0)) = 1.1
        _DoraTint("下地を金に寄せる量", Range(0.0, 1.0)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "Queue" = "Geometry"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex MahjongTileVertex
            #pragma fragment MahjongTileFragment

            #pragma shader_feature_local MAHJONG_TILE_FACE

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "MahjongTileForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex MahjongTileShadowVertex
            #pragma fragment MahjongTileDepthFragment

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "MahjongTileDepthPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex MahjongTileDepthVertex
            #pragma fragment MahjongTileDepthFragment

            #pragma multi_compile_instancing

            #include "MahjongTileDepthPasses.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex MahjongTileDepthNormalsVertex
            #pragma fragment MahjongTileDepthNormalsFragment

            #pragma multi_compile_instancing

            #include "MahjongTileDepthPasses.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
