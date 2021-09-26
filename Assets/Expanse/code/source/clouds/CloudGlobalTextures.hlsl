#ifndef EXPANSE_CLOUD_GLOBAL_TEXTURES_INCLUDED
#define EXPANSE_CLOUD_GLOBAL_TEXTURES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"

TEXTURE2D_ARRAY(_EXPANSE_CLOUD_LIGHTING_FRAMEBUFFERS);
TEXTURE2D_ARRAY(_EXPANSE_CLOUD_TRANSMITTANCE_FRAMEBUFFERS);
TEXTURE2D(_EXPANSE_CLOUD_LIGHTING_COMPOSITE);
TEXTURE2D(_EXPANSE_CLOUD_TRANSMITTANCE_COMPOSITE);
TEXTURE2D(_EXPANSE_CLOUD_REFLECTION);
bool _EXPANSE_CLOUD_USE_ARRAY;

void SampleExpanseClouds_float(float2 uv, out float3 Color, out float3 Alpha, out float T) {
    if (_EXPANSE_CLOUD_USE_ARRAY) 
    {
        Color = SAMPLE_TEXTURE2D_ARRAY_LOD(_EXPANSE_CLOUD_LIGHTING_FRAMEBUFFERS, s_linear_clamp_sampler, uv, 0, 0).xyz;
        float4 alphaT = SAMPLE_TEXTURE2D_ARRAY_LOD(_EXPANSE_CLOUD_TRANSMITTANCE_FRAMEBUFFERS, s_linear_clamp_sampler, uv, 0, 0);
        Alpha = alphaT.xyz;
        T = alphaT.w;
    }
    else 
    {
        Color = SAMPLE_TEXTURE2D_LOD(_EXPANSE_CLOUD_LIGHTING_COMPOSITE, s_linear_clamp_sampler, uv, 0).xyz;
        float4 alphaT = SAMPLE_TEXTURE2D_LOD(_EXPANSE_CLOUD_TRANSMITTANCE_COMPOSITE, s_linear_clamp_sampler, uv, 0);
        Alpha = alphaT.xyz;
        T = alphaT.w;
    }
}

void SampleExpanseCloudsHit_float(float2 uv, out float T) {
    if (_EXPANSE_CLOUD_USE_ARRAY) 
    {
        T = SAMPLE_TEXTURE2D_ARRAY_LOD(_EXPANSE_CLOUD_TRANSMITTANCE_FRAMEBUFFERS, s_linear_clamp_sampler, uv, 0, 0).w;
    }
    else 
    {
        T = SAMPLE_TEXTURE2D_LOD(_EXPANSE_CLOUD_TRANSMITTANCE_COMPOSITE, s_linear_clamp_sampler, uv, 0).w;
    }
}

#endif // EXPANSE_CLOUD_GLOBAL_TEXTURES_INCLUDED