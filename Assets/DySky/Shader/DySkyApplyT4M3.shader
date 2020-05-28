Shader "DySky/Opaque/T4M3"
{
	Properties
	{
		_Control("Control (RGB)", 2D) = "white" {}
		_Splat0("Layer 1", 2D) = "white" {}
		_Splat1("Layer 2", 2D) = "white" {}
		_Splat2("Layer 3", 2D) = "white" {}
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

			sampler2D _Control;
			float4 _Control_ST;
			sampler2D _Splat0;
			float4 _Splat0_ST;
			sampler2D _Splat1;
			float4 _Splat1_ST;
			sampler2D _Splat2;
			float4 _Splat2_ST;

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
				half4 uv0			: TEXCOORD0;
				half4 uv1			: TEXCOORD1;
				half3 worldNormal	: TEXCOORD2;
				half3 worldPos		: TEXCOORD3;
#ifdef LIGHTMAP_ON
				half2 lmap			: TEXCOORD4;
#elif UNITY_SHOULD_SAMPLE_SH
				half3 sh			: TEXCOORD4;
#endif
				UNITY_SHADOW_COORDS(5)
				DY_SKY_FOG_POS(6)
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv0.xy = TRANSFORM_TEX(v.texcoord, _Control);
				o.uv0.zw = TRANSFORM_TEX(v.texcoord, _Splat0);
				o.uv1.xy = TRANSFORM_TEX(v.texcoord, _Splat1);
				o.uv1.zw = TRANSFORM_TEX(v.texcoord, _Splat2);
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

				UNITY_TRANSFER_SHADOW(o, v.texcoord1.xy); // coord used by SHADOWS_SHADOWMASK

				DY_SKY_FOG_VERT(v, o)
				return o;
			}

			fixed4 frag(v2f IN) : COLOR
			{
				fixed3 splat_control = tex2D(_Control, IN.uv0.xy).rgb;
				fixed3 lay1 = tex2D(_Splat0, IN.uv0.zw);
				fixed3 lay2 = tex2D(_Splat1, IN.uv1.xy);
				fixed3 lay3 = tex2D(_Splat2, IN.uv1.zw);

				fixed3 albedo = lay1 * splat_control.r + lay2 * splat_control.g + lay3 * splat_control.b;

				// Unity shadow
				fixed atten = UNITY_SHADOW_ATTENUATION(IN, IN.worldPos);

				fixed4 col = fixed4(0, 0, 0, 1);

				// Realtime diffuse
#ifndef LIGHTMAP_ON
				fixed diff = max(0, dot(IN.worldNormal, _WorldSpaceLightPos0.xyz));
				col.rgb += albedo * _LightColor0.rgb * diff * atten;
#endif

				// GI
#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
#if defined(LIGHTMAP_ON)
				half2 lmap = IN.lmap.xy;
#else
				half2 lmap = half2(0, 0);
#endif
#if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
				half3 ambient = IN.sh;
#else
				half3 ambient = half3(0, 0, 0);
#endif
				col.rgb += albedo * DySkyGI(atten, IN.worldPos, IN.worldNormal, ambient, lmap);
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
			#include "UnityCG.cginc"

			struct v2f {
				V2F_SHADOW_CASTER;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}
