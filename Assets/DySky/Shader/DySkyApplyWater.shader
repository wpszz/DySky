Shader "DySky/Water/Standard"
{
	Properties
	{
		[NoScaleOffset]
		_WaveTex("Wave Texture (RGB)", 2D) = "white" {}
		_WaveTiling("Wave Tiling", Vector) = (2.0 ,2.0, -2.0, 2.0)
		_WaveTilingScale("Wave Tiling Scale", Range(0.1, 5.0)) = 1.0
		_WaveSpeed("Wave Speed", Vector) = (1.0 ,1.0, -1.0, 1.0)
		_WaveStrength("Wave Strength", Range(0.5, 3.0)) = 1.0
		_SpecularPower("Specular Power", Range(5.0, 200.0)) = 100.0
		_FresnelPower("Freshnel Power", Range(1.0, 100.0)) = 50.0
		_FresnelScale("Freshnel Scale", Range(0.1, 1.0)) = 0.5
		_BaseColor("Base color", COLOR) = (0.3820755, 0.7432312, .99, 0.5)
		_SpecularColor("Specular Reflection", COLOR) = (1.0, 1.0, 1.0, 0.5)
		_FresnelColor("Freshnel Reflection", COLOR) = (1.0, 1.0, 1.0, 0.8)
		_DistortionStrength("Distortion Strength", Range(0.01,2.0)) = 0.3

		_EdgeInvFade("Edge Soft Factor", Range(0.01,10.0)) = 1.0
		[NoScaleOffset]
		_EdgeFoamTex("Edge Foam Texture (R)", 2D) = "white" {}
		_EdgeFoamFreq("Edge Foam Frequency", Range(0.01,1.0)) = 1.0
	}

	CGINCLUDE
	#include "DySky.cginc"

	sampler2D _WaveTex;
	half4 _WaveTiling;
	half _WaveTilingScale;
	half4 _WaveSpeed;
	half _WaveStrength;
	half _SpecularPower;
	half _FresnelPower;
	half _FresnelScale;
	half4 _BaseColor;
	half4 _SpecularColor;
	half4 _FresnelColor;

	sampler2D _DySkyGrabTexture;
	half _DistortionStrength;

	UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
	half _EdgeInvFade;
	sampler2D _EdgeFoamTex;
	half _EdgeFoamFreq;

	struct appdata_t
	{
		float4 vertex		: POSITION;
	};

	struct v2f
	{
		float4 vertex		: POSITION;
		half4 uv			: TEXCOORD0;
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

		half3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

		o.uv = (worldPos.xzxz + _Time.xxxx * _WaveSpeed) * _WaveTiling * _WaveTilingScale;
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

	fixed4 frag(v2f i) : COLOR
	{
		half3 viewDir = -normalize(i.viewDir);

		half3 normal = half3(0, 1, 0);
		half3 fbm = UnpackNormal(tex2D(_WaveTex, i.uv.xy)) + UnpackNormal(tex2D(_WaveTex, i.uv.zw));
		normal.x += fbm.x * _WaveStrength;
		normal.z += fbm.y * _WaveStrength;
		normal = normalize(normal);

#ifdef DY_SKY_GRAB_PASS_ENABLE
		half4 grabPos = UNITY_PROJ_COORD(i.grabPos);
		grabPos.xy += normal.xz * _DistortionStrength;
		half4 grabColor = tex2Dproj(_DySkyGrabTexture, grabPos);
		half4 albedo = lerp(grabColor, _BaseColor, _BaseColor.a - 0.2);
#else
		half4 albedo = _BaseColor;
#endif

		half3 h = normalize(_WorldSpaceLightPos0.xyz + viewDir);
		half ndoth = max(0, dot(normal, h));
		half specular = pow(ndoth, _SpecularPower);

		half ndotv = max(0, dot(normal * half3(_FresnelScale, 1, _FresnelScale), viewDir));
		half fresnel = pow(1.0 - ndotv, _FresnelPower);

		half4 col = _SpecularColor * specular * _LightColor0;
		col += lerp(albedo, _FresnelColor, fresnel);

#ifdef DY_SKY_SOFT_EDGE_ENABLE
		half sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
		half partZ = i.projPos.z;
		half fade = saturate(_EdgeInvFade * (sceneZ - partZ));
		col.a *= fade;

	#ifdef DY_SKY_FOAM_EDGE_ENABLE
		half foam = tex2D(_EdgeFoamTex, half2(pow(fade, _EdgeFoamFreq) + _Time.y * 0.2, 0.5) + normal.xy * 0.15).r;
		foam *= 1.0 - fade;
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
		LOD 400

		GrabPass{ "_DySkyGrabTexture" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile __ DY_SKY_FOG_ENABLE
			#pragma multi_compile DY_SKY_GRAB_PASS_ENABLE
			#pragma multi_compile DY_SKY_SOFT_EDGE_ENABLE
			#pragma shader_feature DY_SKY_FOAM_EDGE_ENABLE

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
		LOD 300

		GrabPass{ "_DySkyGrabTexture" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile __ DY_SKY_FOG_ENABLE
			#pragma multi_compile DY_SKY_GRAB_PASS_ENABLE

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
