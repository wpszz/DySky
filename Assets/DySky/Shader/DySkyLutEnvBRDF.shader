Shader "DySky/Lut/EnvBRDF"
{
	Properties
	{
	}

	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag           
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float RadicalInverse_VdC(uint bits)
			{
				bits = (bits << 16) | (bits >> 16);
				bits = ((bits & 0x00ff00ff) << 8) | ((bits & 0xff00ff00) >> 8);
				bits = ((bits & 0x0f0f0f0f) << 4) | ((bits & 0xf0f0f0f0) >> 4);
				bits = ((bits & 0x33333333) << 2) | ((bits & 0xcccccccc) >> 2);
				bits = ((bits & 0x55555555) << 1) | ((bits & 0xaaaaaaaa) >> 1);
				return float(bits) * 2.3283064365386963e-10;
				// 0x100000000
			}

			float2 Hammersley(uint i, uint N)
			{
				return float2(float(i) / float(N), RadicalInverse_VdC(i));
			}

			float4 ImportanceSampleGGX(float2 u, float roughness)
			{
				float m = roughness * roughness;
				float m2 = m * m;

				float phi = 2 * UNITY_PI * u.x;
				float cosTheta = sqrt((1 - u.y) / (1 + (m2 - 1) * u.y));
				float sinTheta = sqrt(1 - cosTheta * cosTheta);

				float3 H;
				H.x = sinTheta * cos(phi);
				H.y = sinTheta * sin(phi);
				H.z = cosTheta;

				//float d = ( cosTheta * m2 - cosTheta ) * cosTheta + 1;
				//float D = m2 / ( UNITY_PI * d *d );
				//float PDF = D * cosTheta;
				//return float4( H, PDF );
				return float4(H, 1.0);
			}

			float3x3 GetTangentBasis(float3 tangentZ)
			{
				float3 up = abs(tangentZ.z) < 0.999 ? float3(0, 0, 1) : float3(1, 0, 0);
				float3 tangentX = normalize(cross(up, tangentZ));
				float3 tangentY = cross(tangentZ, tangentX);
				return float3x3(tangentX, tangentY, tangentZ);
			}

			float3 TangentToWorld(float3 vec, float3 tangentZ)
			{
				return mul(vec, GetTangentBasis(tangentZ));
			}

			inline float Vis_SmithJointApprox(float roughness, float nv, float nl)
			{
				float a = roughness * roughness;
				float Vis_SmithV = nl * (nv * (1 - a) + a);
				float Vis_SmithL = nv * (nl * (1 - a) + a);
				// Note: will generate NaNs with roughness = 0.  MinRoughness is used to prevent this
				return 0.5 / (Vis_SmithV + Vis_SmithL + 1e-5f);
			}

			float2 IntegrateBRDF(float NoV, float roughness)
			{
				const uint SAMPLE_COUNT = 1024u;
				float3 N = float3(0, 0, 1);
				float3 V;
				V.x = sqrt(1.0 - NoV * NoV);
				V.y = 0.0;
				V.z = NoV;
				float scale = 0;
				float bias = 0;

				for (uint i = 0; i < SAMPLE_COUNT; i++)
				{
					float2 Xi = Hammersley(i, SAMPLE_COUNT);
					float3 H = TangentToWorld(ImportanceSampleGGX(Xi, roughness).xyz, N);
					float3 L = 2 * dot(V, H) * H - V;

					float NoL = max(L.z, 0.0);
					float NoH = max(H.z, 0.0);
					float VoH = max(dot(V, H), 0.0);

					if (NoL > 0)
					{
						//1 / NumSample * \int[L * fr * (N.L) / pdf]  with pdf = D(H) * (N.H) / (4 * (V.H)) and fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
						float Vis = Vis_SmithJointApprox(roughness, NoV, NoL) * 4 * NoL * VoH / NoH;
						float Fc = pow(1.0 - VoH, 5);

						scale += (1.0 - Fc) * Vis;
						bias += Fc * Vis;
					}
				}

				scale /= float(SAMPLE_COUNT);
				bias /= float(SAMPLE_COUNT);

				return float2(scale, bias);
			}

			float4 frag(v2f i) : SV_Target
			{
				return float4(IntegrateBRDF(i.uv.x, i.uv.y), 0, 1);
			}
			ENDCG
		}
	}
}
