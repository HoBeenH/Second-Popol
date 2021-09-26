Shader "Hidden/HDRP/Sky/Composite Sky" {
  HLSLINCLUDE
  
  #pragma vertex Vert

  #pragma editor_sync_compilation
  #pragma target 4.5
  #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

  #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
  #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
  #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

  /* Expanse globals */
  #include "../../directLight/planet/PlanetGlobals.hlsl"
  #include "../../main/QualityGlobals.hlsl"

  #include "../../common/Mapping.hlsl"
  #include "../../common/Utilities.hlsl"
  #include "../../common/Random.hlsl"
  #include "../../atmosphere/AtmosphereMapping.hlsl"
  #include "../../atmosphere/AtmosphereGeometry.hlsl"
  #include "../../atmosphere/Atmosphere.hlsl"
  #include "../../atmosphere/AtmosphereGlobalBuffers.hlsl"
  #include "../../clouds/Clouds.hlsl"
  #include "../SkyCompositor.cs.hlsl"
  #include "../SkyCompositorGlobalTextures.hlsl"

  /* Parameters. */
  float4 _WorldSpaceCameraPos1;
  float4x4 _InversePixelCoordToViewDirWS;
  float4 _resT;
  float4 _resSkyView;
  int _useCloudTextureArray;

  /* Textures to composite. */
  TEXTURE2D(_directLight);
  TEXTURE2D(_atmosphereSkyView);
  TEXTURE3D(_atmosphereAerialPerspective);
  TEXTURE2D(_cloudLighting);
  TEXTURE2D(_cloudTransmittanceAndHit);
  TEXTURE2D(_cloudLightAttenuation);
  TEXTURE2D_ARRAY(_cloudLightingArray);
  TEXTURE2D_ARRAY(_cloudTransmittanceAndHitArray);
  TEXTURE2D_ARRAY(_cloudGBufferArray);
  TEXTURE2D(_cloudReflection);
  TEXTURE2D(_cloudReflectionT);
  TEXTURE3D(_screenspaceVolumetrics);
  TEXTURE2D(_fullscreenNoGeometry);

  struct Attributes {
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  struct Varyings {
    float4 positionCS : SV_POSITION;
    float2 screenPosition : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
  };

  struct ColorAndDepth {
    float4 color : SV_Target;
    float depth : SV_DEPTH;
  };

  Varyings Vert(Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
    output.screenPosition = GetFullScreenTriangleTexCoord(input.vertexID);
    return output;
  }

  /* Wrapper for using the correct textures. */
  void sampleClouds(float2 uv, bool geoHit, float depth, out float4 lighting, out float4 tAndHit) {
    if (_useCloudTextureArray) {
      float intersection = Clouds::unmapGBuffer(SAMPLE_TEXTURE2D_ARRAY_LOD(_cloudGBufferArray, s_linear_clamp_sampler, uv, 0, 0).xy).x;
      // HACK: tolerance value.
      if (intersection > -0.01 && (!geoHit || intersection < depth)) {
        tAndHit = SAMPLE_TEXTURE2D_ARRAY_LOD(_cloudTransmittanceAndHitArray, s_linear_clamp_sampler, uv, 0, 0);
        // TODO: how do we set up this condition to work with subresolution?
        if (!geoHit || tAndHit.w < depth) {
          lighting = SAMPLE_TEXTURE2D_ARRAY_LOD(_cloudLightingArray, s_linear_clamp_sampler, uv, 0, 0);
        } else {
          lighting = 0;
          tAndHit = float4(1, 1, 1, -1);
        }
      } else {
        lighting = 0;
        tAndHit = float4(1, 1, 1, -1);
      }
    } else {
      tAndHit = SAMPLE_TEXTURE2D_LOD(_cloudTransmittanceAndHit, s_linear_clamp_sampler, uv, 0);
      // TODO: how do we set up this condition to work with subresolution?
      if (tAndHit.w < depth || !geoHit) {
        lighting = SAMPLE_TEXTURE2D_LOD(_cloudLighting, s_linear_clamp_sampler, uv, 0);
      } else {
        lighting = 0;
        tAndHit = float4(1, 1, 1, -1);
      }
    }
  }

  /* Samples/composites textures to be composited over geometry. */
  float4 CompositeGeo(float2 uv, float linear01Depth, float depth, float farClip, float ditherMultiplier) {
    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    /* Aerial perspective. */
    float3 apUV = AtmosphereMapping::mapFrustumCoordinate(uv, linear01Depth, quality.AP_depthSkew);
    float4 aerialPerspective = SAMPLE_TEXTURE3D_LOD(_atmosphereAerialPerspective, s_linear_clamp_sampler, apUV, 0);

    /* Screenspace on geometry. */
    float3 geometryScreenspaceUV = AtmosphereMapping::mapFrustumCoordinate(uv, linear01Depth, quality.screenspace_depthSkew);
    float4 geometryScreenspace = SAMPLE_TEXTURE3D_LOD(_screenspaceVolumetrics, s_linear_clamp_sampler, geometryScreenspaceUV, 0);

    float4 cloudLighting = 0;
    float4 cloudTransmittanceAndHit = 1;
    sampleClouds(uv, true, depth, cloudLighting, cloudTransmittanceAndHit);

    if (quality.dither > 0) {
      aerialPerspective.xyz *= ditherMultiplier;
      cloudLighting *= ditherMultiplier;
      geometryScreenspace.xyz *= ditherMultiplier;
    } 

    float3 screenspaceOnAP = aerialPerspective.xyz * geometryScreenspace.w + geometryScreenspace.xyz;
    float3 clouds = cloudLighting.xyz;

    float3 cloudsOnSky = 0;
    // Only sample additional fog + AP for clouds if the clouds are visible.
    if (Utilities::average(cloudTransmittanceAndHit.xyz) < 1) {
      float3 cloudAPUV = AtmosphereMapping::mapFrustumCoordinate(uv, saturate(cloudTransmittanceAndHit.w / farClip), quality.AP_depthSkew);
      float4 cloudAerialPerspective = SAMPLE_TEXTURE3D_LOD(_atmosphereAerialPerspective, s_linear_clamp_sampler, cloudAPUV, 0);
      
      float3 cloudScreenspaceUV = AtmosphereMapping::mapFrustumCoordinate(uv, saturate(cloudTransmittanceAndHit.w / farClip), quality.screenspace_depthSkew);
      float4 cloudScreenspace = (cloudTransmittanceAndHit.w < 0) ? float4(0, 0, 0, 1) : SAMPLE_TEXTURE3D_LOD(_screenspaceVolumetrics, s_linear_clamp_sampler, cloudScreenspaceUV, 0);

      if (quality.dither > 0) {
        cloudAerialPerspective.xyz *= ditherMultiplier;
        cloudScreenspace.xyz *= ditherMultiplier;
      }

      float3 apOnClouds = clouds * exp(cloudAerialPerspective.w) + cloudAerialPerspective.xyz;
      float3 screenspaceOnClouds = apOnClouds * cloudScreenspace.w + cloudScreenspace.xyz;
      cloudsOnSky = screenspaceOnAP * cloudTransmittanceAndHit.xyz + screenspaceOnClouds * (1 - cloudTransmittanceAndHit.xyz);
    } else {
      cloudsOnSky = screenspaceOnAP * cloudTransmittanceAndHit.xyz + clouds * (1 - cloudTransmittanceAndHit.xyz);
    }

    // Transmittance only takes into account the first few terms, which fully
    // describe the transmittance to the geometry hit.
    float finalTransmittance = exp(aerialPerspective.w) * geometryScreenspace.w * Utilities::average(cloudTransmittanceAndHit.xyz);

    return float4(GetCurrentExposureMultiplier() * cloudsOnSky, finalTransmittance);
  }

  /* Samples/composites textures in the event that we're just looking right at the sky. */
  float4 CompositeNoGeo(float2 uv, float3 o, float3 d, float r, float mu, float t, 
    SkyIntersectionData intersection, float depth, float farClip, float ditherMultiplier) {
    
    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];
    QualityRenderSettings quality = _ExpanseQualitySettings[0];

    /* Direct light. Stored with exposure multiplier to improve precision distribution---expanding 
     * back to 32-bit gives no real visible precision loss, and ensures things like bloom work 
     * with physical sun luminance values (100000+ Lux). */
    float4 directLight = SAMPLE_TEXTURE2D_LOD(_directLight, s_linear_clamp_sampler, uv, 0) / GetCurrentExposureMultiplier();

    /* Atmosphere. */
    float theta = AtmosphereMapping::d_to_theta(d, o);
    float2 atmoUV = AtmosphereMapping::mapSkyRenderCoordinate(r, mu,
      theta, planet.atmosphereRadius, planet.radius, t,
      intersection.groundHit, _resSkyView.x, _resSkyView.y);
    float4 atmosphere = SAMPLE_TEXTURE2D_LOD(_atmosphereSkyView, s_linear_clamp_sampler, atmoUV, 0);

    /* Far away screenspace volumetrics. */
    float3 distantScreenspaceUV = AtmosphereMapping::mapFrustumCoordinate(uv, saturate(t/farClip), quality.AP_depthSkew);
    float4 distantScreenspace = SAMPLE_TEXTURE3D_LOD(_screenspaceVolumetrics, s_linear_clamp_sampler, distantScreenspaceUV, 0);

    /* Clouds. */
    float4 cloudLighting = 0;
    float4 cloudTransmittanceAndHit = 1;
    sampleClouds(uv, false, depth, cloudLighting, cloudTransmittanceAndHit);

    /* Aerial perspective on top of clouds. */
    float3 apUV = AtmosphereMapping::mapFrustumCoordinate(uv, saturate(cloudTransmittanceAndHit.w / farClip), quality.AP_depthSkew);
    float4 aerialPerspective = SAMPLE_TEXTURE3D_LOD(_atmosphereAerialPerspective, s_linear_clamp_sampler, apUV, 0);

    /* Screenspace fog on top of clouds. */
    float3 cloudScreenspaceUV = AtmosphereMapping::mapFrustumCoordinate(uv, saturate(cloudTransmittanceAndHit.w / farClip), quality.screenspace_depthSkew);
    float4 cloudScreenspace = SAMPLE_TEXTURE3D_LOD(_screenspaceVolumetrics, s_linear_clamp_sampler, cloudScreenspaceUV, 0);

    if (quality.dither > 0) {
      aerialPerspective.xyz *= ditherMultiplier;
      cloudLighting.xyz *= ditherMultiplier;
      atmosphere.xyz *= ditherMultiplier;
      distantScreenspace.xyz *= ditherMultiplier;
    }

    /* Sky is furthest away. */
    float3 sky = directLight.xyz + atmosphere.xyz;
    /* Next composite screenspace layer on top of sky. */
    float3 distantScreenspaceOnSky = sky.xyz * distantScreenspace.w + distantScreenspace.xyz;
    /* Composite clouds on top of that. */
    float3 cloudsOnSky = distantScreenspaceOnSky * cloudTransmittanceAndHit.xyz + cloudLighting.xyz * exp(aerialPerspective.w) * cloudScreenspace.w;
    /* Composite aerial perspective on top of the clouds. */
    float3 apOnClouds = cloudsOnSky + aerialPerspective.xyz * (1-cloudTransmittanceAndHit.xyz) * cloudScreenspace.w;
    /* Finally, composite screenspace fog on top of that. */
    float3 fogOnClouds = apOnClouds + cloudScreenspace.xyz * (1-cloudTransmittanceAndHit.xyz);
    return float4(GetCurrentExposureMultiplier() * fogOnClouds, 0);
  }

  float4 CompositeWithoutGeometry(Varyings input) : SV_Target {
    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];
    
    float2 uv = input.screenPosition;

    /* Compute some information about where we are. */
    float3 o = Mapping::transformPointToPlanetSpace(_WorldSpaceCameraPos1.xyz, planet.originOffset.xyz, planet.radius);
    float3 d = -GetSkyViewDirWS(input.positionCS.xy);
    float r = length(o);
    float mu = dot(normalize(o), d);
    SkyIntersectionData intersection = AtmosphereGeometry::traceSkyVolume(o, d,
      planet.radius, planet.atmosphereRadius);
    float t = intersection.endT - intersection.startT;

    /* Load depth buffer info. */
    float linear01Depth = Linear01Depth(LoadCameraDepth(input.positionCS.xy), _ZBufferParams);
    float linearDepth = linear01Depth * _ProjectionParams.z;
    /* Make sure depth is distance to view aligned plane. */
    float3 cameraCenterD = -GetSkyViewDirWS(float2(_ScreenParams.x/2, _ScreenParams.y/2));
    float cosTheta = dot(cameraCenterD, d);
    float depth = linearDepth / max(cosTheta, 0.00001);
    float farClip = _ProjectionParams.z / max(cosTheta, 0.00001);
    bool geoHit = depth < farClip - 0.001;

    /* Generate a random value to use for dithering. */
    float ditherMultiplier = 0.985 + 0.03 * Random::random_3_1(d);
    return CompositeNoGeo(uv, o, d, r, mu, t, intersection, depth, farClip, ditherMultiplier);
  }

  float4 CompositeFullscreen(Varyings input) : SV_Target {
    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];

    float2 uv = input.screenPosition;

    /* Compute some information about where we are. */
    float3 o = Mapping::transformPointToPlanetSpace(_WorldSpaceCameraPos1.xyz, planet.originOffset.xyz, planet.radius);
    float3 d = -GetSkyViewDirWS(input.positionCS.xy);
    float r = length(o);
    float mu = dot(normalize(o), d);
    SkyIntersectionData intersection = AtmosphereGeometry::traceSkyVolume(o, d,
      planet.radius, planet.atmosphereRadius);
    float t = intersection.endT - intersection.startT;

    /* Load depth buffer info. */
    float linear01Depth = Linear01Depth(LoadCameraDepth(input.positionCS.xy), _ZBufferParams);
    float linearDepth = linear01Depth * _ProjectionParams.z;
    /* Make sure depth is distance to view aligned plane. */
    float3 cameraCenterD = -GetSkyViewDirWS(float2(_ScreenParams.x/2, _ScreenParams.y/2));
    float cosTheta = dot(cameraCenterD, d);
    float depth = linearDepth / max(cosTheta, 0.00001);
    float farClip = _ProjectionParams.z / max(cosTheta, 0.00001);
    bool geoHit = depth < farClip - 0.001;

    /* Generate a random value to use for dithering. */
    float ditherMultiplier = 0.985 + 0.03 * Random::random_3_1(d);

    if (geoHit) {
      float4 geo = CompositeGeo(uv, linear01Depth, depth, farClip, ditherMultiplier);
      if (planet.clipFade == 1) {
        return geo;
      }

      /* Clip blend. */
      float4 noGeo = SAMPLE_TEXTURE2D_LOD(_fullscreenNoGeometry, s_linear_clamp_sampler, uv, 0);
      float clipPlaneAlpha = evaluateClipBlend(linear01Depth, uv);
      return lerp(geo, noGeo, clipPlaneAlpha);
    } else {
      return SAMPLE_TEXTURE2D_LOD(_fullscreenNoGeometry, s_linear_clamp_sampler, uv, 0);
    }
  }

  float4 CompositeCubemap(Varyings input) : SV_Target {
    PlanetRenderSettings planet = _ExpansePlanetRenderSettings[0];

    float2 uv = input.screenPosition;

    /* Sampling the sky view texture takes a bit of work. */
    float3 o = Mapping::transformPointToPlanetSpace(_WorldSpaceCameraPos1.xyz, planet.originOffset.xyz, planet.radius);
    float3 d = -GetSkyViewDirWS(input.positionCS.xy);
    float r = length(o);
    float mu = dot(normalize(o), d);
    SkyIntersectionData intersection = AtmosphereGeometry::traceSkyVolume(o, d,
      planet.radius, planet.atmosphereRadius);
    float t = intersection.endT - intersection.startT;
    float theta = AtmosphereMapping::d_to_theta(d, o);
    float2 atmoUV = AtmosphereMapping::mapSkyRenderCoordinate(r, mu,
      theta, planet.atmosphereRadius, planet.radius, t,
      intersection.groundHit, _resSkyView.x, _resSkyView.y);

    float3 atmosphere = SAMPLE_TEXTURE2D_LOD(_atmosphereSkyView, s_linear_clamp_sampler, atmoUV, 0).xyz;
    /* Make sure we take light attenuation into account. Take the 8-way min. */
    float3 claMin_01 = min(LOAD_TEXTURE2D_LOD(_cloudLightAttenuation, uint2(0, 0), 0).xyz, 
      LOAD_TEXTURE2D_LOD(_cloudLightAttenuation, uint2(1, 0), 0).xyz);
    float3 claMin_23 = min(LOAD_TEXTURE2D_LOD(_cloudLightAttenuation, uint2(2, 0), 0).xyz, 
      LOAD_TEXTURE2D_LOD(_cloudLightAttenuation, uint2(3, 0), 0).xyz);
    float3 claMin_45 = min(LOAD_TEXTURE2D_LOD(_cloudLightAttenuation, uint2(4, 0), 0).xyz, 
      LOAD_TEXTURE2D_LOD(_cloudLightAttenuation, uint2(5, 0), 0).xyz);
    float3 claMin_67 = min(LOAD_TEXTURE2D_LOD(_cloudLightAttenuation, uint2(5, 0), 0).xyz, 
      LOAD_TEXTURE2D_LOD(_cloudLightAttenuation, uint2(6, 0), 0).xyz);
    float3 cloudLightAttenuationMin = min(min(claMin_01, claMin_23), min(claMin_45, claMin_67));
    atmosphere *= cloudLightAttenuationMin;

    /* We also need to compute the screenspace scattering, which we'll do
     * directly but with no occlusion. */
    /* Make sure depth is distance to view aligned plane. */
    float3 cameraCenterD = -GetSkyViewDirWS(float2(_ScreenParams.x/2, _ScreenParams.y/2));
    float cosTheta = dot(cameraCenterD, d);
    float farClip = _ProjectionParams.z / max(cosTheta, 0.00001);
    float3 screenspaceScattering = 0;
    float3 screenspaceTransmittance = 1;
    float3 ambient = 0;
    Atmosphere::ComputeScreenspaceScattering(o, _WorldSpaceCameraPos1.xyz, d, float2(0, 0), min(t, farClip), 
       true, ambient, true, _atmosphereSettingsBuffer[0],
      screenspaceScattering, screenspaceTransmittance);

    /* Get clouds and cloud AP.*/
    float2 cloudUV = Mapping::mapPolar(d);
    float4 cloudLightingAndTransmittance = SAMPLE_TEXTURE2D_LOD(_cloudReflection, s_linear_clamp_sampler, cloudUV, 0);
    float cloudT = SAMPLE_TEXTURE2D_LOD(_cloudReflectionT, s_linear_clamp_sampler, cloudUV, 0).x;
    /* To avoid sampling the aerial perspective texture, just blend out with the sky in a constant way. */
    const float kBlendDistance = 150000;
    const float kBlendOffset = 0.5;
    cloudLightingAndTransmittance = lerp(cloudLightingAndTransmittance, float4(0, 0, 0, 1), saturate(kBlendOffset + cloudT/kBlendDistance));

    /* Composite. */
    float3 cloudsOnSky = atmosphere * cloudLightingAndTransmittance.w + cloudLightingAndTransmittance.xyz;
    float3 screenspaceOnClouds = cloudsOnSky * screenspaceTransmittance + screenspaceScattering;

    /* No exposure. */
    return float4(1 * screenspaceOnClouds, 1);
  }

  ENDHLSL

  SubShader {
    Pass {
      ZWrite Off
      ZTest Always
      Blend One Zero
      Cull Off

      HLSLPROGRAM
        #pragma fragment CompositeWithoutGeometry
      ENDHLSL
    }

    Pass {
      ZWrite Off
      ZTest Always
      Blend One SrcAlpha
      Cull Off

      HLSLPROGRAM
        #pragma fragment CompositeFullscreen
      ENDHLSL
    }

    Pass {
      ZWrite Off
      ZTest Always
      Cull Off

      HLSLPROGRAM
        #pragma fragment CompositeCubemap
      ENDHLSL
    }
  }
  Fallback Off
}
