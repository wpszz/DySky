Shader "DySky/Fog/BakeVert" {
	Properties {

	}

	CGINCLUDE
	#include "DySky.cginc"
	ENDCG

	SubShader {
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Front 
		ZWrite Off
		Fog{Mode Off}

		Pass {

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata_t {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 pos	  		: SV_POSITION;
				half4 skyColor		: TEXCOORD0;
			};

			v2f vert (appdata_t v) {
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);

				OUT.pos = UnityObjectToClipPos(v.vertex);
				//OUT.pos = mul(_DySky_mMVP, float4(v.vertex.xyz, 1.0));

				//half3 viewDir = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
				half3 viewDir	= v.vertex.xyz;

				float3x3 mSun = (float3x3)_DySky_mSunSpace;

				//half3 sunDir = mul(mSun, half3(0, 0, 1));
				half3 sunDir = half3(mSun._13, mSun._23, mSun._33);
				half3 sunDirInv = -sunDir;
				half sunCosTheta = dot(viewDir, sunDirInv);
				half sunrise = saturate(sunDirInv.y * 10.0);
				half sunset = clamp(sunDirInv.y, 0.0, 0.5);

				half3 moonDirInv = -sunDirInv;
				half moonCosTheta = dot(viewDir, moonDirInv);
				half moonrise = saturate(moonDirInv.y * 10.0);

				//Optical Depth.
				//--------------------------------
#if 0
				half zenith = acos(saturate(dot(half3(0.0, 1.0, 0.0), viewDir)));
				half z      = cos(zenith) + 0.15 * pow(93.885 - ((zenith * 180.0) / UNITY_PI), -1.253);
#else
				half z		= max(0, viewDir.y) + 1e-5;
#endif
				half SR     = _DySky_kRayleigh / z;
				half SM     = _DySky_kMie / z;

				//Total Extinction.
				//--------------------------------
				half3 fex = exp(-(__DySky_RayleighWavelength*SR + __DySky_MieWavelength*SM));
				half3 extinction = lerp(fex, (1.0 - fex), sunset);

				//Scattering.
				//--------------------------------
				half  rayPhase = 2.0 + 0.5 * pow(sunCosTheta, 2.0);									 	   //Rayleigh phase function based on the Nielsen's paper.
				half  miePhase = __DySky_MiePhaseFunc.x / pow(__DySky_MiePhaseFunc.y - __DySky_MiePhaseFunc.z * sunCosTheta, 1.5); //The Henyey-Greenstein phase function.

				half3 BrTheta  = (3.0 / (16.0 * UNITY_PI)) * __DySky_RayleighWavelength * rayPhase * _DySky_cRayleigh * extinction;
				half3 BmTheta  = (1.0 / (4.0 * UNITY_PI)) * __DySky_MieWavelength * miePhase * _DySky_cMie * extinction * sunrise;
				half3 BrmTheta = (BrTheta + BmTheta) / (__DySky_RayleighWavelength + __DySky_MieWavelength);

				half3 inScatter = BrmTheta * __DySky_Scattering * (1.0 - fex);
				inScatter *= sunrise;

				//Night Sky.
				//--------------------------------
				BrTheta  = (3.0 / (16.0 * UNITY_PI)) * __DySky_RayleighWavelength * rayPhase * _DySky_cRayleigh;
				BrmTheta = (BrTheta) / (__DySky_RayleighWavelength + __DySky_MieWavelength);
				half3 nightSky = BrmTheta * _DySky_tNightIntensity * (1.0 - fex);

				//Moon Bright.
				//--------------------------------
				half  bright      = 1.0 - moonCosTheta;
				half3 moonBright  = 1.0 / (1.0  + bright * _DySky_tMoonBrightRange) * _DySky_cMoonBright;
				moonBright += 1.0 / (_DySky_tMoonEmissionIntensity + bright * 200.0) * _DySky_cMoonEmission;
				moonBright *= moonrise;

				//Fade out skyline
				half horizonExtinction = saturate(viewDir.y * 1000.0) * fex.b;

				OUT.skyColor = half4(inScatter + nightSky + moonBright, horizonExtinction);

				return OUT;
			}

			half4 frag (v2f IN) : SV_Target {
				half3 col = IN.skyColor.rgb;

				//Tonemapping.

				//Exposure.
				col = saturate(1.0 - exp(-_DySky_tExposure * col));

				//Color Correction.
#ifndef UNITY_COLORSPACE_GAMMA
				col = pow(col, 2.2);
#endif
				return half4(col, 1.0);
			}
			ENDCG
		}
	}

	Fallback Off
}
