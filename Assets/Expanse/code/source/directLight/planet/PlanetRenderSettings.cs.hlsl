//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef PLANETRENDERSETTINGS_CS_HLSL
#define PLANETRENDERSETTINGS_CS_HLSL
// Generated from Expanse.PlanetRenderSettings
// PackingRules = Exact
struct PlanetRenderSettings
{
    float radius;
    float atmosphereRadius;
    float3 originOffset;
    float clipFade;
    float4 groundTint; // x: r y: g z: b w: a 
    float groundEmissionMultiplier;
    float4x4 rotation;
    int hasAlbedoTexture;
    int hasEmissionTexture;
};


#endif
