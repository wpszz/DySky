Shader "DySky/Opaque/MatCap"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_MatCap("MatCap Texture", 2D) = "white" {}
		_MatCapFactor("MatCap Factor", Range(0,5)) = 2
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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _MatCap;
			half _MatCapFactor;

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				float4 uvuv : TEXCOORD0;

				DY_SKY_FOG_POS(1)
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvuv.xy = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvuv.zw = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal)).xy;
				//o.uvuv.z = dot(normalize(UNITY_MATRIX_MV[0].xyz), normalize(v.normal));
				//o.uvuv.w = dot(normalize(UNITY_MATRIX_MV[1].xyz), normalize(v.normal));
				o.uvuv.zw = o.uvuv.zw * 0.5 + 0.5;

				DY_SKY_FOG_VERT(v, o)
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				fixed4 col = tex2D(_MainTex, i.uvuv.xy);
				fixed4 matCap = tex2D(_MatCap, i.uvuv.zw);
				col.rgb *= matCap.rgb * _MatCapFactor;

				DY_SKY_FOG_FRAG(i, col)
				return col;
			}

			ENDCG
		}
	}
}
