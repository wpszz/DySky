Shader "DySky/Opaque/Diffuse"
{
	Properties
	{
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_MainTex("Base (RGB&A)", 2D) = "white" {}
	}

	CGINCLUDE
	#include "DySky.cginc"
	#include "Lighting.cginc"
	#include "AutoLight.cginc"
	ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 150

		Pass
		{
			Name "FORWARD"
			Tags { "LightMode" = "ForwardBase" }
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile LIGHTMAP_ON LIGHTPROBE_SH
			#pragma multi_compile __ SHADOWS_SCREEN
			#pragma multi_compile __ DY_SKY_FOG_ENABLE
			#pragma shader_feature DY_SKY_ALPHA_TEST_ON

			sampler2D _MainTex;
			float4 _MainTex_ST;

#ifdef DY_SKY_ALPHA_TEST_ON
			half _Cutoff;
#endif
			struct appdata_t
			{
				float4 vertex		: POSITION;
				float2 texcoord		: TEXCOORD0;
#ifdef LIGHTMAP_ON
				float4 texcoord1	: TEXCOORD1;
#endif
				float3 normal		: NORMAL;
			};

			struct v2f
			{
				float4 vertex		: POSITION;
				half2 uv			: TEXCOORD0;
				half3 worldNormal	: TEXCOORD1;
				half3 worldPos		: TEXCOORD2;
#ifdef LIGHTMAP_ON
				half2 lmap			: TEXCOORD3;
#elif UNITY_SHOULD_SAMPLE_SH
				half3 sh			: TEXCOORD3;
#endif
				UNITY_SHADOW_COORDS(4)
				DY_SKY_FOG_POS(5)
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

#ifdef LIGHTMAP_ON
				o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#elif UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
				o.sh = 0;
				// Approximated illumination from non-important point lights
	//#ifdef VERTEXLIGHT_ON
				o.sh += Shade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, o.worldPos, o.worldNormal);
	//#endif
				o.sh = ShadeSHPerVertex(o.worldNormal, o.sh);
#endif

				UNITY_TRANSFER_SHADOW(o, v.texcoord1.xy);	// coord used by SHADOWS_SHADOWMASK

				DY_SKY_FOG_VERT(v, o)
				return o;
			}

			fixed4 frag(v2f IN) : COLOR
			{
				fixed4 texCol = tex2D(_MainTex, IN.uv.xy);

#ifdef DY_SKY_ALPHA_TEST_ON
				clip(texCol.a - _Cutoff);
#endif
				fixed3 albedo = texCol.rgb;
				half3 lightDir = _WorldSpaceLightPos0.xyz;
				half3 lightColor = _LightColor0.rgb;

				// Unity shadow
				fixed atten = UNITY_SHADOW_ATTENUATION(IN, IN.worldPos);

				// Diffuse
				fixed diff = max(0, dot(IN.worldNormal, lightDir));
				fixed4 col = fixed4(0,0,0,1);
				col.rgb += albedo * lightColor * diff * atten;

				// GI
#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
				UnityGIInput giInput;
				UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
				giInput.light.dir = lightDir;
				giInput.light.color = lightColor;
				giInput.worldPos = IN.worldPos;
				giInput.atten = atten;
	#if defined(LIGHTMAP_ON)
				giInput.lightmapUV.xy = IN.lmap;
	#else
				giInput.lightmapUV = 0.0;
	#endif
	#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
				giInput.ambient = IN.sh;
	#else
				giInput.ambient.rgb = 0.0;
	#endif
				UnityGI gi = UnityGlobalIllumination(giInput, 1.0, IN.worldNormal);
				col.rgb += albedo * gi.indirect.diffuse;
#endif
				// DySky Fog
				DY_SKY_FOG_FRAG(IN, col)
				return col;
			}

			ENDCG
		}

		// Pass to render object as a shadow caster
		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_shadowcaster
			#pragma shader_feature DY_SKY_ALPHA_TEST_ON
			#include "UnityCG.cginc"

			struct v2f {
				V2F_SHADOW_CASTER;

#ifdef DY_SKY_ALPHA_TEST_ON
				float2 uv : TEXCOORD1;
#endif

				UNITY_VERTEX_OUTPUT_STEREO
			};

#ifdef DY_SKY_ALPHA_TEST_ON
			sampler2D _MainTex;
			float4 _MainTex_ST;
			half _Cutoff;
#endif

			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

#ifdef DY_SKY_ALPHA_TEST_ON
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
#endif

				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
#ifdef DY_SKY_ALPHA_TEST_ON
				fixed4 texCol = tex2D(_MainTex, i.uv);
				clip(texCol.a - _Cutoff);
#endif

				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}

	CustomEditor "DySkyShaderDiffuseEditor"
}
