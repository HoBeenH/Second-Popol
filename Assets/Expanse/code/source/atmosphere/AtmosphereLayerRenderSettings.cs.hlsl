//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef ATMOSPHERELAYERRENDERSETTINGS_CS_HLSL
#define ATMOSPHERELAYERRENDERSETTINGS_CS_HLSL
// Generated from Expanse.AtmosphereLayerRenderSettings
// PackingRules = Exact
struct AtmosphereLayerRenderSettings
{
    float3 extinctionCoefficients;
    float3 scatteringCoefficients;
    int densityDistribution;
    float height;
    float thickness;
    int phaseFunction;
    float anisotropy;
    float density;
    float3 tint;
    float multipleScatteringMultiplier;
    int screenspaceShadows;
    float maxGeometryOcclusion;
    float maxCloudOcclusion;
    int geometryShadows;
    int cloudShadows;
    int useCloudArray;
    int physicalLighting;
};


#endif
