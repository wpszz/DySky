#ifndef DY_SKY_INCLUDED
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

#define __DySky_RayleighWavelength half3(0.00519673, 0.0121427, 0.0296453) * _DySky_tRayleigh
#define __DySky_MieWavelength half3(0.005721017, 0.004451339, 0.003146905) * _DySky_tMie
#define __DySky_MiePhaseFunc half3(0.4375, 1.5625, 1.5)
#define __DySky_Scattering 60.0 * _DySky_tScattering

#endif
