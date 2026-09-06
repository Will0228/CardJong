#ifndef CARDJONG_MAHJONG_TILE_DORA_INCLUDED
#define CARDJONG_MAHJONG_TILE_DORA_INCLUDED

#include "MahjongTileInput.hlsl"

// ドラの光り方。3 つを重ねている。
//   1. フレネル : 見込み角が浅いほど強く光り、牌の輪郭が金色に浮く
//   2. 掃引     : 面をゆっくり流れる帯。磨いた金の反射に見せる
//   3. 明滅     : 全体をゆっくり呼吸させる
// どれも _DoraIntensity で一括して 0 に落ちるので、ドラでない牌は完全に素の見た目に戻る。
struct MahjongDoraGlow
{
    half3 emission;
    half  tint;
};

MahjongDoraGlow ComputeMahjongDoraGlow(half intensity, float2 sweepUV, half3 normalWS, half3 viewDirectionWS)
{
    MahjongDoraGlow glow;
    glow.emission = half3(0.0h, 0.0h, 0.0h);
    glow.tint = 0.0h;

    intensity = saturate(intensity);
    if (intensity <= 0.0h)
    {
        return glow;
    }

    // 明滅。_DoraPulseDepth が 0 なら常時点灯、1 なら消灯まで落ちる。
    half wave = 0.5h + 0.5h * sin(_Time.y * _DoraPulseSpeed * TWO_PI);
    half pulse = lerp(1.0h - _DoraPulseDepth, 1.0h, wave);

    // フレネル。牌は角が丸いので、縁の丸め部分がぐるりと光る。
    half facing = saturate(dot(normalWS, viewDirectionWS));
    half rim = pow(1.0h - facing, max(_DoraRimPower, 0.1h)) * _DoraRimStrength;

    // 掃引。UV を斜めに切って、周期 1 の帯を時間で流す。
    half phase = frac(sweepUV.x * 0.35h + sweepUV.y * 0.65h - _Time.y * _DoraSweepSpeed);
    half offset = phase - 0.5h;
    half width = max(_DoraSweepWidth, 1e-3h);
    half sweep = exp(-(offset * offset) / (width * width)) * _DoraSweepStrength;

    glow.emission = _DoraColor.rgb * ((rim + sweep + _DoraCoreStrength) * pulse * intensity);
    glow.tint = saturate(_DoraTint * intensity);
    return glow;
}

#endif
