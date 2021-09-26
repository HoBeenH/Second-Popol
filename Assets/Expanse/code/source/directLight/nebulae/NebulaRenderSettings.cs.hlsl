//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef NEBULARENDERSETTINGS_CS_HLSL
#define NEBULARENDERSETTINGS_CS_HLSL
// Generated from Expanse.NebulaRenderSettings
// PackingRules = Exact
struct NebulaRenderSettings
{
    float4x4 rotation;
    float definition;
    float intensity;
    float4 tint;
    float coverageScale;
    float3 coverageSeed;
    float transmittanceScale;
    float2 transmittanceRange;
    float3 transmittanceSeedX;
    float3 transmittanceSeedY;
    float3 transmittanceSeedZ;
};

// Generated from Expanse.NebulaRenderSettings+NebulaGeneratorLayerSettings
// PackingRules = Exact
struct NebulaGeneratorLayerSettings
{
    float intensity;
    float4 color;
    int noise;
    float scale;
    int octaves;
    float octaveScale;
    float octaveMultiplier;
    float coverage;
    float spread;
    float bias;
    float definition;
    float strength;
    float warpScale;
    float warpIntensity;
    float3 baseSeedX;
    float3 baseSeedY;
    float3 baseSeedZ;
    float3 warpSeedX;
    float3 warpSeedY;
    float3 warpSeedZ;
};


#endif
