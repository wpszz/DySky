﻿#ifndef DY_SKY_INCLUDED
#define DY_SKY_INCLUDED

#include "UnityCG.cginc"

uniform half		_DySky_kRayleigh;
uniform half		_DySky_kMie;
uniform half		_DySky_tScattering;
uniform half		_DySky_tRayleigh;
uniform half		_DySky_tMie;
uniform half3		_DySky_cRayleigh;
uniform half3		_DySky_cMie;
uniform half		_DySky_tNightIntensity;
uniform half		_DySky_tMoonEmissionIntensity;
uniform half		_DySky_tMoonBrightRange;
uniform half3		_DySky_cMoonEmission;
uniform half3		_DySky_cMoonBright;
uniform half4		_DySky_unionMoonPhaseSize;		// xy:phase z:size w:phase type

uniform sampler2D	_DySky_texSun;
uniform sampler2D	_DySky_texMoon;
uniform sampler2D	_DySky_texCloudNoise;
uniform samplerCUBE _DySky_texStarfield;

uniform float4x4	_DySky_mSunSpace;
uniform half		_DySky_tSunSize;
uniform half3		_DySky_cSunEmission;

uniform half4		_DySky_unionCloudHeightDirSpeed; // x:height y:sin(dir) z:cos(dir) w:speed
uniform half		_DySky_tCloudDensity;
uniform half3		_DySky_cCloudMainTint;
uniform half3		_DySky_cCloudSecondaryTint;

uniform float4x4	_DySky_mStarfieldSpace;
uniform half		_DySky_tStarfieldIntensity;
uniform half		_DySky_tGalaxyIntensity;

uniform half		_DySky_tExposure;

uniform float4x4	_DySky_mMVP;

uniform samplerCUBE _DySky_texFogCubemap;
uniform half4       _DySky_unionHeightFogParams;  // x:start y:1/dis z:density w:unused
uniform half4       _DySky_unionDisFogParams;     // x:start y:1/dis z:density w:unused


#define __DySky_RayleighWavelength half3(0.00519673, 0.0121427, 0.0296453) * _DySky_tRayleigh
#define __DySky_MieWavelength half3(0.005721017, 0.004451339, 0.003146905) * _DySky_tMie
#define __DySky_MiePhaseFunc half3(0.4375, 1.5625, 1.5)
#define __DySky_Scattering 60.0 * _DySky_tScattering


#define DY_SKY_FOG_POS(idx) half4 dySkyFogPos : TEXCOORD##idx;
#define DY_SKY_FOG_VERT(v, o) o.dySkyFogPos = ApplyDySkyFogVert(v.vertex);
#define DY_SKY_FOG_FRAG(IN, c) c.rgb = ApplyDySkyFogFrag(c, IN.dySkyFogPos);

inline half4 ApplyDySkyFogVert(float4 vertex)
{
    half4 o = 0;   
    o.xyz = mul(unity_ObjectToWorld, float4(vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos.xyz;
    o.w = -UnityObjectToViewPos(vertex).z;
    return o;
}

inline half3 ApplyDySkyFogFrag(half3 col, half4 dySkyFogPos)
{
    half disFog = saturate((dySkyFogPos.w - _DySky_unionDisFogParams.x) * _DySky_unionDisFogParams.y);
    disFog = 1.0 - disFog;
    disFog *= disFog;
    disFog = 1.0 - disFog;

    half heightFog = saturate((-dySkyFogPos.y - _DySky_unionHeightFogParams.x) * _DySky_unionHeightFogParams.y);
    heightFog = 1.0 - heightFog;
    heightFog *= heightFog;
    heightFog = 1.0 - heightFog;

    half3 fogCol = texCUBE(_DySky_texFogCubemap, normalize(dySkyFogPos.xyz)).rgb;
    
    half fogFactor = saturate(disFog * _DySky_unionDisFogParams.z + heightFog * _DySky_unionHeightFogParams.z);
    return lerp(col, fogCol, fogFactor);
}

//--------------------------------
inline float4 DepthToWorld(float depth, float2 uv, float4x4 inverseViewMatrix)
{
	float viewDepth = LinearEyeDepth(depth);
	float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
	float3 vpos = float3((uv * 2 - 1) / p11_22, -1) * viewDepth;
	float4 wpos = mul(inverseViewMatrix, float4(vpos, 1));
	return wpos;
}

#endif
