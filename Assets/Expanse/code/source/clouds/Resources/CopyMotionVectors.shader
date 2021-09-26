Shader "Hidden/HDRP/Sky/Copy Motion Vectors" {
  HLSLINCLUDE
  
  #pragma vertex Vert

  #pragma editor_sync_compilation
  #pragma target 4.5
  #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

  #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
  #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
  #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Sky/SkyUtils.hlsl"

  struct Attributes {
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  struct Varyings {
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
  };

  Varyings Vert(Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
    return output;
  }

  float4 CopyMotionVectors(Varyings input) : SV_Target {
    return float4(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, input.positionCS.xy).xy, 0, 0);
  }

  ENDHLSL

  SubShader {
    Pass {
      ZWrite Off
      ZTest Always
      Cull Off

      HLSLPROGRAM
        #pragma fragment CopyMotionVectors
      ENDHLSL
    }
  }
  Fallback Off
}
