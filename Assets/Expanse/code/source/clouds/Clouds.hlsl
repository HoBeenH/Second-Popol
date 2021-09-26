#ifndef EXPANSE_CLOUDS_INCLUDED
#define EXPANSE_CLOUDS_INCLUDED

/* Global buffers. */
#include "../directLight/planet/PlanetGlobals.hlsl"
#include "../directLight/general/NightSkyGlobals.hlsl"
#include "../lighting/LightingGlobals.hlsl"
#include "CloudGlobals.hlsl"

#include "../common/Geometry.hlsl"
#include "../common/Utilities.hlsl"
#include "../common/Noise.hlsl"
#include "../common/Random.hlsl"
#include "../atmosphere/AtmosphereGeometry.hlsl"
#include "../atmosphere/AtmosphereMapping.hlsl"
#include "../atmosphere/AtmosphereGlobalBuffers.hlsl"
#include "../atmosphere/Atmosphere.hlsl"
#include "CloudDatatypes.cs.hlsl"
#include "CloudGeometry.hlsl"

/* Noise textures. */
TEXTURE2D(_CloudCoverage2D);
TEXTURE2D(_CloudBase2D);
TEXTURE2D(_CloudStructure2D);
TEXTURE2D(_CloudDetail2D);
TEXTURE2D(_CloudBaseWarp2D);
TEXTURE2D(_CloudDetailWarp2D);

/* Coverage is always 2D, but we'll assign it a different texture just in
 * case we ever want to change that. */
TEXTURE2D(_CloudCoverage3D);
TEXTURE3D(_CloudBase3D);
TEXTURE3D(_CloudStructure3D);
TEXTURE3D(_CloudDetail3D);
TEXTURE3D(_CloudBaseWarp3D);
TEXTURE3D(_CloudDetailWarp3D);

struct CloudResult {
  float3 lighting;
  float3 transmittance;
  float t;
  float geoHit;
};

/**
 * @brief: static class for rendering clouds.
 * */
class Clouds {

  static float2 mapGBuffer(float2 intersectionsIn) {
    return intersectionsIn / 256;
  }
  static float2 unmapGBuffer(float2 gBufferIn) {
    return gBufferIn * 256;
  }

  /**
   * @brief: given current framecount and layer subresolution, returns
   * the current subpixel to be rendering for reprojection.
   * */
  static uint getCurrentSubpixelID(uint frameCount, uint subresolution) {
    static int grids[4][16] = {{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
      {0, 3, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
      {0, 5, 7, 1, 3, 8, 2, 4, 6, 0, 0, 0, 0, 0, 0, 0},
      {0, 10, 2, 8, 5, 15, 7, 13, 1, 11, 3, 9, 4, 14, 6, 12}};
    return grids[subresolution-1][frameCount % (subresolution * subresolution)] % (subresolution * subresolution); // HACK
  }
  static uint2 getCurrentSubpixel(uint frameCount, uint subresolution) {
    uint pixelID = getCurrentSubpixelID(frameCount, subresolution);
    return uint2(pixelID / subresolution, pixelID % subresolution);
  }
  static uint getSubpixelID(uint2 pixel, uint subresolution) {
    return (pixel.x % subresolution) * subresolution + pixel.y % subresolution;
  }

  static float henyeyGreensteinPhase(float dLd, float e) {
    return ((1 - e * e) / pow(abs(1 + e * e - 2 * e * dLd), 3.0/2.0)) / (4 * PI);
  }

  static float cloudPhaseFunction(float dLd, float anisotropy, float silverIntensity, float silverSpread) {
    return max(henyeyGreensteinPhase(dLd, anisotropy), silverIntensity * henyeyGreensteinPhase(dLd, 0.99 - silverSpread));
  }

  static float computeHeightGradient(float y, float2 bottom, float2 top) {
    return NoiseUtilities::remap(y, bottom.x, bottom.y, 0, 1) * NoiseUtilities::remap(y, top.x, top.y, 1, 0);
  }

  static float3 computeMSModifiedTransmittance(float3 extinctionCoefficients, float opticalDepth, int octaves, float a, float b) {
    float3 transmittance = 0;
    for (int i = 0; i < octaves; i++) {
      transmittance += pow(abs(b), i) * exp((-pow(abs(a), i) * opticalDepth) * extinctionCoefficients);
    }
    return max(exp(-extinctionCoefficients * opticalDepth), saturate(transmittance));
  }

  static float computeVerticalInScatterProbability(float height, float2 heightRange,
    float strength) {
    float clampedHeight = clamp(height, heightRange.x, heightRange.y);
    float remappedHeight = NoiseUtilities::remap(clampedHeight, heightRange.x, heightRange.y, 0.1, 1.0);
    return pow(abs(remappedHeight), strength);
  }

  static float computeDepthInScatterProbability(float loddedDensity, float height,
    float2 heightRange, float2 strengthRange, float multiplier, float bias) {
    float multipliedLoddedDensity = abs(loddedDensity * multiplier);
    float clampedHeight = clamp(height, heightRange.x, heightRange.y);
    float strength = NoiseUtilities::remap(clampedHeight, heightRange.x, heightRange.y,
      strengthRange.x, strengthRange.y);
    return saturate(bias + pow(multipliedLoddedDensity, strength));
  }

  /* Samples density textures at point p at specified mip level. */
  static float takeMediaSample2DLowLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry, int mipLevel) {
    /* Warp. */
    float2 baseWarp = float2(0, 0);
    if (settings.baseWarpIntensity > FLT_EPSILON) {
      const float kBaseWarpMax = 0.5;
      float2 baseWarpUV = geometry.mapCoordinate(p, settings.baseWarpTile, settings.baseWarpOffset).xz;
      baseWarp = settings.baseWarpIntensity * kBaseWarpMax
        * SAMPLE_TEXTURE2D_LOD(_CloudBaseWarp2D, s_linear_repeat_sampler,
        baseWarpUV, mipLevel).xy;
    }

    /* Base. */
    float2 baseUV = frac(geometry.mapCoordinate(p, settings.baseTile, settings.baseOffset).xz - baseWarp);
    float sample = SAMPLE_TEXTURE2D_LOD(_CloudBase2D, s_linear_repeat_sampler,
      baseUV, mipLevel).x;

    /* Coverage. */
    float coverageIntensity = 1-settings.coverageIntensity;
    if (coverageIntensity > FLT_EPSILON) {
      float2 coverageUV = geometry.mapCoordinate(p, settings.coverageTile, settings.coverageOffset).xz;
      float coverage = SAMPLE_TEXTURE2D_LOD(_CloudCoverage2D, s_linear_repeat_sampler,
        coverageUV, mipLevel).x;
      coverage = coverageIntensity * coverage;
      sample = max(0, NoiseUtilities::remap(sample, coverage, 1, 0, 1));
    }

    return sample;
  }

  static float takeMediaSample2DLowLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry) {
    return takeMediaSample2DLowLOD(settings, p, geometry, 0);
  }

  /* Samples density textures at point p at specified mip level. */
  static float takeMediaSample2DMediumLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry, int mipLevel) {
    /* Base. */
    float sample = takeMediaSample2DLowLOD(settings, p, geometry, mipLevel);

    /* Structure. */
    if (settings.structureIntensity > FLT_EPSILON || settings.structureMultiply > FLT_EPSILON) {
      float2 cloudStructureUV = geometry.mapCoordinate(p, settings.structureTile, settings.structureOffset).xz;
      float structure = SAMPLE_TEXTURE2D_LOD(_CloudStructure2D, s_linear_repeat_sampler,
        cloudStructureUV, mipLevel).x;
      // sample = max(0, NoiseUtilities::remap(sample, structure, 1, 0, 1));
      sample = max(0, NoiseUtilities::remap(sample, settings.structureIntensity * structure, 1, 0, 1));
      sample = max(0, sample * pow(structure, 4 * max(0.001, settings.structureMultiply)));
    }

    return sample;
  }

  static float takeMediaSample2DMediumLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry) {
    return takeMediaSample2DMediumLOD(settings, p, geometry, 0);
  }

  /* Samples density textures at point p at specified mip level. Assumes p is in
   * bounds. */
  static float takeMediaSample2DHighLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry, int mipLevel) {
    /* Base and structure. */
    float sample = takeMediaSample2DMediumLOD(settings, p, geometry, mipLevel);

    /* Detail. */
    if (settings.detailIntensity > FLT_EPSILON || settings.detailMultiply > FLT_EPSILON) {
      float2 detailWarp = float2(0, 0);
      if (settings.detailWarpIntensity > FLT_EPSILON) {
        const float kDetailWarpMax = 0.1;
        float2 detailWarpUV = geometry.mapCoordinate(p, settings.detailWarpTile, settings.detailWarpOffset).xz;
        detailWarp = settings.detailWarpIntensity * kDetailWarpMax
          * SAMPLE_TEXTURE2D_LOD(_CloudDetailWarp2D, s_linear_repeat_sampler,
          detailWarpUV, mipLevel).xy;
      }

      float2 detailUV = frac(geometry.mapCoordinate(p, settings.detailTile, settings.detailOffset).xz - detailWarp);
      float detail = SAMPLE_TEXTURE2D_LOD(_CloudDetail2D, s_linear_repeat_sampler,
        detailUV, mipLevel).x;

      sample = max(0, NoiseUtilities::remap(sample, settings.detailIntensity * detail, 1, 0, 1));
      sample = max(0, sample * pow(detail, 4 * max(0.001, settings.detailMultiply)));
    }

    return sample;
  }

  static float takeMediaSample2DHighLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry) {
    return takeMediaSample2DHighLOD(settings, p, geometry, 0);
  }

  /* Samples density textures at point p at specified mip level.
   * Returns: float2(low lod, height gradient). */
  static float2 takeMediaSample3DLowLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry, int mipLevel) {
    /* Warp. */
    float3 baseWarp = 0;
    if (settings.baseWarpIntensity > FLT_EPSILON) {
      const float kBaseWarpMax = 0.5;
      float3 baseWarpUV = geometry.mapCoordinate(p, settings.baseWarpTile, settings.baseWarpOffset);
      baseWarp = settings.baseWarpIntensity * kBaseWarpMax
        * SAMPLE_TEXTURE3D_LOD(_CloudBaseWarp3D, s_linear_repeat_sampler,
        baseWarpUV, mipLevel).xyz;
    }

    /* Base. */
    float3 baseUV = frac(geometry.mapCoordinate(p, settings.baseTile, settings.baseOffset) - baseWarp);
    float sample = SAMPLE_TEXTURE3D_LOD(_CloudBase3D, s_linear_repeat_sampler,
      baseUV, mipLevel).x;

    /* Compute height gradient early, since we use it in the coverage
     * calculation. */
    float heightGradient = geometry.heightGradient(p);

    /* Coverage. */
    float coverageIntensity = 3*(1-settings.coverageIntensity);
    if (coverageIntensity > FLT_EPSILON) {
      float2 skew = heightGradient * settings.windSkew * 0.01;
      float2 coverageUV = frac(geometry.mapCoordinate(p, settings.coverageTile, settings.coverageOffset).xz + skew);
      float coverage = SAMPLE_TEXTURE2D_LOD(_CloudCoverage2D, s_linear_repeat_sampler,
        coverageUV, mipLevel).x;
      /* Modify the coverage to decrease over height to create nice, domed
       * cumulus clouds. */
      coverage = coverage * (pow(abs(heightGradient), settings.roundingShape) * settings.rounding + 1);
      coverage = saturate(coverageIntensity * coverage);
      sample = saturate(NoiseUtilities::remap(sample, coverage, 1, 0, 1));
    }

    float heightGradientDensity = computeHeightGradient(heightGradient, settings.heightGradientBottom, settings.heightGradientTop);
    sample = NoiseUtilities::remap(sample, saturate(1-heightGradientDensity), 1, 0, 1);

    return float2(sample, heightGradient);
  }

  static float2 takeMediaSample3DLowLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry) {
    return takeMediaSample3DLowLOD(settings, p, geometry, 0);
  }

  /* Samples density textures at point p at specified mip level. Assumes p is in
   * bounds. Returns: float3(medium lod, low lod, height gradient). */
  static float3 takeMediaSample3DMediumLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry, int mipLevel) {
    /* Base. */
    float3 sample = float3(0, takeMediaSample3DLowLOD(settings, p, geometry, mipLevel));
    sample.x = sample.y;

    /* Structure. */
    if (settings.structureIntensity > FLT_EPSILON || settings.structureMultiply > FLT_EPSILON) {
      float3 cloudStructureUV = geometry.mapCoordinate(p, settings.structureTile, settings.structureOffset);
      float structure = SAMPLE_TEXTURE3D_LOD(_CloudStructure3D, s_linear_repeat_sampler,
        cloudStructureUV, mipLevel).x;
      sample.x = max(0, NoiseUtilities::remap(sample.x, settings.structureIntensity * structure, 1, 0, 1));
      sample.x = max(0, sample.x * pow(structure, 4 * max(0.001, settings.structureMultiply)));
    }

    return sample;
  }

  /* Returns: float3(medium lod, low lod, height gradient). */
  static float3 takeMediaSample3DMediumLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry) {
    return takeMediaSample3DMediumLOD(settings, p, geometry, 0);
  }

  /* Samples density textures at point p at specified mip level. Assumes p is in
   * bounds. Returns: float3(high lod, medium lod, low lod). */
  static float4 takeMediaSample3DHighLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry, int mipLevel) {
    /* Base and structure. */
    float4 sample = float4(0, takeMediaSample3DMediumLOD(settings, p, geometry, mipLevel));
    sample.x = sample.y;

    /* Detail. */
    if (settings.detailIntensity > FLT_EPSILON || settings.detailMultiply > FLT_EPSILON) {
      float3 detailWarp = 0;
      if (settings.detailWarpIntensity > FLT_EPSILON) {
        const float kDetailWarpMax = 0.1;
        float3 detailWarpUV = geometry.mapCoordinate(p, settings.detailWarpTile, settings.detailWarpOffset);
        detailWarp = settings.detailWarpIntensity * kDetailWarpMax
          * SAMPLE_TEXTURE3D_LOD(_CloudDetailWarp3D, s_linear_repeat_sampler,
          detailWarpUV, mipLevel).xyz;
      }

      float3 detailUV = frac(geometry.mapCoordinate(p, settings.detailTile, settings.detailOffset) - detailWarp);
      float detail = SAMPLE_TEXTURE3D_LOD(_CloudDetail3D, s_linear_repeat_sampler,
        detailUV, mipLevel).x;
      sample.x = max(0, NoiseUtilities::remap(sample.x, settings.detailIntensity * detail, 1, 0, 1));
      sample.x = max(0, sample.x * pow(detail, 4 * max(0.001, settings.detailMultiply)));
    }

    return sample;
  }

  /* Returns: float4(high lod, medium lod, low lod, height gradient). */
  static float4 takeMediaSample3DHighLOD(UniversalCloudLayerRenderSettings settings,
    float3 p, ICloudGeometry geometry) {
    return takeMediaSample3DHighLOD(settings, p, geometry, 0);
  }

  static float3 lightCloudLayer2D(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 p, float3 d, float opticalDepth, float3 ambient) {
    float3 cumulativeLighting = float3(0, 0, 0);

    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];

    /* Precompute what we can. */
    float3 cloudTransmittance = 1 - exp(-opticalDepth * settings.extinctionCoefficients);
    cloudTransmittance *= settings.scatteringCoefficients/Utilities::clampAboveZero(settings.extinctionCoefficients);

    /* Loop over all celestial bodies. */
    for (int l = 0; l < _ExpanseNumCloudDirectionalLights; l++) {
      DirectionalLightRenderSettings light = _ExpanseCloudDirectionalLights[l];
      float3 L = light.direction;

      /* Check for occlusion by the planet. */
      SkyIntersectionData lightIntersection = AtmosphereGeometry::traceSkyVolume(p, L, planet.radius, planet.atmosphereRadius);
      if (lightIntersection.groundHit) {
        continue;
      }

      /* For shadow blur and sky transmittance. */
      float r = length(p);
      float mu = dot(normalize(p), L);

      /* Planet shadow blur to smooth out the transition from day to night. */
      const float blurDistance = 0.025;
      float shadowBlur = Atmosphere::computeShadowBlur(r, mu, blurDistance, planet.radius);

      float2 lightTCoord = AtmosphereMapping::mapTransmittanceCoord(r, mu,
        planet.atmosphereRadius, planet.radius,
        lightIntersection.endT - lightIntersection.startT, false, _atmosphereSettingsBuffer[0].resT.y);
      float3 skyTransmittance = Atmosphere::sampleTransmittance(lightTCoord);

      float3 screenspaceTransmittance = Atmosphere::ComputeScreenspaceTransmittance(p, L, 
        lightIntersection.endT - lightIntersection.startT, true,
        _atmosphereSettingsBuffer[0]);

      float dot_L_d = dot(L, d);

      /* If we are permitted, take a few samples at increasing mip levels to get
       * some self shadowing. */
      float3 selfShadow = float3(1, 1, 1);
      if (settings.selfShadowing) {
        const int numShadowPoints = 5;
        const float shadowPoints[5] = {0.01, 0.04, 0.10, 0.22, 0.655};
        const float shadowWidths[5] = {0.02, 0.04, 0.08, 0.16, 0.69};
        float lightDirectionRampdown = settings.multipleScatteringRampDown * pow(saturate(dot_L_d), settings.multipleScatteringRampDownShape);
        /* Figure out where the light direction would leave the
         * cloud volume. */
        float2 lightExit = geometry.intersect3D(p, L);
        float shadowDistance = max(0, min(1500, lightExit.y));
        float opticalDepthMultiplier = settings.density * shadowDistance;
        float shadowOpticalDepth = 0;
        for (int j = 0; j < numShadowPoints; j++) {
          float3 shadowSamplePoint = p + L * shadowPoints[j] * shadowDistance;
          float shadowSample = takeMediaSample2DMediumLOD(settings, shadowSamplePoint, geometry, 1 + float(j)/2);
          shadowOpticalDepth += opticalDepthMultiplier * shadowSample * shadowWidths[j];
        }
        selfShadow = computeMSModifiedTransmittance(settings.extinctionCoefficients, shadowOpticalDepth, 3, lerp(settings.multipleScatteringBias, 1.0, lightDirectionRampdown), settings.multipleScatteringAmount);
      }

      /* Finally, compute the phase function. */
      float phase = cloudPhaseFunction(dot_L_d, settings.anisotropy, settings.silverIntensity, settings.silverSpread);

      cumulativeLighting += phase * light.lightColor * cloudTransmittance * skyTransmittance * screenspaceTransmittance * shadowBlur * selfShadow;
    }

    /* Loop over all point lights. */
    for (l = 0; l < _ExpanseNumCloudPointLights; l++) {
      PointLightRenderSettings light = _ExpanseCloudPointLights[l];
      float3 lightPlanetSpace = Mapping::transformPointToPlanetSpace(light.position, planet.originOffset, planet.radius);
      float3 sampleToLightWS = lightPlanetSpace - p;
      float attenuation = rcp(max(light.geometryParam1, dot(sampleToLightWS, sampleToLightWS)) + 1);
      float3 lighting = light.lightColor * attenuation;
      lighting = cloudTransmittance * lighting;
      cumulativeLighting += lighting;
    }

    /* Do all the same, but for light pollution, skipping some steps that aren't so
     * important but are very expensive. Namely, self-shadowing and atmosphere
     * transmittance. */
    cumulativeLighting += settings.lightPollutionDimmer * _ExpanseNightSky[0].lightPollution * cloudTransmittance;

    /* Add in the ambient light. */
    float3 ambientLighting = ambient * cloudTransmittance * settings.ambient;
    cumulativeLighting += ambientLighting;

    return cumulativeLighting;
  }

  static float3 lightCloudLayer3D(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 p, float3 d, float4 mediaSample, float3 lightingTransmittance, 
    float3 totalLightingTransmittance, float3 ambient, int frameCount) {
    int l;
    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];
    
    /* Total lighting from all bodies. */
    float3 cumulativeLighting = float3(0, 0, 0);

    /* Precompute parameters that are light independent. */
    float loddedDensity = mediaSample[settings.depthProbabilityDetailIndex];
    float height = mediaSample.w;
    float verticalInScatterProbability = computeVerticalInScatterProbability(height,
      settings.verticalProbabilityHeightRange, settings.verticalProbabilityStrength);
    float depthInScatterProbability = computeDepthInScatterProbability(loddedDensity, height,
      settings.depthProbabilityHeightRange, settings.depthProbabilityStrengthRange, settings.depthProbabilityDensityMultiplier, settings.depthProbabilityBias);
    float inScatterProbability = depthInScatterProbability * verticalInScatterProbability;
    float r = length(p);
    float3 clampedAbsorption = Utilities::clampAboveZero(settings.extinctionCoefficients);

    /* Loop over all directional lights. */
    for (l = 0; l < _ExpanseNumCloudDirectionalLights; l++) {
      DirectionalLightRenderSettings light = _ExpanseCloudDirectionalLights[l];
      float3 L = light.direction;

      /* Check for occlusion by the planet. */
      SkyIntersectionData lightIntersection = AtmosphereGeometry::traceSkyVolume(p, L, planet.radius, planet.atmosphereRadius);
      if (lightIntersection.groundHit) {
        continue;
      }

      /* The cloud lighting model is purely attenuative. We start with the
       * full brightness of the light. */
      float monochromeLightAttenuation = 1.0;
      float3 lighting = light.lightColor;

      /* Next, we attenuate according to the cloud's phase function. */
      float dot_L_d = dot(L, d);
      monochromeLightAttenuation *= cloudPhaseFunction(dot_L_d, settings.anisotropy, settings.silverIntensity, settings.silverSpread);

      /* In order to account for in-scattering probability, we use the lodded
       * density hack proposed by Andrew Schneider in his Nubis system. */
      monochromeLightAttenuation *= inScatterProbability;

      /* For shadow blur and sky transmittance. */
      float mu = dot(normalize(p), L);

      /* Planet shadow blur to smooth out the transition from day to night. */
      const float blurDistance = 0.025;
      monochromeLightAttenuation *= Atmosphere::computeShadowBlur(r, mu, blurDistance, planet.radius);

      /* Apply the monochrome attenuation we've calculated so far. */
      lighting *= monochromeLightAttenuation;

      float2 lightTCoord = AtmosphereMapping::mapTransmittanceCoord(r, mu,
        planet.atmosphereRadius, planet.radius,
        lightIntersection.endT - lightIntersection.startT, false, _atmosphereSettingsBuffer[0].resT.y);
      lighting *= Atmosphere::sampleTransmittance(lightTCoord);

      lighting *= Atmosphere::ComputeScreenspaceTransmittance(p, L, 
        lightIntersection.endT - lightIntersection.startT, true,
        _atmosphereSettingsBuffer[0]);

      /* If we are permitted, take a few samples at increasing mip levels to
       * model self-shadowing. */
      if (settings.selfShadowing) {
        float blueNoise = Random::random_4_1(float4(p.x, p.y, p.z, (uint(frameCount) % 256) / 256.0));
        float lightDirectionRampdown = settings.multipleScatteringRampDown * pow(saturate(dot_L_d), settings.multipleScatteringRampDownShape);
        float shadowOpticalDepth = 0;
        if (settings.highQualityShadows) {
          static const int numShadowPoints = 5;
          static const float shadowPoints[6] = {0, 0.01, 0.04, 0.10, 0.22, 0.655};
          static const float shadowWidths[6] = {0, 0.02, 0.04, 0.08, 0.16, 0.69};
          float opticalDepthMultiplier = settings.density * settings.maxSelfShadowDistance;
          for (int j = 1; j < numShadowPoints; j++) {
            float3 shadowSamplePoint = p + L * (shadowPoints[j - 1] + (shadowPoints[j] - shadowPoints[j - 1]) * blueNoise) * settings.maxSelfShadowDistance;
            if (geometry.inBounds(shadowSamplePoint)) {
              float shadowSample = takeMediaSample3DHighLOD(settings, shadowSamplePoint, geometry, float(j-1)/3).x;
              shadowOpticalDepth += shadowWidths[j] * shadowSample * opticalDepthMultiplier;
            }
          }
        } else {
          static const int numShadowPoints = 5;
          static const float shadowPoints[6] = {0, 0.01, 0.04, 0.10, 0.22, 0.655};
          static const float shadowWidths[6] = {0, 0.02, 0.04, 0.08, 0.16, 0.69};
          float opticalDepthMultiplier = settings.density * settings.maxSelfShadowDistance;
          for (int j = 1; j < numShadowPoints; j++) {
            float3 shadowSamplePoint = p + L * (shadowPoints[j - 1] + (shadowPoints[j] - shadowPoints[j - 1]) * blueNoise) * settings.maxSelfShadowDistance;
            if (geometry.inBounds(shadowSamplePoint)) {
              float shadowSample = takeMediaSample3DMediumLOD(settings, shadowSamplePoint, geometry, float(j-1)/3).x;
              shadowOpticalDepth += shadowWidths[j] * shadowSample * opticalDepthMultiplier;
            }
          }
        }
        lighting *= computeMSModifiedTransmittance(settings.extinctionCoefficients, shadowOpticalDepth, 3, lerp(settings.multipleScatteringBias, 1.0, lightDirectionRampdown), settings.multipleScatteringAmount);
      }

      /* The final step is to integrate the attenuated luminance we've
       * calculated according to the cloud's density. We do this using
       * Sebastien Hillaire's improved integration method. */
      lighting = totalLightingTransmittance * (lighting - lightingTransmittance * lighting) * (settings.scatteringCoefficients/clampedAbsorption);

      cumulativeLighting += lighting;
    }

    /* Loop over all point lights. */
    for (l = 0; l < _ExpanseNumCloudPointLights; l++) {
      PointLightRenderSettings light = _ExpanseCloudPointLights[l];
      float3 lightPlanetSpace = Mapping::transformPointToPlanetSpace(light.position, planet.originOffset, planet.radius);
      float3 sampleToLightWS = lightPlanetSpace - p;
      float attenuation = rcp(max(light.geometryParam1, dot(sampleToLightWS, sampleToLightWS)) + 1);
      float3 lighting = light.lightColor * attenuation;
      lighting = totalLightingTransmittance * (lighting - lightingTransmittance * lighting) * (settings.scatteringCoefficients/clampedAbsorption);
      cumulativeLighting += lighting;
    }

    /* Do all the same, but for light pollution, skipping some steps that aren't so
     * important but are very expensive. Namely, self-shadowing and atmosphere
     * transmittance. */
    float3 lightPollution = inScatterProbability * settings.lightPollutionDimmer * _ExpanseNightSky[0].lightPollution;
    cumulativeLighting += totalLightingTransmittance * (lightPollution - lightingTransmittance * lightPollution) * (settings.scatteringCoefficients/clampedAbsorption);

    /* Add in the ambient light. */
    ambient *= inScatterProbability * clamp(NoiseUtilities::remap(height, settings.ambientHeightRange.x, settings.ambientHeightRange.y, settings.ambientStrengthRange.x, settings.ambientStrengthRange.y), settings.ambientStrengthRange.x, settings.ambientStrengthRange.y);
    float3 ambientLighting = totalLightingTransmittance * (ambient - lightingTransmittance * ambient) * (settings.scatteringCoefficients/clampedAbsorption);
    cumulativeLighting += ambientLighting;

    return cumulativeLighting;

  }

  static CloudResult raymarchCloudLayer3D(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 o, float3 start, float3 d, float marchDistance,
    float blueNoise, float3 ambient, int frameCount) {
    /* Initialize to null result. */
    CloudResult result;
    result.lighting = 0;
    result.transmittance = 1;
    result.t = 0;
    result.geoHit = -1;

    float tMarched = (2 * blueNoise - 1) * marchDistance * settings.detailStepSize * 2;
    bool marchCoarse = true;
    int samplesTaken = 0;
    int consecutiveZeroSamples = 0;
    float monochromeTransmittance = 1;
    float summedWeight = 0;
    float attenuation = 1;

    while (monochromeTransmittance > settings.transmittanceZeroThreshold
      && tMarched < marchDistance && attenuation > settings.mediaZeroThreshold && samplesTaken < 512) { // HACK: hard upper limit on sample count
      if (marchCoarse) {
        /* Take a test coarse sample. */
        float ds = settings.coarseStepSize * marchDistance;
        float3 testPoint = start + d * (tMarched + 0.5 * ds);

        // Compute attenuation, so we stop marching if our maximum density sample is below the media zero threshold.
        attenuation = geometry.densityAttenuation(testPoint, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);

        /* Only test a sample if we are in bounds. */
        bool inBounds = geometry.inBounds(testPoint);
        marchCoarse = !inBounds;
        if (inBounds) {
          float testSample = takeMediaSample3DLowLOD(settings, testPoint, geometry).x;
          marchCoarse = testSample < settings.mediaZeroThreshold;
        }
        if (marchCoarse) {
          /* If it's zero, keep up with the coarse marching. */
          tMarched += ds;
          samplesTaken++;
          continue;
        }
      }

      /* Take a detailed sample. Set to zero if we are out of bounds. */
      float ds = settings.detailStepSize * marchDistance;
      float3 p = start + d * (tMarched + 0.5 * ds);
      float4 mediaSample = takeMediaSample3DHighLOD(settings, p, geometry, 0);
      attenuation = geometry.densityAttenuation(p, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);
      mediaSample.x *= attenuation;

      /* If it's zero, skip. If it's been zero for a while, switch back to
       * coarse marching. */
      if (mediaSample.x < settings.mediaZeroThreshold) {
        consecutiveZeroSamples++;
        marchCoarse = consecutiveZeroSamples > settings.maxConsecutiveZeroSamples;
        tMarched += ds;
        samplesTaken++;
        continue;
      }

      /* Compute transmittance. */
      float opticalDepth = settings.density * mediaSample.x * ds;
      float3 transmittance = exp(-settings.extinctionCoefficients * opticalDepth);

      /* Accumulate lighting. */
      result.lighting += lightCloudLayer3D(settings, geometry, p, d, mediaSample, transmittance, result.transmittance, ambient, frameCount);

      /* Accumulate our weighted average for t_hit, based on this point's transmittance. */
      float weight = monochromeTransmittance * (1 - Utilities::average(transmittance));
      result.t += weight * (tMarched + 0.5 * ds);
      summedWeight += weight;

      /* Accumulate transmittance and compute monochrome transmittance for loop check. */
      result.transmittance *= transmittance;
      monochromeTransmittance = Utilities::average(result.transmittance);

      tMarched += ds;
      samplesTaken++;

    };

    result.t /= Utilities::clampAboveZero(summedWeight);
    if (result.t < FLT_EPSILON) {
      result.t = marchDistance;
    }

    return result;
  }

  static CloudResult raymarchCloudLayer3DInterior(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 o, float3 start, float3 d, float marchDistance,
    float blueNoise, float3 ambient, int frameCount) {
    /* Initialize to null result. */
    CloudResult result;
    result.lighting = 0;
    result.transmittance = 1;
    result.t = 0;
    result.geoHit = -1;
    float attenuation = 1;

    /* Initial interior step sizes. */
    float kFlythroughCoarseMultiplier = 8;
    settings.detailStepSize = settings.flythroughStepRange.x / marchDistance;
    settings.coarseStepSize = kFlythroughCoarseMultiplier * settings.detailStepSize;

    float tMarched = (2 * blueNoise - 1) * marchDistance * settings.detailStepSize * 2;
    bool marchCoarse = true;
    int samplesTaken = 0;
    int consecutiveZeroSamples = 0;
    float monochromeTransmittance = 1;
    float summedWeight = 0;

    while (monochromeTransmittance > settings.transmittanceZeroThreshold
      && tMarched < marchDistance && attenuation > settings.mediaZeroThreshold && samplesTaken < 512) { // HACK: hard upper limit on sample count
      /* Compute step sizes based on distance heuristic. */  
      settings.detailStepSize = lerp(settings.flythroughStepRange.x, 
        settings.flythroughStepRange.y, 
        saturate((tMarched - settings.flythroughStepDistanceRange.x) 
          / (settings.flythroughStepDistanceRange.y - settings.flythroughStepDistanceRange.x))) 
        / marchDistance;
      settings.coarseStepSize = kFlythroughCoarseMultiplier * settings.detailStepSize;
      
      if (marchCoarse) {
        /* Take a test coarse sample. */
        float ds = settings.coarseStepSize * marchDistance;
        float3 testPoint = start + d * (tMarched + 0.5 * ds);

        // Compute attenuation, so we stop marching if our maximum density sample is below the media zero threshold.
        attenuation = geometry.densityAttenuation(testPoint, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);

        /* Only test a sample if we are in bounds. */
        bool inBounds = geometry.inBounds(testPoint);
        marchCoarse = !inBounds;
        if (inBounds) {
          float testSample = takeMediaSample3DLowLOD(settings, testPoint, geometry).x;
          marchCoarse = testSample < settings.mediaZeroThreshold;
        }
        if (marchCoarse) {
          /* If it's zero, keep up with the coarse marching. */
          tMarched += ds;
          samplesTaken++;
          continue;
        }
      }

      /* Take a detailed sample. Set to zero if we are out of bounds. */
      float ds = settings.detailStepSize * marchDistance;
      float3 p = start + d * (tMarched + 0.5 * ds);
      attenuation = geometry.densityAttenuation(p, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);
      float4 mediaSample = takeMediaSample3DHighLOD(settings, p, geometry, 0);
      mediaSample.x *= attenuation;

      /* If it's zero, skip. If it's been zero for a while, switch back to
       * coarse marching. */
      if (mediaSample.x < settings.mediaZeroThreshold) {
        consecutiveZeroSamples++;
        marchCoarse = consecutiveZeroSamples > settings.maxConsecutiveZeroSamples;
        tMarched += ds;
        samplesTaken++;
        continue;
      }

      /* Compute transmittance. */
      float opticalDepth = settings.density * mediaSample.x * ds;
      float3 transmittance = exp(-settings.extinctionCoefficients * opticalDepth);

      /* Accumulate lighting. */
      result.lighting += lightCloudLayer3D(settings, geometry, p, d, mediaSample, transmittance, result.transmittance, ambient, frameCount);

      /* Accumulate our weighted average for t_hit, based on this point's transmittance. */
      float weight = monochromeTransmittance * (1 - Utilities::average(transmittance));
      result.t += weight * (tMarched + 0.5 * ds);
      summedWeight += weight;

      /* Accumulate transmittance and compute monochrome transmittance for loop check. */
      result.transmittance *= transmittance;
      monochromeTransmittance = Utilities::average(result.transmittance);

      tMarched += ds;
      samplesTaken++;

    };

    result.t /= Utilities::clampAboveZero(summedWeight);
    if (result.t < FLT_EPSILON) {
      result.t = marchDistance;
    }
    
    return result;
  }

  static CloudResult raymarchCloudLayerShadowMap3D(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 start, float3 d, float marchDistance) {
    /* Initialize to null result. */
    CloudResult result;
    result.lighting = 0;
    result.transmittance = 1;
    result.t = 0;
    result.geoHit = -1;
    float attenuation = 1;

    float tMarched = 0.0;
    bool marchCoarse = true;
    int samplesTaken = 0;
    int consecutiveZeroSamples = 0;
    float monochromeTransmittance = 1;
    float summedMonochromeTransmittance = 0;

    while (monochromeTransmittance > settings.transmittanceZeroThreshold
      && tMarched < marchDistance && attenuation > settings.mediaZeroThreshold && samplesTaken < 512) { // HACK: hard upper limit on sample count

      if (marchCoarse) {
        /* Take a test coarse sample. */
        float ds = settings.coarseStepSize * marchDistance;
        float3 testPoint = start + d * (tMarched + 0.5 * ds);

        // Compute attenuation, so we stop marching if our maximum density sample is below the media zero threshold.
        attenuation = geometry.densityAttenuation(testPoint, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);

        float testSample = takeMediaSample3DLowLOD(settings, testPoint, geometry).x;
        /* If it's zero, keep up with the coarse marching. */
        if (testSample < settings.mediaZeroThreshold) {
          tMarched += ds;
          samplesTaken++;
          continue;
        }
        /* Otherwise switch to detailed marching. */
        marchCoarse = false;
      }

      /* Take a detailed sample. */
      float ds = settings.detailStepSize * marchDistance;
      float3 p = start + d * (tMarched + 0.5 * ds);
      attenuation = geometry.densityAttenuation(p, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);
      float4 mediaSample = takeMediaSample3DHighLOD(settings, p, geometry);
      mediaSample.x *= attenuation;

      /* If it's zero, skip. If it's been zero for a while, switch back to
       * coarse marching. */
      if (mediaSample.x < settings.mediaZeroThreshold) {
        consecutiveZeroSamples++;
        marchCoarse = consecutiveZeroSamples > settings.maxConsecutiveZeroSamples;
        tMarched += ds;
        samplesTaken++;
        continue;
      }

      /* Compute transmittance---including a modified transmittance used
       * in the lighting calculation to simulate multiple scattering. */
      float opticalDepth = settings.density * mediaSample.x * ds;
      float3 transmittance = exp(-settings.extinctionCoefficients * opticalDepth);

      /* Accumulate transmittance. */
      result.transmittance *= transmittance;

      /* Compute monochrome transmittance for loop check. */
      monochromeTransmittance = Utilities::average(result.transmittance);

      /* Accumulate our weighted average for t_hit. */
      result.t += monochromeTransmittance * (tMarched + 0.5 * ds);
      summedMonochromeTransmittance += monochromeTransmittance;

      tMarched += ds;
      samplesTaken++;

    };

    result.t /= Utilities::clampAboveZero(summedMonochromeTransmittance);
    if (result.t < FLT_EPSILON) {
      result.t = marchDistance;
    }
    return result;
  }

  static CloudResult renderCloudsShadowMap2D(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 o, float3 d) {
    /* Initialize to null result. */
    CloudResult result;
    result.lighting = 0;
    result.transmittance = 1;
    result.t = -1;
    result.geoHit = -1;

    result.t = geometry.intersect(o, d).x;
    if (result.t < 0) {
      return result;
    }


    float3 sample = o + d * result.t;
    // HACK: this mip level is a hack but it looks good. It should definitely
    // be based on texture resolution though.
    int mipLevel = clamp(length(sample.xz - o.xz) / 50000.0, 0, 8);
    float mediaSample = takeMediaSample2DHighLOD(settings, sample, geometry, mipLevel);
    mediaSample.x *= geometry.densityAttenuation(sample, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);
    float opticalDepth = mediaSample * settings.apparentThickness * settings.density;

    result.transmittance = exp(-opticalDepth * settings.extinctionCoefficients);
    result.geoHit = result.t;

    return result;
  }

  static CloudResult renderClouds2D(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 o, float3 d, float3 ambient) {
    /* Initialize to null result. */
    CloudResult result;
    result.lighting = 0;
    result.transmittance = 1;
    result.t = -1;
    result.geoHit = -1;

    result.t = geometry.intersect(o, d).x;
    if (result.t < 0) {
      return result;
    }

    float3 sample = o + d * result.t;
    // HACK: this mip level is a hack but it looks good. It should definitely
    // be based on texture resolution though.
    int mipLevel = clamp(length(sample.xz - o.xz) / 50000.0, 0, 8);
    float mediaSample = takeMediaSample2DHighLOD(settings, sample, geometry, mipLevel);
    mediaSample.x *= geometry.densityAttenuation(sample, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);
    float opticalDepth = mediaSample * settings.apparentThickness * settings.density;

    result.transmittance = exp(-opticalDepth * settings.extinctionCoefficients);

    result.lighting = lightCloudLayer2D(settings, geometry, sample, d, opticalDepth, ambient);

    if (settings.celShade) {
      const float tBand0 = Utilities::clampAboveZero(settings.celShadeTransmittanceBands);
      float transmittance = exp(floor(log(Utilities::average(result.transmittance)) / tBand0) * tBand0);
      result.transmittance = transmittance * normalize(result.transmittance);

      float brightness = Utilities::average(result.lighting);
      const float band0 = settings.celShadeLightingBands;
      float lowBand = floor(log(brightness)/band0);
      float highBand = ceil(log(brightness)/band0);
      float smoothness = 0.05;
      float band = lowBand + (highBand - lowBand) * smoothstep(lowBand + (highBand - lowBand) * (1 - smoothness), highBand, log(brightness)/band0);
      brightness = exp(band * band0);
      result.lighting = max(0, brightness * normalize(result.lighting));
    }

    /* We've intersected geometry, so we will render clouds. TODO: depth. */
    result.geoHit = result.t;

    return result;
  }

  static CloudResult renderClouds3D(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 o, float3 d, float depth, float blueNoise,
    float3 ambient, int frameCount) {
    /* Initialize to null result. */
    CloudResult result;
    result.lighting = 0;
    result.transmittance = 1;
    result.t = -1;
    result.geoHit = -1;

    float2 t = geometry.intersect(o, d);
    if (t.x < 0 && t.y < 0) {
      return result;
    }

    /* Raymarch to accumulate transmittance and lighting, and determine
     * volumetric hit point. */
    float3 start = o + t.x * d;
    float marchDistance = (depth < 0 || depth < t.x) ? t.y - t.x : min(t.y - t.x, depth - t.x);

   
    if (t.x == 0) {
      /* We're inside the clouds. To reduce artifacts, only use a small amount of blue noise.  */
      blueNoise = 0.4 + blueNoise * 0.2;
      /* Ensure we use a sufficiently small step range for close-up objects. */
      settings.flythroughStepRange.x = min(settings.flythroughStepRange.x, marchDistance/8);
      result = raymarchCloudLayer3DInterior(settings, geometry, o, start, d, marchDistance, blueNoise, ambient, frameCount);
    } else {
      /* Compute the step size based on the intersection point distance. */
      float stepLerp = saturate((marchDistance - settings.stepDistanceRange.x) / (settings.stepDistanceRange.y - settings.stepDistanceRange.x));
      settings.detailStepSize = 1.0 / lerp(settings.detailStepRange.x, settings.detailStepRange.y, stepLerp);
      settings.coarseStepSize = max(settings.detailStepSize, 1.0 / lerp(settings.coarseStepRange.x, settings.coarseStepRange.y, stepLerp));
      result = raymarchCloudLayer3D(settings, geometry, o, start, d, marchDistance, blueNoise, ambient, frameCount);
    }

    if (settings.celShade) {
      float attenuation = geometry.densityAttenuation(start, settings.attenuationDistance, settings.attenuationBias, settings.rampUp);
      const float tBand0 = Utilities::clampAboveZero(settings.celShadeTransmittanceBands);
      float transmittance = exp(floor(log(Utilities::average(result.transmittance)) / tBand0) * tBand0);
      result.transmittance = transmittance < 1 ? lerp(settings.celShadeTransmittanceBands, 1, 1-attenuation) : 1;//transmittance * normalize(result.transmittance);

      float brightness = Utilities::average(result.lighting);
      const float band0 = settings.celShadeLightingBands;
      float lowBand = floor(log(brightness)/band0);
      float highBand = ceil(log(brightness)/band0);
      float smoothness = 0.05;
      float band = lowBand + (highBand - lowBand) * smoothstep(lowBand + (highBand - lowBand) * (1 - smoothness), highBand, log(brightness)/band0);
      brightness = exp(band * band0);
      result.lighting = max(0, brightness * normalize(result.lighting));
    }

    /* Only render if our depth is greater than our start intersection or
     * if we hit no geo at all. */
    result.geoHit = t.x;
    result.t = (result.t < 0) ? result.t : (result.t + t.x);

    return result;
  }

  static CloudResult renderCloudsShadowMap3D(UniversalCloudLayerRenderSettings settings,
    ICloudGeometry geometry, float3 o, float3 d, float depth) {
    /* Initialize to null result. */
    CloudResult result;
    result.lighting = 0;
    result.transmittance = 1;
    result.t = -1;
    result.geoHit = -1;

    float2 t = geometry.intersect(o, d);
    if (t.x < 0 && t.y < 0) {
      return result;
    }

    /* Raymarch to accumulate transmittance and lighting, and determine
     * volumetric hit point. */
    float3 start = o + t.x * d;
    float marchDistance = min(depth - t.x, t.y - t.x);
    /* Compute the step size based on the intersection point distance. */
    float stepLerp = saturate((marchDistance - settings.stepDistanceRange.x) / (settings.stepDistanceRange.y - settings.stepDistanceRange.x));
    settings.detailStepSize = 1.0 / lerp(8, 32, stepLerp);
    settings.coarseStepSize = max(settings.detailStepSize, 1.0 / lerp(4, 16, stepLerp));
    result = raymarchCloudLayerShadowMap3D(settings, geometry, start, d, marchDistance);

    result.geoHit = t.x;

    return result;
  }

};

#endif // EXPANSE_CLOUDS_INCLUDED
