#ifndef CARDJONG_MAHJONG_TILE_FORWARD_PASS_INCLUDED
#define CARDJONG_MAHJONG_TILE_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "MahjongTileInput.hlsl"
#include "MahjongTileDora.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 uv         : TEXCOORD0;
    half4  color      : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS              : SV_POSITION;
    float2 uv                      : TEXCOORD0;
    float3 positionWS              : TEXCOORD1;
    half3  normalWS                : TEXCOORD2;
    half4  fogFactorAndVertexLight : TEXCOORD3;
    half4  color                   : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings MahjongTileVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = half3(normalInputs.normalWS);
    output.color = input.color;

#if defined(MAHJONG_TILE_FACE)
    // 牌面はアトラスの矩形に貼り直すので、ここではタイリングをかけない。
    output.uv = input.uv;
#else
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
#endif

    half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
    half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    return output;
}

half4 MahjongTileFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(MAHJONG_TILE_FACE)
    // アトラス内のセルへ写す。牌ごとの矩形は MaterialPropertyBlock から届く。
    float4 faceRect = MAHJONG_TILE_FACE_RECT;
    float2 sampleUV = faceRect.xy + saturate(input.uv) * faceRect.zw;
#else
    float2 sampleUV = input.uv;
#endif

    half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, sampleUV);

    // 頂点カラーの R が 1 なら牌面側の白、0 なら胴体の象牙色。
    half3 tileColor = lerp(_BackColor.rgb, _BaseColor.rgb, input.color.r);
    half3 albedo = baseSample.rgb * tileColor;

    half3 normalWS = normalize(input.normalWS);
    half3 viewDirectionWS = half3(GetWorldSpaceNormalizeViewDir(input.positionWS));

    MahjongDoraGlow glow = ComputeMahjongDoraGlow(MAHJONG_TILE_DORA_INTENSITY, input.uv, normalWS, viewDirectionWS);
    albedo = lerp(albedo, albedo * _DoraColor.rgb, glow.tint);

    InputData inputData = (InputData)0;
    inputData.positionCS = input.positionCS;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = viewDirectionWS;
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SampleSH(normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo;
    surfaceData.alpha = 1.0h;
    surfaceData.metallic = _Metallic;
    surfaceData.smoothness = _Smoothness;
    surfaceData.occlusion = 1.0h;
    surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
    surfaceData.emission = glow.emission;

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = 1.0h;
    return color;
}

#endif
