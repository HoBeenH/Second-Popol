#ifndef EXPANSE_ATMOSPHERE_INCLUDED
#define EXPANSE_ATMOSPHERE_INCLUDED

// HACK: define these in the event that they aren't.
#ifndef PUNCTUAL_FILTER_ALGORITHM
#define PUNCTUAL_FILTER_ALGORITHM(sd, posSS, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_3x3(_ShadowAtlasSize.zwxy, posTC, sampleBias, tex, samp)
#endif
#ifndef DIRECTIONAL_FILTER_ALGORITHM
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posSS, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_5x5(_CascadeShadowAtlasSize.zwxy, posTC, sampleBias, tex, samp)
#endif
#ifndef AREA_FILTER_ALGORITHM
#define AREA_FILTER_ALGORITHM(sd, posSS, posTC, tex, samp, bias) SampleShadow_EVSM_1tap(posTC, sd.shadowFilterParams0.y, sd.shadowFilterParams0.z, sd.shadowFilterParams0.xx, false, tex, s_linear_clamp_sampler)
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
// We need to include this "for reasons"...
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

/* Expanse globals */
#include "../directLight/planet/PlanetGlobals.hlsl"
#include "../directLight/general/NightSkyGlobals.hlsl"
#include "../main/QualityGlobals.hlsl"
#include "../lighting/LightingGlobals.hlsl"
#include "../clouds/CloudGlobalTextures.hlsl"
#include "AerialPerspectiveGlobals.hlsl"
#include "AtmosphereGlobals.hlsl"

#include "../common/Geometry.hlsl"
#include "../common/Utilities.hlsl"
#include "../common/Mapping.hlsl"
#include "../lighting/LightGeometry.hlsl"
#include "AtmosphereDatatypes.cs.hlsl"
#include "AtmosphereRenderer.cs.hlsl"
#include "AtmosphereMapping.hlsl"
#include "AtmosphereGeometry.hlsl"

/**
 * @brief: when we compute single scattering, we need to compute it with
 * shadows and with no shadows, to use in our multiple scattering computation.
 * This doesn't hurt efficiency since computing it with no shadows is just
 * a matter of storing an earlier result.
 * */
struct SSResult {
  float3 shadows;
  float3 noShadows;
};
struct SSLayersResult {
  float3 shadows[MAX_ATMOSPHERE_LAYERS];
  float3 noShadows[MAX_ATMOSPHERE_LAYERS];
};
struct MSLayersResult {
  float3 scattering[MAX_ATMOSPHERE_LAYERS];
};

/* Global texture handles to bind T, MS, sky view and T lights to so we can use
 * interpolating samplers on them. */
TEXTURE2D(_TTex);
TEXTURE2D(_MSTex);
TEXTURE2D(_SkyViewTex);
TEXTURE2D(_TLightsTex);
TEXTURE2D(_DownsampledDepthTex);
TEXTURE2D(_CloudTransmittance);
TEXTURE2D_ARRAY(_CloudTransmittanceArray);
TEXTURE2D(_CloudLightAttenuation);

/**
 * @brief: static class for computing atmospheric scattering.
 * */
class Atmosphere {

  static bool phaseDirectional(int phaseFunction) {
    return phaseFunction == PHASEFUNCTION_MIE;
  }

  static bool useImportanceSampling(int densityDistribution) {
    return densityDistribution == DENSITYDISTRIBUTION_EXPONENTIAL;
  }

  static float3 sampleTransmittance(float2 uv) {
    return exp(SAMPLE_TEXTURE2D_LOD(_TTex, s_linear_clamp_sampler, uv, 0).xyz);
  }

  static float3 sampleTransmittanceRaw(float2 uv) {
    return SAMPLE_TEXTURE2D_LOD(_TTex, s_linear_clamp_sampler, uv, 0).xyz;
  }

  // TODO: this looks like it may be buggy?
  static float3 sampleTransmittanceRawBetweenTwoPoints(float3 o, float3 d,
    float t, float t_hit, bool groundHit, float atmosphereRadius, float planetRadius,
    float muResolution) {
    float2 oToSample = AtmosphereMapping::mapTransmittanceCoord(length(o),
      dot(normalize(o), d), atmosphereRadius, planetRadius, t_hit, groundHit, muResolution);
    float3 T = sampleTransmittanceRaw(oToSample);
    float3 depthSample = o + d * t;
    float2 sampleOut = AtmosphereMapping::mapTransmittanceCoord(length(depthSample),
      dot(normalize(depthSample), d), atmosphereRadius, planetRadius, t_hit - t, groundHit, muResolution);
    T -= sampleTransmittanceRaw(sampleOut);
    return T;
  }

  static float3 sampleMultipleScattering(float2 uv) {
    return SAMPLE_TEXTURE2D_LOD(_MSTex, s_linear_clamp_sampler, uv, 0).xyz;
  }

  static float isotropicPhase() {
    return 0.25 / PI;
  }

  static float rayleighPhase(float dot_L_d) {
    return 3.0 / (16.0 * PI) * (1.0 + dot_L_d * dot_L_d);
  }

  static float miePhase(float dot_L_d, float g) {
    return 3.0 / (8.0 * PI) * ((1.0 - g * g) * (1.0 + dot_L_d * dot_L_d))
      / ((2.0 + g * g) * pow(abs(1.0 + g * g - 2.0 * g * dot_L_d), 1.5));
  }

  static float computePhase(float dot_L_d, float anisotropy, int type) {
    switch (type) {
      case PHASEFUNCTION_ISOTROPIC:
        return isotropicPhase();
      case PHASEFUNCTION_RAYLEIGH:
        return rayleighPhase(dot_L_d);
      case PHASEFUNCTION_MIE:
        return miePhase(dot_L_d, anisotropy);
      default:
        return isotropicPhase();
    }
  }

  static float computeShadowBlur(float r, float mu, float dist, float planetRadius) {
    float h = r - planetRadius;
    float cos_h = -Utilities::safeSqrt(h * (2 * planetRadius + h)) / (planetRadius + h);
    return min(1, abs(cos_h - mu) / dist);
  }

  /* Computes penumbra shadow factor, between 0 and 1. penumbraRadius is 
   * specified in degrees. */
  static float computePenumbra(float3 samplePoint, float3 L, float penumbraRadius, float planetRadius) {
    const float radians = penumbraRadius * (PI / 180.0);
    float h = length(samplePoint) - planetRadius;
    float theta_h = acos(Utilities::clampCosine(-Utilities::safeSqrt(h * (2 * planetRadius + h)) / (planetRadius + h)));
    float theta_L = acos(dot(L, normalize(samplePoint)));
    // This is physically correct. However, it's both unnecessarily expensive,
    // and it looks kind of bad. So instead we'll use a HACK that makes 
    // the penumbra nice and soft.
    // float theta = 2 * acos(saturate((theta_L - theta_h) / radians));
    // return (theta - sin(theta)) / PI;
    float proportionBelowHorizon = saturate((theta_L - theta_h) / radians);
    return pow(1 - proportionBelowHorizon, 4);
  }

  /**
   * @brief: Computes density at a point for exponentially distributed
   * atmosphere. Assumes the planet is centered at the origin.
   * */
  static float computeDensityExponential(float3 p, float thickness, float density,
    float planetRadius) {
    return density * exp((planetRadius - length(p))/thickness);
  }

  /**
   * @brief: Computes density at a point for tent distributed atmosphere.
   * Assumes the planet is centered at the origin.
   * */
  static float computeDensityTent(float3 p, float height, float thickness,
    float density, float planetRadius) {
    return density * max(0.0,
      1.0 - abs(length(p) - planetRadius - height) / (0.5 * thickness));
  }

  static float computeDensity(float3 p, float planetRadius,
    int densityDistribution, float height, float thickness, float density) {
    switch (densityDistribution) {
      case DENSITYDISTRIBUTION_EXPONENTIAL:
        return computeDensityExponential(p, thickness, density, planetRadius);
      case DENSITYDISTRIBUTION_TENT:
        return computeDensityTent(p, height, thickness, density, planetRadius);
      default:
        return 0;
    }
  }









/******************************************************************************/
/******************************* TRANSMITTANCE ********************************/
/******************************************************************************/

  static float computeOpticalDepth(float3 O, float3 d, float t, float planetRadius,
    int densityDistribution, float height, float thickness, float density,
    int samples, bool importanceSample) {
    /* Only use importance sampling if we use exponential distribution. */
    importanceSample = importanceSample && useImportanceSampling(densityDistribution);
    /* Evaluate integral over curved planet with a midpoint integrator. */
    float acc = 0;
    for (int i = 0; i < samples; i++) {
      /* Compute where along the ray we're going to sample. */
      float2 t_ds = importanceSample ?
         (Utilities::generateCubicSampleFromIndex(i, samples)) :
         (Utilities::generateLinearSampleFromIndex(i, samples));

      /* Compute the point we're going to sample at. */
      float3 pt = O + (d * t_ds.x * t);

      /* Accumulate the density at that point. */
      acc += computeDensity(pt, planetRadius, densityDistribution, height,
        thickness, density)
        * t_ds.y * t;
    }
    return acc;
  }

  /**
   * @brief: Computes raymarched transmittance for a single layer.
   */
  static float3 computeRaymarchedTransmittance(float3 O, float3 d, float t,
    float planetRadius, float3 planetOriginOffset, int samples,
    bool importanceSample, AtmosphereLayerRenderSettings layer) {
    /* Compute optical depth for all enabled atmosphere layers. */
    float opticalDepth = computeOpticalDepth(O, d, t, planetRadius, layer.densityDistribution,
      layer.height, layer.thickness, layer.density, samples, importanceSample);
    return -opticalDepth * layer.extinctionCoefficients;
  }









/******************************************************************************/
/***************************** SINGLE SCATTERING ******************************/
/******************************************************************************/

  static SSLayersResult computeSS(float3 O, float3 d, float dist, float t, bool groundHit,
    float3 L, float penumbraRadius, AtmosphereSettings settings, 
    bool useOcclusionMultiplier, int samples, bool lightPollution,
    float3 cloudAttenuation) {

    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];
    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    /* Loop variables. */
    int i, j;

    /* Final result */
    SSLayersResult result;
    float scaledDensity[MAX_ATMOSPHERE_LAYERS];
    [unroll(MAX_ATMOSPHERE_LAYERS)]
    for (i = 0; i < MAX_ATMOSPHERE_LAYERS; i++) {
      scaledDensity[i] = 0;
      result.shadows[i] = float3(0, 0, 0);
      result.noShadows[i] = float3(0, 0, 0);
    }

    /* Precompute transmittance in direction d to edge of atmosphere, as well
     * as screenspace transmittance. */
    float2 oToSample = AtmosphereMapping::mapTransmittanceCoord(length(O), dot(normalize(O), d),
      planet.atmosphereRadius, planet.radius, t, groundHit, settings.resT.y);
    float3 T_oOut = sampleTransmittanceRaw(oToSample);

    for (i = 0; i < samples; i++) {
      /* Generate the sample. */
      float2 t_ds;
      if (quality.importanceSample) {
        t_ds = Utilities::generateCubicSampleFromIndex(i, samples);
      } else {
        t_ds = Utilities::generateLinearSampleFromIndex(i, samples);
      }
      float sampleT = t_ds.x * dist;
      float ds = t_ds.y;
      float3 samplePoint = O + d * sampleT;
      float3 normalizedSamplePoint = normalize(samplePoint);

      /* Compute the scaled density of the layer at the sample point. */
      for (j = 0; j < _ExpanseNumAtmosphereLayers; j++) {
        scaledDensity[j] = ds * computeDensity(samplePoint, planet.radius,
          _ExpanseAtmosphereLayers[j].densityDistribution, _ExpanseAtmosphereLayers[j].height, 
          _ExpanseAtmosphereLayers[j].thickness, _ExpanseAtmosphereLayers[j].density);
      }

      /* Our transmittance value for O to the sample point is too large---we
       * need to divide out the transmittance from the sample point to the
       * atmosphere edge (or ground). */
      float r = length(samplePoint);
      float mu = Utilities::clampCosine(dot(normalizedSamplePoint, d));
      float2 sampleOut = AtmosphereMapping::mapTransmittanceCoord(r,
        mu, planet.atmosphereRadius, planet.radius, t - sampleT,
        groundHit, settings.resT.y);
      float3 T_oToSample = T_oOut - sampleTransmittanceRaw(sampleOut);

      [unroll(MAX_ATMOSPHERE_LAYERS)]
      for (j = 0; j < _ExpanseNumAtmosphereLayers; j++) {
        result.noShadows[j] += scaledDensity[j] * exp(T_oToSample);
      }

      /* Trace a ray from the sample point to the light to check visibility. */
      SkyIntersectionData lightIntersection = AtmosphereGeometry::traceSkyVolume(samplePoint,
        L, planet.radius, planet.atmosphereRadius);

      if (!lightPollution) {
        /* Either we hit the sky and the light is fully visible, or we hit the ground 
         * and we have to compute the celestial body's penumbra to figure out the 
         * light's visibility factor. */
        float lightVisibility = lightIntersection.groundHit ? computePenumbra(samplePoint, L, penumbraRadius, planet.radius) : 1;
        /* Only compute if we are in partial or no shadow. */
        if (lightVisibility > 0) {
          /* Compute transmittance from the sample to the light hit point and add
           * it to the total transmittance. */
          float2 sampleToL = 0;
          if (lightIntersection.groundHit) {
            sampleToL.x = AtmosphereMapping::map_r_transmittance(r, planet.atmosphereRadius, planet.radius);
            sampleToL.y = 1 - 0.5 * rcp(settings.resT.y);
          } else {
            float mu_L = Utilities::clampCosine(dot(normalizedSamplePoint, L));
            sampleToL = AtmosphereMapping::mapTransmittanceCoord(r,
              mu_L, planet.atmosphereRadius, planet.radius,
              lightIntersection.endT, lightIntersection.groundHit, settings.resT.y);
          }
          
          // Only use screenspace transmittance if we are computing aerial perspective.
          float3 screenspaceTransmittance = useOcclusionMultiplier ? ComputeScreenspaceTransmittance(samplePoint, L, lightIntersection.endT, 
            true, settings) : 1;
          float3 T = exp(T_oToSample + sampleTransmittanceRaw(sampleToL)) * screenspaceTransmittance;

          [unroll(MAX_ATMOSPHERE_LAYERS)]
          for (j = 0; j < _ExpanseNumAtmosphereLayers; j++) {
            result.shadows[j] += scaledDensity[j] * T * cloudAttenuation * lightVisibility;
          }
        }
      } else {
        /* Compute transmittance to the ground. */
        float lengthSamplePoint = length(samplePoint);
        float2 sampleToGround = AtmosphereMapping::mapTransmittanceCoord(lengthSamplePoint,
          -1, planet.atmosphereRadius, planet.radius,
          lengthSamplePoint - planet.radius, true, settings.resT.y);
        float3 T = exp(T_oToSample + sampleTransmittanceRaw(sampleToGround));

        [unroll(MAX_ATMOSPHERE_LAYERS)]
        for (j = 0; j < _ExpanseNumAtmosphereLayers; j++) {
          result.shadows[j] += scaledDensity[j] * T * cloudAttenuation;
        }
      }
    }

    if (useOcclusionMultiplier) {
      AerialPerspectiveRenderSettings apSettings = _ExpanseAerialPerspectiveSettings[0];
      float dot_L_d = dot(L, d);
      float directionalOcclusionMultiplier = apSettings.directionalOcclusionBias
        + (1 - apSettings.directionalOcclusionBias) * pow(1-saturate(dot_L_d),
        apSettings.directionalOcclusionSpread);
      float uniformOcclusionMultiplier = apSettings.uniformOcclusionBias
        + (1 - apSettings.uniformOcclusionBias) * pow(1-saturate(dot_L_d),
        apSettings.uniformOcclusionSpread);
      [unroll(MAX_ATMOSPHERE_LAYERS)]
      for (j = 0; j < _ExpanseNumAtmosphereLayers; j++) {
        if (phaseDirectional(_ExpanseAtmosphereLayers[j].phaseFunction)) {
          result.shadows[j] *= directionalOcclusionMultiplier;
        } else {
          result.shadows[j] *= uniformOcclusionMultiplier;
        }
      }
    }

    return result;
  }

  /* Doesn't use phase function or light color. */
  static SSResult computeSSForMS(float3 O, float3 d, float dist, float t,
    bool groundHit, float3 L, AtmosphereSettings settings) {

    QualityRenderSettings quality = _ExpanseQualitySettings[0];
    
    SSLayersResult layersResult = Atmosphere::computeSS(O, d, dist, t, groundHit,
      L, 1, settings, false, quality.samplesMSAcc, false, 1);

    SSResult result;
    result.shadows = float3(0, 0, 0);
    result.noShadows = float3(0, 0, 0);
    for (int i = 0; i < _ExpanseNumAtmosphereLayers; i++) {
      AtmosphereLayerRenderSettings layer = _ExpanseAtmosphereLayers[i];
      result.shadows += layer.scatteringCoefficients * (2.0 * layer.tint) * layersResult.shadows[i];
      result.noShadows += layer.scatteringCoefficients * (2.0 * layer.tint) * layersResult.noShadows[i];
    }

    result.shadows *= dist;
    result.noShadows *= dist;
    return result;
  }

  /* Computes single scattering for a single celestial body, across all
   * layers. */
  static SSResult computeSSBody(float3 O, float3 d, float dist, float t,
    bool groundHit, bool useOcclusionMultiplier, float3 L, float penumbraRadius, 
    float3 lightColor, int bodyIndex, AtmosphereSettings settings) {

    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    const bool useCloudAttenuation = useOcclusionMultiplier;
    float3 cloudAttenuation = 1;
    if (useCloudAttenuation) {
      cloudAttenuation = LOAD_TEXTURE2D_LOD(_CloudLightAttenuation, uint2(bodyIndex, 0), 0).xyz;
    }

    SSLayersResult layersResult = Atmosphere::computeSS(O, d, dist, t, groundHit,
      L, penumbraRadius, settings, useOcclusionMultiplier, quality.samplesSS, false, cloudAttenuation);

    float dot_L_d = dot(L, d);
    SSResult result;
    result.shadows = float3(0, 0, 0);
    result.noShadows = float3(0, 0, 0);
    for (int i = 0; i < _ExpanseNumAtmosphereLayers; i++) {
      AtmosphereLayerRenderSettings layer = _ExpanseAtmosphereLayers[i];
      float phase = computePhase(dot_L_d, layer.anisotropy, layer.phaseFunction);
      result.shadows += phase * layer.scatteringCoefficients * (2.0 * layer.tint) * layersResult.shadows[i];
      result.noShadows += phase * layer.scatteringCoefficients * (2.0 * layer.tint) * layersResult.noShadows[i];
    }

    result.shadows *= lightColor * dist;
    result.noShadows *= lightColor * dist;
    return result;
  }

  static SSResult computeSSLP(float3 O, float3 d, float dist, float t,
    bool groundHit, bool useOcclusionMultiplier, float3 groundEmission,
    AtmosphereSettings settings) {

    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    SSLayersResult layersResult = Atmosphere::computeSS(O, d, dist, t, groundHit,
      float3(1, 0, 0), 1, settings, useOcclusionMultiplier, quality.samplesSS, true, 1);

    SSResult result;
    result.shadows = float3(0, 0, 0);
    result.noShadows = float3(0, 0, 0);
    for (int i = 0; i < _ExpanseNumAtmosphereLayers; i++) {
      AtmosphereLayerRenderSettings layer = _ExpanseAtmosphereLayers[i];
      result.shadows += layer.scatteringCoefficients * (2.0 * layer.tint) * layersResult.shadows[i];
      result.noShadows += layer.scatteringCoefficients * (2.0 * layer.tint) * layersResult.noShadows[i];
    }

    result.shadows *= groundEmission * dist;
    result.noShadows *= groundEmission * dist;
    return result;
  }

  static SSResult computeSS(float3 O, float3 d, float dist, float t,
    bool groundHit, bool useOcclusionMultiplier, float nightScatterMultiplier,
    AtmosphereSettings settings) {
    SSResult result;
    result.shadows = float3(0, 0, 0);
    result.noShadows = float3(0, 0, 0);
    for (int i = 0; i < _ExpanseNumAtmosphereDirectionalLights; i++) {
      DirectionalLightRenderSettings light = _ExpanseAtmosphereDirectionalLights[i];
      SSResult bodySS = computeSSBody(O, d, dist, t, groundHit,
        useOcclusionMultiplier, light.direction, light.penumbraRadius, light.lightColor, i, settings);
      result.shadows += bodySS.shadows;
      result.noShadows += bodySS.noShadows;
    }

    /* Fake some scattering from the stars texture using the sky's average color. */
    NightSkyRenderSettings nightSky = _ExpanseNightSky[0];
    SSResult nightSkySS = computeSSForMS(O, d, dist, t, groundHit, d, settings);
    result.shadows += nightSkySS.shadows * nightSky.tint * nightSky.scatterTint * nightScatterMultiplier;
    result.noShadows += nightSkySS.noShadows * nightSky.tint * nightSky.scatterTint * nightScatterMultiplier;

    /* Integrate the light pollution. */
    SSResult lightPollutionSS = computeSSLP(O, d, dist, t, groundHit,
      useOcclusionMultiplier, nightSky.lightPollution, settings);
    result.shadows += lightPollutionSS.shadows;
    result.noShadows += lightPollutionSS.noShadows;

    return result;
  }









/******************************************************************************/
/**************************** MULTIPLE SCATTERING *****************************/
/******************************************************************************/

  static MSLayersResult computeMSLayers(float3 O, float3 d, float dist, float t,
    bool groundHit, float3 L, AtmosphereSettings settings) {
    
    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];
    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    /* Loop counters. */
    int i, j;

    /* Final result. */
    MSLayersResult result;
    [unroll(MAX_ATMOSPHERE_LAYERS)]
    for (i = 0; i < MAX_ATMOSPHERE_LAYERS; i++) {
      result.scattering[i] = float3(0, 0, 0);
    }

    for (i = 0; i < quality.samplesMSAcc; i++) {
      /* Generate the sample. */
      float2 t_ds;
      if (quality.importanceSample) {
        t_ds = Utilities::generateCubicSampleFromIndex(i, quality.samplesMSAcc);
      } else {
        t_ds = Utilities::generateLinearSampleFromIndex(i, quality.samplesMSAcc);
      }
      float sampleT = t_ds.x * dist;
      float ds = t_ds.y;
      float3 samplePoint = O + d * sampleT;
      float3 normalizedSamplePoint = normalize(samplePoint);

      /* Sample multiple scattering table. */
      float2 msUV = AtmosphereMapping::mapMSCoordinate(length(samplePoint),
        dot(normalizedSamplePoint, L), planet.atmosphereRadius, planet.radius);
      float3 msContrib = sampleMultipleScattering(msUV);

      /* Compute the scaled density of the layer at the sample point. */
      [unroll(MAX_ATMOSPHERE_LAYERS)]
      for (j = 0; j < _ExpanseNumAtmosphereLayers; j++) {
        float scaledDensity = ds * computeDensity(samplePoint, planet.radius,
          _ExpanseAtmosphereLayers[j].densityDistribution, _ExpanseAtmosphereLayers[j].height, 
          _ExpanseAtmosphereLayers[j].thickness, _ExpanseAtmosphereLayers[j].density);
        // HACK: max().
        result.scattering[j] += max(0, msContrib) * scaledDensity;
      }
    }

    return result;
  }

  /* Computes single scattering for a single celestial body, across all
   * layers. */
  static float3 computeMSBody(float3 O, float3 d, float dist, float t,
    bool groundHit, float3 L, float3 lightColor, AtmosphereSettings settings) {

    MSLayersResult layersResult = Atmosphere::computeMSLayers(O, d, dist, t, groundHit,
      L, settings);

    float3 result = 0;
    for (int i = 0; i < _ExpanseNumAtmosphereLayers; i++) {
      AtmosphereLayerRenderSettings layer = _ExpanseAtmosphereLayers[i];
      result += layer.scatteringCoefficients * (2.0 * layer.tint)
        * layer.multipleScatteringMultiplier * layersResult.scattering[i];
    }

    return result * lightColor * dist;
  }

  static float3 computeMS(float3 O, float3 d, float dist, float t,
    bool groundHit, float nightScatterMultiplier, AtmosphereSettings settings) {
    float3 result = 0;
    for (int i = 0; i < _ExpanseNumAtmosphereDirectionalLights; i++) {
      DirectionalLightRenderSettings light = _ExpanseAtmosphereDirectionalLights[i];
      result += computeMSBody(O, d, dist, t, groundHit, light.direction,
        light.lightColor, settings);
    }

    /* Fake some scattering from the stars texture using the sky's average color. */
    NightSkyRenderSettings nightSky = _ExpanseNightSky[0];
    result += computeMSBody(O, d, dist, t, groundHit, d,
      nightSky.tint * nightSky.scatterTint * nightScatterMultiplier, settings);

    return result;
  }



/******************************************************************************/
/************************** SCREENSPACE SCATTERING ****************************/
/******************************************************************************/


  /**
   * @brief: Computes density at a point for a uniformly distributed atmosphere
   * that attenuates away from the origin.
   * */
  static float computeDensityUniform(float3 p, float3 o, float attenuationDistance, 
    float density) {
    return density * saturate(exp(-length(p - o) / attenuationDistance));
  }

  /**
   * @brief: Computes density at a point for a exponentially distributed atmosphere
   * that attenuates away from the origin.
   * */
  static float computeDensityExponentialAttenuated(float3 p, float3 o, float thickness, 
    float attenuationDistance, float density, float planetRadius) {
    return computeDensityExponential(p, thickness, density, planetRadius) 
      * saturate(exp(-length(p - o) / attenuationDistance));
  }

  /**
   * @brief: Computes density for screenspace distributions---aka distributions that can't
   * be stored in the general transmittance LUT, because they vary across the planet surface.
   * */
  static float computeDensityScreenspace(float3 p, float3 o, int densityDistribution, 
    float thickness, float attenuationDistance, float density, float planetRadius) {
    switch (densityDistribution) {
      case DENSITYDISTRIBUTION_SCREENSPACE_UNIFORM:
        return computeDensityUniform(p, o, attenuationDistance, density);
      case DENSITYDISTRIBUTION_SCREENSPACE_HEIGHT_FOG:
        return computeDensityExponentialAttenuated(p, o, thickness, attenuationDistance, density, planetRadius);
      default:
        return 0;
    }
  }

  /**
   * This function is an analytical approximation that does not take into account the curvature of the Earth. 
   * The solution that does take into account curvature does not have an efficient analytical solution.
   * */
  static float3 screenspaceLayerTransmittance(AtmosphereSettings settings, 
    AtmosphereLayerRenderSettings layer, float3 start, float3 d, float t, float3 o) {
    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];

    switch (layer.densityDistribution) {
      case DENSITYDISTRIBUTION_SCREENSPACE_UNIFORM: {
        t = min(t, layer.height);
        return saturate(exp(-layer.density * layer.extinctionCoefficients * t));
      }
      case DENSITYDISTRIBUTION_SCREENSPACE_HEIGHT_FOG: {
        t = min(t, layer.height);
        float3 start = o + float3(0, planet.originOffset.y - planet.radius, 0);
        float3 dest = start + t * d;
        float deltaH = (dest.y - start.y) / t;
        float opticalDepth = max(0, (deltaH < 1e-9 && deltaH > -1e-9) ? (t * exp(-dest.y / layer.thickness)) : ((layer.thickness / deltaH) * (exp(-(start.y / layer.thickness)) - exp(-(dest.y / layer.thickness)))));
        return saturate(exp(-layer.density * layer.extinctionCoefficients * opticalDepth));
      }
      default:
        return 1;
    }
  }

  static float estimateOcclusionDepthBuffer(float3 o, float2 uv, float3 worldspaceL,
    int samples, float blueNoiseOffset, int cloudMipLevel, float maxGeometryOcclusion,
    float maxCloudOcclusion, bool geometryShadows, bool cloudShadows, bool useCloudArray, bool justClouds) {
    /* Compute the pixel coordinate and light vector in NDC. */
    float2 P = uv * 2 - 1;
    float4 L4 = mul(UNITY_MATRIX_VP, float4(worldspaceL, 0));
    
    /* We can't properly compute the volumetric shadows if we are facing
     * away from the light. */
    if (L4.w < 0) {
      return 1;
    }

    float3 L = L4.xyz / L4.w;
    L.y = -L.y;
    float2 dTexCoord = L.xy - P;
    float outOfBoundsBlend = saturate(pow(1-L4.w, 2));

    bool lightOutOfBounds = !(Utilities::boundsCheck(L.x, float2(-1, 1)) 
      && Utilities::boundsCheck(L.y, float2(-1, 1)));
    if (lightOutOfBounds) {
      /* We need to adjust dTexCoord to avoid sampling outside the screenspace boundary. */
      float2 normalizedDTexCoord = normalize(dTexCoord);
      float2 clampedNormalizedDTexCoord = float2(Utilities::clampNonZero(normalizedDTexCoord.x), Utilities::clampNonZero(normalizedDTexCoord.y));
      float tX = Utilities::minNonNegative((-1 - P.x) / clampedNormalizedDTexCoord.x, (1 - P.x) / clampedNormalizedDTexCoord.x);
      float tY = Utilities::minNonNegative((-1 - P.y) / clampedNormalizedDTexCoord.y, (1 - P.y) / clampedNormalizedDTexCoord.y);
      float t = Utilities::minNonNegative(tX, tY);
      dTexCoord = normalizedDTexCoord * t;
    }

    /* March from the pixel coordinate to the view frustum intersection. */
    float occlusion = 0;
    for (int i = 0; i < samples; i++) {
      /* Generate the linearly spaced sample point. */
      float sampleT = saturate((i + blueNoiseOffset) / float(samples));

      /* Convert it to a UV coordinate. */
      float2 sampleUV = P + dTexCoord * sampleT;
      sampleUV = (sampleUV + 1) / 2;

      /* Sample the depth buffer(s) at the UV. */
      float stencil = 0;
      if (!justClouds) {
        stencil = geometryShadows ? SAMPLE_TEXTURE2D_LOD(_DownsampledDepthTex, s_trilinear_clamp_sampler, sampleUV, 0).x : 0;
      }
      float sampleCloudTransmittance = cloudShadows ? (useCloudArray ? Utilities::average(SAMPLE_TEXTURE2D_ARRAY_LOD(_CloudTransmittanceArray, s_linear_clamp_sampler, sampleUV, 0, cloudMipLevel).xyz) : Utilities::average(SAMPLE_TEXTURE2D_LOD(_CloudTransmittance, s_linear_clamp_sampler, sampleUV, cloudMipLevel).xyz)) : 1;

      occlusion += max(min(1 - sampleCloudTransmittance, maxCloudOcclusion), min(stencil, maxGeometryOcclusion));
    }

    occlusion = 1 - (occlusion / float(samples));
    return lerp(occlusion, 1, outOfBoundsBlend);
  }

  static float estimateOcclusionPointLightDepthBuffer(float3 oWS, float2 uv, float3 lightPosWS,
    int samples, float blueNoiseOffset, int cloudMipLevel, float maxGeometryOcclusion) {
    /* Compute the pixel coordinate and light vector in NDC. */
    float3 worldspaceL = normalize(lightPosWS - oWS);
    float distToLight = length(lightPosWS - oWS);
    float2 P = uv * 2 - 1;
    float4 L4 = mul(UNITY_MATRIX_VP, float4(worldspaceL, 0));
    
    /* We can't properly compute the volumetric shadows if we are facing
     * away from the light. */
    if (L4.w < 0) {
      return 1;
    }

    float3 L = L4.xyz / L4.w;
    L.y = -L.y;
    float2 dTexCoord = L.xy - P;
    float outOfBoundsBlend = saturate(pow(1-L4.w, 2));

    bool lightOutOfBounds = !(Utilities::boundsCheck(L.x, float2(-1, 1)) 
      && Utilities::boundsCheck(L.y, float2(-1, 1)));
    if (lightOutOfBounds) {
      /* We need to adjust dTexCoord to avoid sampling outside the screenspace boundary. */
      float2 normalizedDTexCoord = normalize(dTexCoord);
      float2 clampedNormalizedDTexCoord = float2(Utilities::clampNonZero(normalizedDTexCoord.x), Utilities::clampNonZero(normalizedDTexCoord.y));
      float tX = Utilities::minNonNegative((-1 - P.x) / clampedNormalizedDTexCoord.x, (1 - P.x) / clampedNormalizedDTexCoord.x);
      float tY = Utilities::minNonNegative((-1 - P.y) / clampedNormalizedDTexCoord.y, (1 - P.y) / clampedNormalizedDTexCoord.y);
      float t = Utilities::minNonNegative(tX, tY);
      dTexCoord = normalizedDTexCoord * t;
    }

    /* March from the pixel coordinate to the view frustum intersection. */
    float occlusion = 0;
    for (int i = 0; i < samples; i++) {
      /* Generate the linearly spaced sample point. */
      float sampleT = saturate((i + blueNoiseOffset) / float(samples));

      /* Convert it to a UV coordinate. */
      float2 sampleUV = P + dTexCoord * sampleT;
      sampleUV = (sampleUV + 1) / 2;

      /* Sample the depth buffer at the UV. */
      float depth = Linear01Depth(SampleCameraDepth(sampleUV), _ZBufferParams).x;
      float stencil = 1-saturate(depth * _ProjectionParams.z / distToLight);

      occlusion += min(stencil, maxGeometryOcclusion);
    }

    occlusion = 1 - (occlusion / float(samples));
    return lerp(occlusion, 1, outOfBoundsBlend);
  }

  static float estimateOcclusionPointLightShadowmap(float3 d, float tStart, float tEnd, float3 lightPosCameraSpace, float2 uv, float maxOcclusion, float maxDistance, int samples, float blueNoise, int shadowIndex, HDShadowContext shadowContext) {
    /* March from the camera origin to the intersection point. */
    LightData light = _LightDatas[shadowIndex];
    float occlusion = 0;
    float t = min((tEnd - tStart), maxDistance);
    float summedAttenuation = 0;
    for (int i = 0; i < samples; i++) {
      /* Generate the linearly spaced sample point. */
      float3 samplePoint = d * (tStart + saturate((i + blueNoise) / float(samples)) * t);

      /* Sample the shadow map and weight it by the light attenuation. */
      float shadow = GetPunctualShadowAttenuation(shadowContext, 
        uv * _ScreenParams.xy, samplePoint, 0, light.shadowIndex, 
        normalize(light.positionRWS - samplePoint), length(light.positionRWS - samplePoint), 
        light.lightType == GPULIGHTTYPE_POINT, light.lightType != GPULIGHTTYPE_PROJECTOR_BOX);
      float attenuation = rcp(dot(samplePoint - light.positionRWS, samplePoint - light.positionRWS) + 1);
      occlusion += attenuation * max(saturate(shadow), 1 - maxOcclusion);
      summedAttenuation += attenuation;
    }
    return occlusion / (summedAttenuation);
  }

  static float estimateOcclusionShadowmap(float3 d, float t, float2 uv, float maxOcclusion, float maxDistance, int samples, float blueNoise, HDShadowContext shadowContext) {
    /* March from the camera origin to the intersection point. */
    DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];
    float occlusion = 0;
    // HACK: because we now use froxel fog, we can't compute occlusion all the way out
    // to our supposed hit point---otherwise we'll end up with bleedthrough from interpolation
    // to the next piece of geometry. For now, if we only go 0.75 of the distance to the hit point,
    // this resolves the bleedthrough, at the cost of objects casting slightly less aggressive
    // shadows.
    t = min(t * 0.75, maxDistance);
    for (int i = 0; i < samples; i++) {
      /* Generate the linearly spaced sample point. */
      float3 samplePoint = saturate((i + blueNoise) / float(samples)) * t * d;

      /* Sample the shadow map. */
      float shadow = GetDirectionalShadowAttenuation(shadowContext, uv * _ScreenParams.xy, samplePoint, 0, light.shadowIndex, -light.forward);
      occlusion += max(saturate(shadow), 1 - maxOcclusion);
    }

    return occlusion / float(samples);
  }

  /* Assumes that the entire volume is traversed through. */
  static float3 ComputeScreenspaceTransmittance(float3 o, float3 d, float t, 
    bool ignoreIfNonPhysical,
    AtmosphereSettings settings) {
    float3 transmittance = 1;
    for (int i = 0; i < _ExpanseNumFogLayers; i++) {
      AtmosphereLayerRenderSettings layer = _ExpanseFogLayers[i];
      if (!ignoreIfNonPhysical || layer.physicalLighting) {
        transmittance *= screenspaceLayerTransmittance(settings, layer, o, d, t, o);
      }
    }
    return transmittance;
  }

  static float computeOcclusionEstimate(AtmosphereSettings settings, 
    AtmosphereLayerRenderSettings layer, DirectionalLightRenderSettings light, 
    float3 o, float3 d, float t, float2 uv, float blueNoise, bool cubemap, 
    HDShadowContext shadowContext) {
    
    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    float occlusionEstimate = 1;
    if (layer.screenspaceShadows && !cubemap) {
      /* Get an estimate of how much geometry is between the pixel we're
        * shading and the light. */
      if (light.useShadowmap) {
        if (layer.geometryShadows && light.volumetricGeometryShadows) {
          occlusionEstimate = estimateOcclusionShadowmap(d, t, uv, layer.maxGeometryOcclusion, 
            light.maxShadowmapDistance, quality.samplesScreenspace, blueNoise, shadowContext);
        }
        if (layer.cloudShadows && light.volumetricCloudShadows) {
          // HACK: this looks bad when both volumetric geometry shadows
          // are enabled and non-volumetric cloud shadows are enabled.
          occlusionEstimate = min(occlusionEstimate, estimateOcclusionDepthBuffer(o, uv, light.direction, 
            quality.samplesScreenspace, blueNoise, quality.downsampledDepthMip,
            layer.maxGeometryOcclusion, layer.maxCloudOcclusion, layer.geometryShadows, layer.cloudShadows, layer.useCloudArray, true));
        }
      } else {
        occlusionEstimate = estimateOcclusionDepthBuffer(o, uv, light.direction, 
          quality.samplesScreenspace, blueNoise, quality.downsampledDepthMip,
          layer.maxGeometryOcclusion, layer.maxCloudOcclusion, 
          layer.geometryShadows && light.volumetricGeometryShadows, 
          layer.cloudShadows && light.volumetricCloudShadows, layer.useCloudArray, false);
      }
    }
    return occlusionEstimate;
  }

  static void computeScreenspaceScatteringApproximate(AtmosphereSettings settings,
    AtmosphereLayerRenderSettings layer,
    float3 o, float3 d, float t, float2 uv, float3 ambient, 
    float blueNoise, bool cubemap, HDShadowContext shadowContext,
    float3 transmittance, out float3 scattering) {

    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];

    /* Initialize to null result. */
    scattering = 0;

    float r = length(o);
    float ssDistance = min(t, layer.height);
    float3 clampedExtinction = Utilities::clampAboveZero(layer.extinctionCoefficients);

    /* HACK: compute a self-shadow approximation for what, at this point, is a very ill-defined 
      * sort of volume. This is the same in all directions, but mostly just attenuates the fog to 
      * a point where it looks more physically plausible. */
    float3 selfShadow = (layer.densityDistribution == DENSITYDISTRIBUTION_SCREENSPACE_UNIFORM) ?
      exp(-layer.density * layer.extinctionCoefficients * layer.height/5) :
      exp(-layer.density * layer.extinctionCoefficients * layer.thickness/5);

    /* Ambient contribution. */
    // HACK: sample upper half of reflection framebuffer. 256x256 resolution, so mip level
    // want is mip level 7 (2x2). However, limit the attenuation to a hardcoded value of 0.67.
    float ambientCloudAttenuation = max(0.33, 0.33 + 0.67 * SAMPLE_TEXTURE2D_LOD(_EXPANSE_CLOUD_REFLECTION, s_linear_clamp_sampler, float2(0.5 , 0.25), 7).w);
    scattering += ambientCloudAttenuation * selfShadow * layer.multipleScatteringMultiplier * (layer.scatteringCoefficients / clampedExtinction) * (1 - transmittance) * ambient;

    /* Single scattering requires a loop over all the lights. */
    for (int i = 0; i < _ExpanseNumAtmosphereDirectionalLights; i++) {
      DirectionalLightRenderSettings light = _ExpanseAtmosphereDirectionalLights[i];
      float3 L = light.direction;
      float mu_L = dot(normalize(o), L);

      /* Global attenuation of this light by cloud cover. */
      float3 cloudAttenuation = LOAD_TEXTURE2D_LOD(_CloudLightAttenuation, uint2(i, 0), 0).xyz;

      /* Our raw single scattering value that we'll attenuate through successive approximations. */
      float3 ssScattering = (layer.scatteringCoefficients / clampedExtinction) * (1 - transmittance) * light.lightColor;
      
      /* Phase function. */
      float phase = Atmosphere::computePhase(dot(L, d), layer.anisotropy, layer.phaseFunction);
      
      /* Transmittance through non-screenspace layers. */
      SkyIntersectionData lightIntersection = AtmosphereGeometry::traceSkyVolume(o, L, 
        planet.radius, planet.atmosphereRadius);
      float lightT = lightIntersection.endT - lightIntersection.startT;
      float2 transmittanceCoord = AtmosphereMapping::mapTransmittanceCoord(r, mu_L,
        planet.atmosphereRadius, planet.radius, lightT, 
        lightIntersection.groundHit, settings.resT.y);
      float3 lightTransmittance = sampleTransmittance(transmittanceCoord) * (lightIntersection.groundHit ? 0 : 1);
      
      /* Occlusion by clouds and geometry. */
      float occlusionEstimate = computeOcclusionEstimate(settings, layer, light, o, d, t, uv, 
        blueNoise, cubemap, shadowContext);

      /* Shadow blur to smooth the transition transition between day and night. */
      const float shadowBlurDist = 0.01;
      float shadowBlur = computeShadowBlur(r, mu_L, shadowBlurDist, planet.radius);

      ssScattering *= selfShadow * cloudAttenuation * phase * lightTransmittance * shadowBlur * (2 * layer.tint) * occlusionEstimate;
      scattering += ssScattering;
    }
  }

  static void computeScreenspaceScatteringPhysical(AtmosphereSettings settings,
    AtmosphereLayerRenderSettings layer,
    float3 o, float3 d, float t, float2 uv, float3 ambient, 
    float blueNoise, bool cubemap, HDShadowContext shadowContext,
    float3 transmittance, out float3 scattering) {
    int i = 0;

    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];
    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    /* Initialize to null result. */
    scattering = 0;

    float r = length(o);

    /* Ambient contribution. */
    // HACK: sample upper half of reflection framebuffer. 256x256 resolution, so mip level
    // want is mip level 7 (2x2). However, limit the attenuation to a hardcoded value of 0.33.
    float ambientCloudAttenuation = max(0.33, 0.33 + 0.67 * SAMPLE_TEXTURE2D_LOD(_EXPANSE_CLOUD_REFLECTION, s_linear_clamp_sampler, float2(0.5 , 0.25), 7).w);
    float3 clampedExtinction = Utilities::clampAboveZero(layer.extinctionCoefficients);
    scattering += ambientCloudAttenuation * layer.multipleScatteringMultiplier * (layer.scatteringCoefficients / clampedExtinction) * (1 - transmittance) * ambient;

    /* Single scattering requires a loop over all the lights. */
    for (i = 0; i < _ExpanseNumAtmosphereDirectionalLights; i++) {
      DirectionalLightRenderSettings light = _ExpanseAtmosphereDirectionalLights[i];
      float3 L = light.direction;
      float mu_L = dot(normalize(o), L);

      /* Global attenuation of this light by cloud cover. */
      float3 cloudAttenuation = LOAD_TEXTURE2D_LOD(_CloudLightAttenuation, uint2(i, 0), 0).xyz;

      /* Phase function. */
      float phase = Atmosphere::computePhase(dot(L, d), layer.anisotropy, layer.phaseFunction);

      /* Now, raymarch the single scattering. */
      float3 ssScattering = 0;
      float ds = rcp((float) quality.samplesScreenspaceScattering);
      for (int j = 0; j < quality.samplesScreenspaceScattering; j++) {
        /* Generate the sample. */
        float sampleT = 0;
        if (quality.screenspaceImportanceSample) {
          float2 t_ds = Utilities::generateCubicSampleFromIndex(j, quality.samplesScreenspaceScattering);
          ds = t_ds.y;
          sampleT = t * t_ds.x;
        } else {
          sampleT = t * ((j + 0.5) / (float) quality.samplesScreenspaceScattering);
        }
        float3 p = o + sampleT * d;
        
        /* Check shadowing. */
        SkyIntersectionData lightIntersection = AtmosphereGeometry::traceSkyVolume(p,
          L, planet.radius, planet.atmosphereRadius);
        if (lightIntersection.groundHit) {
          continue;
        }

        float3 shadowFactor = 1;
        const float shadowBlurDist = 0.01;
        shadowFactor *= computeShadowBlur(length(p), dot(normalize(p), L), shadowBlurDist, planet.radius);

        /* Transmittance through non-screenspace layers. */
        float lightT = lightIntersection.endT - lightIntersection.startT;
        float2 transmittanceCoord = AtmosphereMapping::mapTransmittanceCoord(length(p), 
          dot(normalize(p), L), planet.atmosphereRadius, planet.radius, lightT, 
          lightIntersection.groundHit, settings.resT.y);
        shadowFactor *= sampleTransmittance(transmittanceCoord);

        /* Transmittance through screenspace layer to light. */
        shadowFactor *= screenspaceLayerTransmittance(settings, layer, p, L, lightT, o);
    
        /* Transmittance through screenspace layers to sample point. */
        float3 transmittanceToSample = screenspaceLayerTransmittance(settings, layer, o, d, sampleT, o);

        float density = computeDensityScreenspace(p, o, layer.densityDistribution, 
          layer.thickness, layer.height, layer.density, planet.radius);
        ssScattering += transmittanceToSample * shadowFactor * density * layer.scatteringCoefficients * ds;
      }

      /* Occlusion by clouds and geometry. */
      float occlusionEstimate = computeOcclusionEstimate(settings, layer, light, o, d, t, uv, 
        blueNoise, cubemap, shadowContext);

      scattering += cloudAttenuation * phase * occlusionEstimate * light.lightColor * (2 * layer.tint) * ssScattering * t;
    }
  }

  static void integrateLightVolume(float3 p, float3 d, float t, PointLightRenderSettings light, float cullRadius, 
    out float4 intersection, out float integratedAttenuation) {
    
    // Initialize to null result.
    intersection = -1;
    integratedAttenuation = 0;
    const int kRaymarchSamples = 64;

    // Intersect the light volume.
    switch (light.geometryType) {
      case POINTLIGHTGEOMETRYTYPE_POINT: {
          PointLightVolume v = PointLightVolume::CreatePointLightVolume(light.position, light.range, cullRadius);
          if (light.raymarch) {
            v.raymarch(p, d, t, kRaymarchSamples, intersection, integratedAttenuation);
          } else {
            v.integrate(p, d, t, intersection, integratedAttenuation);
          }
          return;
      }
      case POINTLIGHTGEOMETRYTYPE_CONE: {
          ConeLightVolume v = ConeLightVolume::CreateConeLightVolume(light.position, light.geometryParam2, light.rotation, 
              light.inverseRotation, light.range, cullRadius);
          if (light.raymarch) {
            v.raymarch(p, d, t, kRaymarchSamples, intersection, integratedAttenuation);
          } else {
            v.integrate(p, d, t, intersection, integratedAttenuation);
          }          
          return;
      }
      case POINTLIGHTGEOMETRYTYPE_BOX: {
          BoxLightVolume v = BoxLightVolume::CreateBoxLightVolume(light.position, light.geometryParam1, light.geometryParam2,
              light.rotation, light.inverseRotation, light.range, cullRadius);
          if (light.raymarch) {
            v.raymarch(p, d, t, kRaymarchSamples, intersection, integratedAttenuation);
          } else {
            v.integrate(p, d, t, intersection, integratedAttenuation);
          }          
          return;
      }
      case POINTLIGHTGEOMETRYTYPE_PYRAMID: {
          PyramidLightVolume v = PyramidLightVolume::CreatePyramidLightVolume(light.position, light.geometryParam1, light.geometryParam2,
              light.rotation, light.inverseRotation, light.range, cullRadius);
          if (light.raymarch) {
            v.raymarch(p, d, t, kRaymarchSamples, intersection, integratedAttenuation);
          } else {
            v.integrate(p, d, t, intersection, integratedAttenuation);
          }          
          return;
      }
      default: {
          // Undefined behavior, return null result.
          intersection = -1;
          integratedAttenuation = 0;
          return;
      }
    }
  }

  static float3 computeScreenspaceScatteringPointLights(AtmosphereSettings settings,
    AtmosphereLayerRenderSettings layer,
    float3 o, float3 oWS, float3 d, float t, float2 uv, float3 ambient, 
    float blueNoise, bool cubemap, HDShadowContext shadowContext) {
    int i = 0;
    
    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];
    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    /* Initialize to null result. */
    float3 scattering = 0;
    
    /* Loop over all the point lights. */
    for (i = 0; i < _ExpanseNumFogPointLights; i++) {
      PointLightRenderSettings light = _ExpanseFogPointLights[i];

      float3 oToLightWS = light.position - oWS;
      float3 lightToOWS = oWS - light.position;
      float3 normalizedLightToOWS = normalize(lightToOWS);
      float oToLightWSLengthSq = dot(light.position - oWS, light.position - oWS);

      // Cull lights that are 500x their point light radius away from the camera.
      const float kDistanceCull = 500;
      if (oToLightWSLengthSq > kDistanceCull * kDistanceCull) {
        continue;
      }

      // Only render pixels that intersect with the sphere of radius 5 * lightRadius.
      const float kRadiusCull = 5;
      float4 lightIntersection = -1;
      float integratedAttenuation = 0;
      integrateLightVolume(oWS, d, t, light, kRadiusCull, lightIntersection, integratedAttenuation);

      if (lightIntersection.x < 0) {
        continue;
      }
      
      // Sample layer transmittance to light. Cull if it's too low.
      float oToLightWSLength = Utilities::safeSqrt(oToLightWSLengthSq);
      float3 transmittanceToSample = screenspaceLayerTransmittance(settings, layer, o, oToLightWS / oToLightWSLength, oToLightWSLength, o);
      if (Utilities::average(transmittanceToSample) < 0.001) {
        continue;
      }

      // Assume density is constant over the span of the point light.
      float3 lightPosPlanetSpace = Mapping::transformPointToPlanetSpace(light.position, planet.originOffset, planet.radius);
      float density = computeDensityScreenspace(lightPosPlanetSpace, o, layer.densityDistribution, 
        layer.thickness, layer.height, layer.density, planet.radius);

      // Fade out with distance.
      float distanceFade = 1 - saturate((oToLightWSLength - (0.5 * kDistanceCull)) / (0.5 * kDistanceCull));

      float occlusionEstimate = 1;
      if (layer.geometryShadows && light.volumetricGeometryShadows) {
        if (light.useShadowmap) {
          occlusionEstimate = estimateOcclusionPointLightShadowmap(d, lightIntersection.x, lightIntersection.y, 
            light.position - oWS, uv, layer.maxGeometryOcclusion, light.maxShadowmapDistance, 
            quality.samplesScreenspace, blueNoise, light.shadowIndex, shadowContext);
        } else {
          occlusionEstimate = estimateOcclusionPointLightDepthBuffer(oWS, uv, light.position, 
            quality.samplesScreenspace, blueNoise, quality.downsampledDepthMip, layer.maxGeometryOcclusion);
        }
      }

      scattering += occlusionEstimate * light.multiplier * distanceFade * transmittanceToSample * integratedAttenuation * light.lightColor * density * (2 * layer.tint) * layer.scatteringCoefficients;
    }

    return scattering;
  }

  static void ComputeScreenspaceScattering(float3 o, float3 oWS, float3 d,
    float2 uv, float depth, bool cubemap, float3 ambient, bool overridePhysical,
    AtmosphereSettings settings,
    out float3 scattering, out float3 transmittance) {

    /* Initialize to null result. */
    transmittance = 1;
    scattering = 0;

    /* Pre-compute this pixel's noise offset. */
    float blueNoise = Random::random_4_1(float4(d, (uint(settings.frameCount) % 256) / 256.0));
    
    /* For shadowmap shadows. */
    HDShadowContext shadowContext = InitShadowContext();

    /* Loop over all the screenspace atmosphere layers. */
    for (int i = 0; i < _ExpanseNumFogLayers; i++) {
      AtmosphereLayerRenderSettings layer = _ExpanseFogLayers[i];
      
      /* Transmittance is easy to evaluate. */
      float3 layerTransmittance = screenspaceLayerTransmittance(settings, layer, o, d, depth, o);
      transmittance *= layerTransmittance;

      // Directional lights.
      float3 layerScattering = 0;
      if (layer.physicalLighting && !overridePhysical) {
        computeScreenspaceScatteringPhysical(settings, layer, 
          o, d, depth, uv, ambient, blueNoise, cubemap, shadowContext, 
          layerTransmittance, layerScattering);
      } else {
        computeScreenspaceScatteringApproximate(settings, layer, 
          o, d, depth, uv, ambient, blueNoise, cubemap, shadowContext, 
          layerTransmittance, layerScattering);
      }
      scattering += layerScattering;
    }

    /* Do point lights in a separate loop. */
    if (!cubemap) {
      for (int i = 0; i < _ExpanseNumFogLayers; i++) {
        AtmosphereLayerRenderSettings layer = _ExpanseFogLayers[i];
        // Point lights.
        scattering += computeScreenspaceScatteringPointLights(settings, layer, 
          o, oWS, d, depth, uv, ambient, blueNoise, cubemap, shadowContext);
      }
    }
  }

};

#endif // EXPANSE_ATMOSPHERE_INCLUDED
