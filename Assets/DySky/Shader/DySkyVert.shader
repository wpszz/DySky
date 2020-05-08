Shader "DySky/Vert" {
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
				half3 attens		: TEXCOORD1;
				half4 sunMoonUV		: TEXCOORD2;
				half4 cloudUV		: TEXCOORD3;
				half4 starPos		: TEXCOORD4;
			};

			v2f vert (appdata_t v) {
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);

				//OUT.pos = UnityObjectToClipPos(v.vertex);
				OUT.pos = mul(_DySky_mMVP, float4(v.vertex.xyz, 1.0));

				//half3 viewDir = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
				half3 viewDir	= v.vertex.xyz;

				float3x3 mSun = (float3x3)_DySky_mSunSpace;
				half3 sunDir = -mSun[2];
				OUT.sunMoonUV.xy = mul(mSun, v.vertex.xyz).xy * _DySky_tSunSize + 0.5;
				half sunCosTheta = dot(viewDir, sunDir);
				half sunrise = saturate(sunDir.y * 10.0);
				half sunset = clamp(sunDir.y, 0.0, 0.5);

				half3 moonDir = -sunDir;
				float3x3 mMoon = float3x3(-mSun[0], mSun[1], mSun[2]);
				OUT.sunMoonUV.zw = mul(mMoon, v.vertex.xyz).xy * _DySky_unionMoonPhaseSize.z + 0.5;

				//Optical Depth.
				//--------------------------------
				half zenith = acos(saturate(dot(half3(0.0, 1.0, 0.0), viewDir)));
				half z      = cos(zenith) + 0.15 * pow(93.885 - ((zenith * 180.0) / UNITY_PI), -1.253);
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
				half  moonrise    = saturate(moonDir.y * 10.0);
				half  bright      = 1.0 + dot(viewDir, -moonDir);
				half3 moonBright  = 1.0 / (1.0  + bright * _DySky_tMoonBrightRange) * _DySky_cMoonBright;
				moonBright += 1.0 / (_DySky_tMoonEmissionIntensity  + bright * 200.0) * _DySky_cMoonEmission;
				moonBright  = moonBright * moonrise;

				//Dynamic Clouds.
				//--------------------------------
				half3 cloudPos = v.vertex.xyz;
				cloudPos.y *= _DySky_unionCloudHeightDirSpeed.x;
				half s = _DySky_unionCloudHeightDirSpeed.y;
				half c = _DySky_unionCloudHeightDirSpeed.z;
				cloudPos.xz = mul(float2x2(c, s, -s, c), cloudPos.xz);
				cloudPos = normalize(cloudPos);
				half2 cloudSpeed = _DySky_unionCloudHeightDirSpeed.w * _Time.x;
				cloudSpeed.x *= 0.05;
				OUT.cloudUV.xy = cloudPos.xz * 0.25 + cloudSpeed - 0.005;
				OUT.cloudUV.zw = cloudPos.xz * 0.35 + cloudSpeed - 0.0065;

				//fade out skyline
				half horizonExtinction = saturate(viewDir.y * 1000.0) * fex.b;

				OUT.starPos = half4(mul((float3x3)_DySky_mStarfieldSpace, viewDir), pow(sin(viewDir.x * 1000 + _Time.w) * 0.5 + 0.5, 2.5));
				OUT.skyColor = half4(inScatter + nightSky + moonBright, horizonExtinction);
				OUT.attens = half3(fex.b, sunCosTheta, cloudPos.y);
				return OUT;
			}

			half4 frag (v2f IN) : SV_Target {
				//Clouds.
				//--------------------------------
				half4 tex1 = tex2D(_DySky_texCloudNoise, IN.cloudUV.xy);
				half4 tex2 = tex2D(_DySky_texCloudNoise, IN.cloudUV.zw);
				half noise1 = pow(tex1.g + tex2.g, 0.1);
				half noise2 = pow(tex2.b * tex1.r, 0.25);
				half cloudAlpha = pow(noise1 * noise2, _DySky_tCloudDensity);
				half3 cloud1 = lerp(_DySky_cCloudSecondaryTint.rgb, half3(0.0, 0.0, 0.0), noise1);
				half3 cloud2 = lerp(_DySky_cCloudSecondaryTint.rgb, _DySky_cCloudMainTint.rgb, noise2) * 2.5;
				half3 cloud = lerp(cloud1, cloud2, noise1 * noise2);
				half mixCloud = saturate(pow(IN.attens.z, 5.0) * cloudAlpha);
				cloudAlpha = 1.0 - saturate(cloudAlpha);

				//Sun.
				//--------------------------------
				half4 sun = tex2D(_DySky_texSun, IN.sunMoonUV.xy);
				half3 sunCol = lerp(sun.r, sun.g, _SinTime.w * 0.5 + 0.5) * _DySky_cSunEmission;
				sunCol *= sunCol * IN.skyColor.rgb;
				sunCol *= IN.attens.x * saturate(IN.attens.y) * cloudAlpha;

				//Moon.
				//--------------------------------
				half4 moon = tex2D(_DySky_texMoon, IN.sunMoonUV.zw);
				half moonPhase = tex2D(_DySky_texMoon, IN.sunMoonUV.zw + _DySky_unionMoonPhaseSize.xy).b;
				moonPhase = lerp(1.0 - moonPhase, moonPhase, _DySky_unionMoonPhaseSize.w);
				half3 moonCol = moon.r * _DySky_cMoonEmission;
				moonCol *= IN.skyColor.a * saturate(-IN.attens.y) * cloudAlpha * moonPhase;

				//Starfield.
				//--------------------------------
				half4 starfield = texCUBE(_DySky_texStarfield, IN.starPos.xyz);
				half3 starCol = pow(starfield.rgb, 1.5) * _DySky_tGalaxyIntensity;
				starCol += min(1.0, pow(starfield.a, 2.5) * 1000.0) * IN.starPos.w * _DySky_tStarfieldIntensity;
				starCol *= IN.skyColor.a * cloudAlpha * (1.0 - moon.b);

				half3 col = IN.skyColor.rgb + sunCol + moonCol + starCol;

				//Tonemapping.

				//Exposure.
				col = saturate(1.0 - exp(-_DySky_tExposure * col));

				//Color Correction.
#ifndef UNITY_COLORSPACE_GAMMA
				col = pow(col, 2.2);
#endif

				//Apply Clouds.
				col = lerp(col, cloud, mixCloud);

				return half4(col, 1.0);
			}
			ENDCG
		}
	}

	Fallback Off
}
