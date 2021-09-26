//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef UNIVERSALCLOUDLAYER_CS_HLSL
#define UNIVERSALCLOUDLAYER_CS_HLSL
// Generated from Expanse.UniversalCloudLayer+UniversalCloudNoiseLayer+UniversalCloudNoiseLayerRenderSettings
// PackingRules = Exact
struct UniversalCloudNoiseLayerRenderSettings
{
    int noiseType;
    float2 scale;
    int octaves;
    float octaveScale;
    float octaveMultiplier;
    int tile;
};

// Generated from Expanse.UniversalCloudLayer+UniversalCloudLayerRenderSettings
// PackingRules = Exact
struct UniversalCloudLayerRenderSettings
{
    int geometryType;
    float2 geometryXExtent;
    float2 geometryYExtent;
    float2 geometryZExtent;
    float geometryHeight;
    float coverageIntensity;
    float structureIntensity;
    float structureMultiply;
    float detailIntensity;
    float detailMultiply;
    float baseWarpIntensity;
    float detailWarpIntensity;
    float2 heightGradientBottom;
    float2 heightGradientTop;
    float rounding;
    float roundingShape;
    float2 windSkew;
    int coverageTile;
    int baseTile;
    int structureTile;
    int detailTile;
    int baseWarpTile;
    int detailWarpTile;
    float3 coverageOffset;
    float3 baseOffset;
    float3 structureOffset;
    float3 detailOffset;
    float3 baseWarpOffset;
    float3 detailWarpOffset;
    float density;
    float attenuationDistance;
    float attenuationBias;
    float2 rampUp;
    float3 extinctionCoefficients;
    float3 scatteringCoefficients;
    float multipleScatteringAmount;
    float multipleScatteringBias;
    float multipleScatteringRampDown;
    float multipleScatteringRampDownShape;
    float silverSpread;
    float silverIntensity;
    float anisotropy;
    float ambient;
    float2 ambientHeightRange;
    float2 ambientStrengthRange;
    int selfShadowing;
    int highQualityShadows;
    float maxSelfShadowDistance;
    float lightPollutionDimmer;
    int celShade;
    float celShadeLightingBands;
    float celShadeTransmittanceBands;
    int castShadows;
    float apparentThickness;
    float2 verticalProbabilityHeightRange;
    float verticalProbabilityStrength;
    float2 depthProbabilityHeightRange;
    float2 depthProbabilityStrengthRange;
    float depthProbabilityDensityMultiplier;
    float depthProbabilityBias;
    int depthProbabilityDetailIndex;
    float maxShadowIntensity;
    int reprojectionFrames;
    int useTemporalDenoising;
    float temporalDenoisingRatio;
    float coarseStepSize;
    float detailStepSize;
    float2 coarseStepRange;
    float2 detailStepRange;
    float2 stepDistanceRange;
    float2 flythroughStepRange;
    float2 flythroughStepDistanceRange;
    float mediaZeroThreshold;
    float transmittanceZeroThreshold;
    int maxConsecutiveZeroSamples;
};


#endif
