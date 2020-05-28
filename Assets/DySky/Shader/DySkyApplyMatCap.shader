Shader "DySky/Opaque/MatCap"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		[NoScaleOffset]
		_MatCap("MatCap (RGB)", 2D) = "white" {}
		[NoScaleOffset]
		_MaskTexture("Mask (R)", 2D) = "white" {}
		_MatCapFactor("Factor", Range(0,5)) = 2
	}

	CGINCLUDE
	#include "DySky.cginc"
	ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
		Cull Back

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile __ DY_SKY_FOG_ENABLE
			#pragma shader_feature DY_SKY_MATCAP_BASE DY_SKY_MATCAP_MASK DY_SKY_MATCAP_MASK_BLEND

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _MatCap;
			half _MatCapFactor;
			sampler2D _MaskTexture;

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				float4 uv : TEXCOORD0;

				DY_SKY_FOG_POS(1)
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);

				float3 normalWorld = mul((float3x3)unity_ObjectToWorld, v.normal);
				float3 normalView = mul((float3x3)UNITY_MATRIX_V, normalWorld);
				o.uv.zw = normalView.xy * 0.5 + 0.5;

				DY_SKY_FOG_VERT(v, o)
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				fixed4 col = tex2D(_MainTex, i.uv.xy);
				fixed4 matCap = tex2D(_MatCap, i.uv.zw);
#if DY_SKY_MATCAP_BASE
				col.rgb *= matCap.rgb * _MatCapFactor;
#elif DY_SKY_MATCAP_MASK
				fixed4 mask = tex2D(_MaskTexture, i.uv.xy);
				col.rgb = lerp(col.rgb, col.rgb * matCap.rgb * _MatCapFactor, mask.r);
#elif DY_SKY_MATCAP_MASK_BLEND
				fixed4 mask = tex2D(_MaskTexture, i.uv.xy);
				col.rgb = col.rgb * _MatCapFactor * lerp(matCap.g, matCap.r, mask.r);
#endif
				DY_SKY_FOG_FRAG(i, col)
				return col;
			}

			ENDCG
		}
	}
	CustomEditor "DySkyShaderMatCapEditor"
}
