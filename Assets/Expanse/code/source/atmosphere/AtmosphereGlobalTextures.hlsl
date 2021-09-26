#ifndef EXPANSE_ATMOSPHERE_GLOBAL_TEXTURES_INCLUDED
#define EXPANSE_ATMOSPHERE_GLOBAL_TEXTURES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"

TEXTURE3D(_EXPANSE_AERIAL_PERPSECTIVE);
TEXTURE3D(_EXPANSE_FOG);
float _AP_depthSkew;
float _Screenspace_depthSkew;

/* Maps linear depth to frustum coordinate. Redefinition of function from AtmosphereMapping.hlsl, 
 * copied here to avoid _TAAJitterStrength undefined error. */
float3 mapFrustumCoordinate(float2 screenSpaceUV, float linear01Depth, float depthSkew) {
    return float3(screenSpaceUV, pow(saturate(linear01Depth), 1.0/depthSkew));
}

void SampleExpanseFog_float(float linear01Depth, float2 uv, out float4 Color) {
    float3 fogUV = mapFrustumCoordinate(uv, linear01Depth, _Screenspace_depthSkew);
    float4 fog = SAMPLE_TEXTURE3D_LOD(_EXPANSE_FOG, s_linear_clamp_sampler, fogUV, 0);
    float3 apUV = mapFrustumCoordinate(uv, linear01Depth, _AP_depthSkew);
    float4 ap = SAMPLE_TEXTURE3D_LOD(_EXPANSE_AERIAL_PERPSECTIVE, s_linear_clamp_sampler, apUV, 0);
    float3 fogOnAP = ap.xyz * fog.w + fog.xyz;
    float alpha = exp(ap.w) * fog.w;
    Color = float4(fogOnAP, alpha);
}

#endif // EXPANSE_ATMOSPHERE_GLOBAL_TEXTURES_INCLUDED