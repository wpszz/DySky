Shader "DySky/Opaque/PBR"
{
	Properties
	{
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_MainTex("Base (RGB&A)", 2D) = "white" {}
		[NoScaleOffset]_BumpMap("Normal Map", 2D) = "bump" {}
		[Gamma]_Metallic("Metallic", Range(0,1)) = 0.0
		_Smoothness("Smoothness", Range(0,1)) = 0.5
		[HideInInspector]_EnvBRDFLUT("Environment BRDF Lut", 2D) = "white" {}
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
			sampler2D _BumpMap;
			sampler2D _EnvBRDFLUT;
			half _Metallic;
			half _Smoothness;

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
				float4 tangent		: TANGENT;
			};

			struct v2f
			{
				float4 vertex		: POSITION;
				half2 uv			: TEXCOORD0;
				half4 tSpace0		: TEXCOORD1;
				half4 tSpace1		: TEXCOORD2;
				half4 tSpace2		: TEXCOORD3;
#ifdef LIGHTMAP_ON
				half2 lmap			: TEXCOORD4;
#elif UNITY_SHOULD_SAMPLE_SH
				half3 sh			: TEXCOORD4;
#endif
				UNITY_SHADOW_COORDS(5)
				DY_SKY_FOG_POS(6)
			};

			// NDF term, GGX / Trowbridge-Reitz
			// [Walter et al. 2007, "Microfacet models for refraction through rough surfaces"]
			inline half D_GGX(half roughness, half nh)
			{
				half a = roughness * roughness;
				half a2 = a * a;
				half d = (nh * a2 - nh) * nh + 1.00001h;
				return a2 / (d * d + 1e-7);
			}

			// Visibility term, Appoximation of joint Smith term for GGX
			// [Heitz 2014, "Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs"]
			inline half V_SmithJointGGX(half roughness, half nv, half nl)
			{
				half a = roughness * roughness;
				half smithV = nl * (nv * (1 - a) + a);
				half smithL = nv * (nl * (1 - a) + a);
				return 0.5 / (smithV + smithL + 1e-5f);
			}

			// Fresnel term, Schlick Appoximation
			// [Schlick 1994, "An Inexpensive BRDF Model for Physically-Based Rendering"]
			inline half3 F_Schlick(half3 f0, half vh)
			{
				half t = Pow5(1 - vh);
				return f0 + (1 - f0) * t;
			}

			// ref: https://zhuanlan.zhihu.com/p/41150563
			half3 EnvBRDFGGX(half3 specColor, half roughness, half nv)
			{
				const half4 c0 = { -1, -0.0275, -0.26, 0.0109 };
				const half4 c1 = { 1, 0.0455, 1.0417, -0.0417 };
				half4 r = roughness * c0 + c1;
				half a004 = min(0.9 - 0.75 * roughness, Pow5(1 - nv)) * r.x + r.y;
				half2 AB = half2(-1.0417, 1.0417) * a004 + r.zw;
				return specColor * AB.x + AB.y;
			}

			inline half3 BRDF(half3 normal, half3 viewDir, half3 lightDir, half3 lightColor, half roughness,
							  half3 diffuse, half3 specular, half3 indirectDiffuse, half3 indirectSpecular)
			{
				half3 halfDir = normalize(lightDir + viewDir);

				half nv = max(0, dot(normal, viewDir));
				half nl = max(0, dot(normal, lightDir));
				half nh = max(0, dot(normal, halfDir));
				half lh = max(0, dot(lightDir, halfDir));
#if USE_ENV_BRDF_LUT		
				half2 envBRDF = tex2D(_EnvBRDFLUT, half2(nv, roughness)).xy;
	#ifdef UNITY_COLORSPACE_GAMMA
				envBRDF *= envBRDF;
	#endif
				half3 F0 = specular * envBRDF.x + envBRDF.y;
#else
				half3 F0 = EnvBRDFGGX(specular, roughness, nv);
#endif
				half D = D_GGX(roughness, nh);
				half V = V_SmithJointGGX(roughness, nv, nl);
				half3 F = F_Schlick(specular, lh);

				half3 specularTerm = D * V * F;

				half3 color = (diffuse + specularTerm) * lightColor * nl + indirectDiffuse * diffuse + indirectSpecular * F0;

				return color;
			}

			v2f vert(appdata_t v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

				half3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
				o.tSpace0 = half4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				o.tSpace1 = half4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				o.tSpace2 = half4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

#ifdef LIGHTMAP_ON
				o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#elif UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
				o.sh = 0;
				// Approximated illumination from non-important point lights
	//#ifdef VERTEXLIGHT_ON
				o.sh += Shade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, worldPos, worldNormal);
	//#endif
				o.sh = ShadeSHPerVertex(worldNormal, o.sh);
#endif

				UNITY_TRANSFER_SHADOW(o, v.texcoord1.xy);

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

				half3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv.xy));
				half3 specular = lerp(unity_ColorSpaceDielectricSpec.rgb, albedo, _Metallic);
				half oneMinusReflectivity = (1 - _Metallic) * unity_ColorSpaceDielectricSpec.a;
				half roughness = (1 - _Smoothness * texCol.a);
				half3 diffuse = albedo * oneMinusReflectivity;

				half3 worldPos = half3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
				half3 worldNormal = normalize(half3(dot(IN.tSpace0.xyz, normal), dot(IN.tSpace1.xyz, normal), dot(IN.tSpace2.xyz, normal)));
				half3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
				half3 worldLightDir = _WorldSpaceLightPos0.xyz;

				// Unity shadow
				fixed atten = UNITY_SHADOW_ATTENUATION(IN, worldPos);

				fixed4 col = fixed4(0, 0, 0, 1);

				half3 indirectDiffuse = 0;
				half3 indirectSpecular = 0;
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
				indirectDiffuse = DySkyGI(atten, worldPos, worldNormal, ambient, lmap);
				indirectSpecular = DySkyGI_IndirectSpecular(worldNormal, worldViewDir, roughness);
#endif

				half3 lightColor = _LightColor0.rgb * atten;
				col.rgb = BRDF(worldNormal, worldViewDir, worldLightDir, lightColor, roughness, diffuse, specular, indirectDiffuse, indirectSpecular);

				// Exposure.
				col = saturate(1.0 - exp(-_DySky_tExposure * col));

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

	CustomEditor "DySkyShaderPBREditor"
}
