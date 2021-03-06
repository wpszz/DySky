﻿Shader "DySky/Particles/Standard" 
{
	Properties 
	{
		_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex("Particle Texture", 2D) = "white" {}
		_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0

		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
	}

	CGINCLUDE
	#include "DySky.cginc"
	ENDCG

	Category
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		Blend [_SrcBlend] [_DstBlend]
		ColorMask RGB
		Cull Off Lighting Off ZWrite Off

		SubShader 
		{
			Pass 
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_particles
				#pragma multi_compile __ DY_SKY_FOG_ENABLE
				#pragma shader_feature DY_SKY_PARTICLE_ADD DY_SKY_PARTICLE_ADD_SMOOTH DY_SKY_PARTICLE_BLEND

				sampler2D _MainTex;
				float4 _MainTex_ST;
				fixed4 _TintColor;

				UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
				half _InvFade;

				struct appdata_t 
				{
					float4 vertex	: POSITION;
					fixed4 color	: COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f 
				{
					float4 vertex	: SV_POSITION;
					fixed4 color	: COLOR;
					half2 texcoord	: TEXCOORD0;

#ifdef SOFTPARTICLES_ON
					half4 projPos	: TEXCOORD1;
#endif

					DY_SKY_FOG_POS(2)
				};

				v2f vert(appdata_t v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);

					o.vertex = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

#ifdef SOFTPARTICLES_ON
					o.projPos = ComputeScreenPos(o.vertex);
					COMPUTE_EYEDEPTH(o.projPos.z);
#endif

					DY_SKY_FOG_VERT(v, o)
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
#ifdef SOFTPARTICLES_ON
					half sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
					half partZ = i.projPos.z;
					half fade = saturate(_InvFade * (sceneZ - partZ));
					i.color.a *= fade;
#endif

					fixed4 col = _TintColor * i.color * tex2D(_MainTex, i.texcoord);
#ifdef DY_SKY_PARTICLE_ADD
					col *= 2.0f;
					col.a = saturate(col.a); // alpha should not have double-brightness applied to it, but we can't fix that legacy behaior without breaking everyone's effects, so instead clamp the output to get sensible HDR behavior (case 967476)

					// DySky Fog
					DY_SKY_FOG_FRAG_ALPHA(i, col, 0.0)  // fog towards black due to our blend mode
#elif DY_SKY_PARTICLE_ADD_SMOOTH
					col.rgb *= col.a;

					// DySky Fog
					DY_SKY_FOG_FRAG_ALPHA(i, col, 0.0); // fog towards black due to our blend mode
#elif DY_SKY_PARTICLE_BLEND
					col *= 2.0f;
					col.a = saturate(col.a); // alpha should not have double-brightness applied to it, but we can't fix that legacy behaior without breaking everyone's effects, so instead clamp the output to get sensible HDR behavior (case 967476)

					// DySky Fog
					DY_SKY_FOG_FRAG_ALPHA(i, col, 1.0)
#endif
					return col;
				}
				ENDCG
			}
		}
	}
	CustomEditor "DySkyShaderParticleEditor"
}

