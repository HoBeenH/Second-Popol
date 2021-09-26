#ifndef EXPANSE_SKY_COMPOSITOR_GLOBAL_TEXTURES_INCLUDED
#define EXPANSE_SKY_COMPOSITOR_GLOBAL_TEXTURES_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesFunctions.hlsl"

TEXTURE2D(_EXPANSE_NO_GEOMETRY_FRAMEBUFFER);
float _EXPANSE_CLIP_FADE;

float evaluateClipBlend(float linear01Depth, float2 uv) {        
    /* Scale linear depth by eye multiplier, so that the fadeout is round. */
    float3 view = float3(0, 0, 1);
    float3 d = normalize(float3(uv * 2 - 1, 1));
    float eyeMultiplier = dot(view, d);
    return saturate((saturate(linear01Depth / eyeMultiplier) - _EXPANSE_CLIP_FADE) / (1 - _EXPANSE_CLIP_FADE));
}

#endif // EXPANSE_SKY_COMPOSITOR_GLOBAL_TEXTURES_INCLUDED