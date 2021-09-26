//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIGHTINGRENDERSETTINGS_CS_HLSL
#define LIGHTINGRENDERSETTINGS_CS_HLSL
//
// Expanse.LightingRenderSettings+PointLightGeometryType:  static fields
//
#define POINTLIGHTGEOMETRYTYPE_POINT (0)
#define POINTLIGHTGEOMETRYTYPE_CONE (1)
#define POINTLIGHTGEOMETRYTYPE_PYRAMID (2)
#define POINTLIGHTGEOMETRYTYPE_BOX (3)

// Generated from Expanse.LightingRenderSettings+DirectionalLightRenderSettings
// PackingRules = Exact
struct DirectionalLightRenderSettings
{
    float3 direction;
    float3 lightColor;
    float penumbraRadius;
    int useShadowmap;
    float maxShadowmapDistance;
    int shadowmapNDCSign;
    int volumetricGeometryShadows;
    int volumetricCloudShadows;
};

// Generated from Expanse.LightingRenderSettings+PointLightRenderSettings
// PackingRules = Exact
struct PointLightRenderSettings
{
    float3 position;
    float4x4 rotation;
    float4x4 inverseRotation;
    float3 lightColor;
    float range;
    int raymarch;
    float multiplier;
    int volumetricGeometryShadows;
    int volumetricCloudShadows;
    int useShadowmap;
    float maxShadowmapDistance;
    int shadowIndex;
    int geometryType;
    float geometryParam1;
    float geometryParam2;
};


#endif
