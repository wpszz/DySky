Shader "DySky/Water/Standard"
{
	Properties
	{
		[NoScaleOffset][HideInInspector]
		_WaveTex("Wave Texture (RGB)", 2D) = "white" {}
		_WaveTiling("Wave Tiling", Range(0.01,30.0)) = 2.0
		_WaveFactor("Wave Factor", Range(0.0, 2.0)) = 1.0
		_WaveStrength("Wave Strength", Range(0.01, 1.0)) = 0.3
		_WaveSpeedX("Wave Speed X", Range(-1.0, 1.0)) = 0.25
		_WaveSpeedZ("Wave Speed Z", Range(-1.0, 1.0)) = -0.25

		_DepthOpaqueFactor("Opaque Factor", Range(5.0, 50.0)) = 10

		_SpecularPower("Specular Power", Range(1.0, 500.0)) = 150.0
		_FresnelScale("Freshnel Scale", Range(0.1, 1.0)) = 0.5
		_BaseColor("Base color", COLOR) = (0.3820755, 0.7432312, .99, 0.5)
		_ReflectColor("Reflect color", COLOR) = (1.0, 1.0, 1.0, 0.7)
		_DistortionStrength("Distortion Strength", Range(0.01, 0.3)) = 0.05

		_EdgeInvFade("Edge Soft Factor", Range(0.01,10.0)) = 3.0

		[NoScaleOffset][HideInInspector]
		_EdgeFoamTex("Foam Texture (R)", 2D) = "white" {}
		_EdgeFoamScale("Foam Scale", Range(0.3, 0.8)) = 0.5
	}

	CGINCLUDE
	#include "DySky.cginc"

	sampler2D _WaveTex;
	half _WaveTiling;
	half _WaveFactor;
	half _WaveStrength;
	half _WaveSpeedX;
	half _WaveSpeedZ;

	half _DepthOpaqueFactor;

	half _SpecularPower;
	half _FresnelScale;
	half4 _BaseColor;
	half4 _ReflectColor;

	sampler2D _CameraColorTexture;
	half _DistortionStrength;

	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
	half _EdgeInvFade;
	sampler2D _EdgeFoamTex;
	half _EdgeFoamScale;

#if defined(DY_SKY_WATER_HIGH) || defined(DY_SKY_WATER_MID)
#define DY_FLOAT  float
#define DY_FLOAT2 float2
#define DY_FLOAT3 float3
#define DY_FLOAT4 float4
#else
#define DY_FLOAT  half
#define DY_FLOAT2 half2
#define DY_FLOAT3 half3
#define DY_FLOAT4 half4
#endif

	struct appdata_t
	{
		float4 vertex		: POSITION;
		DY_FLOAT2 uv		: TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex		: POSITION;
		DY_FLOAT2 uv		: TEXCOORD0;
		half3 viewDir		: TEXCOORD1;

#ifdef DY_SKY_SOFT_EDGE_ENABLE
		half4 projPos		: TEXCOORD2;
#endif

#ifdef DY_SKY_GRAB_PASS_ENABLE
		half4 grabPos		: TEXCOORD3;
#endif
		DY_SKY_FOG_POS(4)
	};

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv.xy * _WaveTiling;

		half3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		o.viewDir = worldPos - _WorldSpaceCameraPos;

#ifdef DY_SKY_SOFT_EDGE_ENABLE
		o.projPos = ComputeScreenPos(o.vertex);
		COMPUTE_EYEDEPTH(o.projPos.z);
#endif

#ifdef DY_SKY_GRAB_PASS_ENABLE
		o.grabPos = ComputeGrabScreenPos(o.vertex);
#endif

		DY_SKY_FOG_VERT(v, o)
		return o;
	}

	DY_FLOAT3 get_wave_normal(DY_FLOAT2 uv, DY_FLOAT z) {
		DY_FLOAT2 speed = DY_FLOAT2(_WaveSpeedX, _WaveSpeedZ) * _Time.x;

		DY_FLOAT3 n1 = tex2D(_WaveTex, uv + speed);
		DY_FLOAT3 n2 = tex2D(_WaveTex, uv * 0.5 - speed * 0.25);
		n1 = n1 * 2.0 - 1.0;
		n2 = n2 * 2.0 - 1.0;
		n1 = lerp(DY_FLOAT3(0, 0, 1), n1, _WaveStrength);
		n2 = lerp(DY_FLOAT3(0, 0, 1), n2, _WaveStrength);

		DY_FLOAT3 nt = DY_FLOAT3(n1.xy + n2.xy, n1.z * n2.z);

	#if defined(DY_SKY_WATER_HIGH)
		DY_FLOAT zFactor = saturate(lerp(2.0, 0.0, z / 1000));
		nt = lerp(DY_FLOAT3(0, 0, 1), nt, zFactor);

		DY_FLOAT3 n3 = tex2D(_WaveTex, uv * 6.0 + speed * 0.5 + nt.xy * 0.06 * lerp(0.0, 1.0, nt.z));
		n3 = n3 * 2.0 - 1.0;
		n3 = lerp(DY_FLOAT3(0, 0, 1), n3, _WaveStrength);

		nt = lerp(nt, DY_FLOAT3(nt.xy + n3.xy, nt.z * n3.z), _WaveFactor);
		nt = lerp(DY_FLOAT3(0, 0, 1), nt, zFactor);
	#elif defined(DY_SKY_WATER_MID)
		DY_FLOAT3 n3 = tex2D(_WaveTex, uv * 6.0 + speed * 0.5 + nt.xy * 0.06 * lerp(0.0, 1.0, nt.z));
		n3 = n3 * 2.0 - 1.0;
		n3 = lerp(DY_FLOAT3(0, 0, 1), n3, _WaveStrength);

		nt = lerp(nt, DY_FLOAT3(nt.xy + n3.xy, nt.z * n3.z), _WaveFactor);
	#endif
	#if 0
		/*
			tangent space
		*/
		return normalize(nt);
	#else
		/* 
			convert to world space 
			worldTangent  = float3(1, 0, 0);
			worldBinormal = float3(0, 0, 1);
			worldNormal   = float3(0, 1, 0);
		*/
		return normalize(DY_FLOAT3(nt.x, nt.z, nt.y));
	#endif
	}

	fixed4 frag(v2f i) : COLOR
	{
		half3 viewDir = -normalize(i.viewDir);

		half3 normal = get_wave_normal(i.uv, i.vertex.w);

#ifdef DY_SKY_SOFT_EDGE_ENABLE
		half sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
		half partZ = i.projPos.z;
		half depthOpaque = smoothstep(5.0, -_DepthOpaqueFactor, partZ - sceneZ);
#else
		half depthOpaque = 0.8;
#endif

#ifdef DY_SKY_GRAB_PASS_ENABLE
		half4 grabPos = UNITY_PROJ_COORD(i.grabPos);
		grabPos.xy += normal.xz * _DistortionStrength;
		half4 grabColor = tex2Dproj(_CameraColorTexture, grabPos);
		half4 refractColor = lerp(grabColor, _BaseColor, _BaseColor.a * depthOpaque);
#else
		half4 refractColor = half4(_BaseColor.rgb, _BaseColor.a * depthOpaque);
#endif

#ifdef DY_SKY_REFLECT_SKY
		half4 reflectColor = texCUBE(_DySky_texReflectSky, reflect(-viewDir, normal));
#else
		half4 reflectColor = _ReflectColor;
#endif

		half3 h = normalize(_WorldSpaceLightPos0.xyz + viewDir);
		half ndoth = max(0, dot(normal, h));
		half specular = pow(ndoth, _SpecularPower);
		half ndotv = max(0, dot(normal * half3(_FresnelScale, 1, _FresnelScale), viewDir));
		half fresnel = pow(1.0 - ndotv, 5);

		half4 col = lerp(refractColor, reflectColor, fresnel);
		col += reflectColor * specular * _LightColor0;

#ifdef DY_SKY_SOFT_EDGE_ENABLE
		//half sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
		//half partZ = i.projPos.z;
		half fade = saturate(_EdgeInvFade * (sceneZ - partZ));
		half eyeSoft = smoothstep(0.3, 0.35, partZ);
		col.a *= min(fade, eyeSoft);

	#ifdef DY_SKY_FOAM_EDGE_ENABLE
		half foam = tex2D(_EdgeFoamTex, half2(-fade - _EdgeFoamScale, -0.5) + normal.xy * 0.35).r;
		foam *= 1.0 - fade;
		foam *= sqrt(ndotv);
		col.a += smoothstep(0.2, 0.5, min(fade, foam));
		col.rgb += _LightColor0 * foam;
	#endif
#endif

		// DySky Fog
		DY_SKY_FOG_FRAG_ALPHA(i, col, 1.0)
		return col;
	}

	ENDCG

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "LightMode" = "ForwardBase" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest LEqual
		ZWrite Off
		Cull Off
		LOD 300

		GrabPass{
			Tags { "LightMode" = "Always" }
			"_CameraColorTexture" 
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile __ DY_SKY_FOG_ENABLE
			#pragma multi_compile DY_SKY_GRAB_PASS_ENABLE
			#pragma multi_compile DY_SKY_SOFT_EDGE_ENABLE
			#pragma multi_compile DY_SKY_WATER_HIGH
			#pragma shader_feature DY_SKY_FOAM_EDGE_ENABLE
			#pragma shader_feature DY_SKY_REFLECT_SKY

			ENDCG
		}
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "LightMode" = "ForwardBase" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest LEqual
		ZWrite Off
		Cull Off
		LOD 250

		GrabPass{
			Tags { "LightMode" = "Always" }
			"_CameraColorTexture"
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile __ DY_SKY_FOG_ENABLE
			#pragma multi_compile DY_SKY_GRAB_PASS_ENABLE
			#pragma multi_compile DY_SKY_SOFT_EDGE_ENABLE
			#pragma multi_compile DY_SKY_WATER_MID

			ENDCG
		}
	}

	SubShader
	{
		Tags {"RenderType" = "Transparent" "Queue" = "Transparent" "LightMode" = "ForwardBase" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest LEqual
		ZWrite Off
		Cull Off
		LOD 200

		GrabPass{
			Tags { "LightMode" = "Always" }
			"_CameraColorTexture" 
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile __ DY_SKY_FOG_ENABLE
			#pragma multi_compile DY_SKY_GRAB_PASS_ENABLE
			#pragma multi_compile DY_SKY_WATER_MID

			ENDCG
		}
	}

	SubShader
	{
		Tags {"RenderType" = "Transparent" "Queue" = "Transparent" "LightMode" = "ForwardBase" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest LEqual
		ZWrite Off
		Cull Off
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile __ DY_SKY_FOG_ENABLE

			ENDCG
		}
	}

	CustomEditor "DySkyShaderWaterEditor"
}
