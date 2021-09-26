#ifndef EXPANSE_TRANSPARENCY_INCLUDED
#define EXPANSE_TRANSPARENCY_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"
#include "../../code/source/common/Utilities.hlsl"
#include "../../code/source/clouds/CloudGlobalTextures.hlsl"
#include "../../code/source/atmosphere/AtmosphereGlobalTextures.hlsl"
#include "../../code/source/main/SkyCompositorGlobalTextures.hlsl"

float4 EvaluateExpanseFog(float linear01Depth, float2 uv, float4 color, float exposure) {
    float4 fog = float4(0, 0, 0, 1);
    SampleExpanseFog_float(linear01Depth, uv, fog);
    return float4(color.xyz * fog.w + exposure * fog.xyz, color.w);
}

float4 EvaluateExpanseFogAndClouds(float linear01Depth, float2 uv, float4 color, float exposure) {
    float4 outColor = color;
    
    /* Here's all the things we'll be sampling + compositing. */
    float4 fogOnGeometry = float4(0, 0, 0, 1);
    float3 cloudColor = 0;
    float3 cloudAlpha = 1;
    float cloudT = 0;
    float4 fogOnClouds = float4(0, 0, 0, 1);
    float4 finalResult = float4(0, 0, 0, 1);

    /* Sample distant fog and clouds. */
    SampleExpanseFog_float(linear01Depth, uv, fogOnGeometry);
    SampleExpanseClouds_float(uv, cloudColor, cloudAlpha, cloudT);
    fogOnGeometry.xyz *= exposure;
    cloudColor *= exposure;

    finalResult.w = fogOnGeometry.w;
    finalResult.xyz = fogOnGeometry.xyz;
    
    /* Only sample fog on clouds if the clouds are in front of the
     * geometry. */
    float3 view = -GetSkyViewDirWS(float2(_ScreenParams.x/2, _ScreenParams.y/2));
    float3 d = -GetSkyViewDirWS(uv * _ScreenParams.xy);
    float eyeMultiplier = dot(view, d);
    float cloudLinearDepth = saturate((cloudT * _ProjectionParams.w * eyeMultiplier));
    if (cloudLinearDepth < linear01Depth) {
        SampleExpanseFog_float(cloudLinearDepth, uv, fogOnClouds);
        fogOnClouds.xyz *= exposure;
        finalResult.w *= Utilities::average(cloudAlpha);
        finalResult.xyz *= cloudAlpha;
        finalResult.xyz += cloudColor * fogOnClouds.w;
        finalResult.xyz += fogOnClouds.xyz * (1 - Utilities::average(cloudAlpha));
    }

    /* Perform clip blending. */
    if (_EXPANSE_CLIP_FADE < 1) {
        float clipPlaneAlpha = evaluateClipBlend(linear01Depth, uv);
        float4 noGeo = SAMPLE_TEXTURE2D_LOD(_EXPANSE_NO_GEOMETRY_FRAMEBUFFER, s_linear_clamp_sampler, uv, 0);
        finalResult = lerp(finalResult, noGeo, clipPlaneAlpha);
    }

    /* Composite it all on top of geometry. */
    outColor.xyz *= finalResult.w;
    outColor.xyz += outColor.w * finalResult.xyz;

    return outColor;
}

#endif // EXPANSE_TRANSPARENCY_INCLUDED