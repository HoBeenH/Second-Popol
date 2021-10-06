// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Effects/Explosions/DistortionFresnel" {
Properties {
		_BumpAmt ("Distortion", Float) = 10
		_FresnelPow ("Fresnel Pow", Float) = 5
		_FresnelR0 ("Fresnel R0", Float) = 0.04
}

Category {

	Tags { "Queue"="Transparent"  "IgnoreProjector"="True"  "RenderType"="Transparent" }
	Blend SrcAlpha One
	Cull Off

	ZWrite Off


	SubShader {

		Pass {
			//Name "BASE"
			//Tags { "LightMode" = "Always" }

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#pragma multi_compile_particles
#include "UnityCG.cginc"

struct appdata_t {
	float4 vertex : POSITION;
	float2 texcoord: TEXCOORD0;
	float4 color : COLOR;
	half3 normal : NORMAL;
};

struct v2f {
	float4 vertex : POSITION;
	float4 uvgrab : TEXCOORD0;
	float4 color : COLOR;
	#ifdef SOFTPARTICLES_ON
		float4 projPos : TEXCOORD3;
	#endif
	half fresnel : TEXCOORD4;
};

half _FresnelPow;
half _FresnelR0;
float _BumpAmt;
sampler2D _GrabTexture;
float4 _GrabTexture_TexelSize;

v2f vert (appdata_t v)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	#ifdef SOFTPARTICLES_ON
		o.projPos = ComputeScreenPos (UnityObjectToClipPos(v.vertex));
		COMPUTE_EYEDEPTH(o.projPos.z);
	#endif
	o.color = v.color;
	#if UNITY_UV_STARTS_AT_TOP
	float scale = -1.0;
	#else
	float scale = 1.0;
	#endif
	o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
	o.uvgrab.zw = o.vertex.w;
#if UNITY_SINGLE_PASS_STEREO
	o.uvgrab.xy = TransformStereoScreenSpaceTex(o.uvgrab.xy, o.uvgrab.w);
#endif

	o.fresnel = (1 - abs(dot(normalize(v.normal), normalize(ObjSpaceViewDir(v.vertex)))));
	o.fresnel = pow(o.fresnel, _FresnelPow);
	o.fresnel = saturate(_FresnelR0 + (1.0 - _FresnelR0) * o.fresnel);

	return o;
}


half4 frag( v2f i ) : COLOR
{
	return saturate(i.fresnel) * i.color.a * 0.5;
}
ENDCG
		}
	}


}

}

