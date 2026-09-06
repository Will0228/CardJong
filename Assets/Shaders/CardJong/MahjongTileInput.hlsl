#ifndef CARDJONG_MAHJONG_TILE_INPUT_INCLUDED
#define CARDJONG_MAHJONG_TILE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

// マテリアル単位のパラメーター。牌面用と本体用の 2 シェーダーで同じ並びを共有する。
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4  _BaseColor;
    half4  _BackColor;
    half   _Cutoff;
    half   _Smoothness;
    half   _Metallic;
    half   _FaceMode;

    half4  _DoraColor;
    half   _DoraCoreStrength;
    half   _DoraRimPower;
    half   _DoraRimStrength;
    half   _DoraPulseSpeed;
    half   _DoraPulseDepth;
    half   _DoraSweepSpeed;
    half   _DoraSweepWidth;
    half   _DoraSweepStrength;
    half   _DoraTint;
CBUFFER_END

// 牌ごとに変わる値。208 枚が同じマテリアルのまま GPU インスタンシングでまとまるよう、
// MaterialPropertyBlock で差し込む 2 つだけをインスタンスバッファに置く。
UNITY_INSTANCING_BUFFER_START(MahjongTileProps)
    UNITY_DEFINE_INSTANCED_PROP(float4, _FaceRect)
    UNITY_DEFINE_INSTANCED_PROP(float, _DoraIntensity)
UNITY_INSTANCING_BUFFER_END(MahjongTileProps)

#define MAHJONG_TILE_FACE_RECT UNITY_ACCESS_INSTANCED_PROP(MahjongTileProps, _FaceRect)
#define MAHJONG_TILE_DORA_INTENSITY UNITY_ACCESS_INSTANCED_PROP(MahjongTileProps, _DoraIntensity)

#endif
