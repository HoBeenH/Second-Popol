//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef CELESTIALBODYRENDERSETTINGS_CS_HLSL
#define CELESTIALBODYRENDERSETTINGS_CS_HLSL
// Generated from Expanse.CelestialBodyRenderSettings
// PackingRules = Exact
struct CelestialBodyRenderSettings
{
    float3 direction;
    float cosAngularRadius;
    float distance;
    int receivesLight;
    int hasAlbedoTexture;
    float2 albedoTextureResolution;
    float4x4 albedoTextureRotation;
    float4 albedoTint;
    int moonMode;
    float retrodirection;
    float anisotropy;
    int emissive;
    int hasEmissionTexture;
    float2 emissionTextureResolution;
    float4 lightColor;
    float limbDarkening;
    float4x4 emissionTextureRotation;
    float4 emissionTint;
};


#endif
