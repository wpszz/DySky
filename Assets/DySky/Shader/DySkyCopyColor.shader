Shader "DySky/BlitCopy" {
	Properties {
		[NoScaleOffset]
		_MainTex("Texture", any) = "" {}
	}
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;

			struct appdata_t {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert(appdata_t v) {
				v2f o;
				o.vertex = float4(v.vertex.xy * 2 - 1, 0.0, 1.0);
				o.texcoord = v.vertex.xy;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				return tex2D(_MainTex, i.texcoord);
			}
			ENDCG

		}
	}
	Fallback Off
}