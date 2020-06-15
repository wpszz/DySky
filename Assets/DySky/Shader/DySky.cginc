#ifndef DY_SKY_INCLUDED
#define DY_SKY_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGlobalIllumination.cginc"

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
#ifdef __DY_SKY_FOG_FORMULA_OPTIMIZE_QUADRATIC
uniform half4       _DySky_unionFogCoef;          // x:coef1 y:coef2 z:coef3 w:coef4
uniform half        _DySky_unionFogConstant;      // constant
#else
uniform half4       _DySky_unionHeightFogParams;  // x:start y:1/dis z:density w:unused
uniform half4       _DySky_unionDisFogParams;     // x:start y:1/dis z:density w:unused
#endif

#define __DySky_RayleighWavelength half3(0.00519673, 0.0121427, 0.0296453) * _DySky_tRayleigh
#define __DySky_MieWavelength half3(0.005721017, 0.004451339, 0.003146905) * _DySky_tMie
#define __DySky_MiePhaseFunc half3(0.4375, 1.5625, 1.5)
#define __DySky_Scattering 60.0 * _DySky_tScattering

#ifdef DY_SKY_FOG_ENABLE
#define DY_SKY_FOG_POS(idx) half4 dySkyFogPos : TEXCOORD##idx;
#define DY_SKY_FOG_VERT(v, o) o.dySkyFogPos = ApplyDySkyFogVert(v.vertex);
#define DY_SKY_FOG_FRAG(IN, c) c.rgb = ApplyDySkyFogFrag(c.rgb, 1.0, IN.dySkyFogPos);
#define DY_SKY_FOG_FRAG_ALPHA(IN, c, a) c.rgb = ApplyDySkyFogFrag(c.rgb, a, IN.dySkyFogPos);
#else
#define DY_SKY_FOG_POS(idx)
#define DY_SKY_FOG_VERT(v, o)
#define DY_SKY_FOG_FRAG(IN, c)
#define DY_SKY_FOG_FRAG_ALPHA(IN, c, a)
#endif

inline half4 ApplyDySkyFogVert(float4 vertex)
{
    half4 o = 0;   
    o.xyz = mul(unity_ObjectToWorld, float4(vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos.xyz;
    o.w = -UnityObjectToViewPos(vertex).z;
    return o;
}

inline half3 ApplyDySkyFogFrag(half3 col, half alpha, half4 dySkyFogPos)
{
#ifdef __DY_SKY_FOG_FORMULA_OPTIMIZE_QUADRATIC   
    half3 fogCol = texCUBE(_DySky_texFogCubemap, dySkyFogPos.xyz).rgb;
    fogCol *= alpha;
        
    half fogFactor = dot(_DySky_unionFogCoef, half4(dySkyFogPos.y * dySkyFogPos.y, dySkyFogPos.y, dySkyFogPos.w * dySkyFogPos.w, dySkyFogPos.w));
    fogFactor += _DySky_unionFogConstant;
    
    return lerp(col, fogCol, saturate(fogFactor));
#else
    half heightFog = saturate((-dySkyFogPos.y - _DySky_unionHeightFogParams.x) * _DySky_unionHeightFogParams.y);
    heightFog = 1.0 - heightFog;
    heightFog *= heightFog;
    heightFog = 1.0 - heightFog;
    
    half disFog = saturate((dySkyFogPos.w - _DySky_unionDisFogParams.x) * _DySky_unionDisFogParams.y);
    disFog = 1.0 - disFog;
    disFog *= disFog;
    disFog = 1.0 - disFog;

    half3 fogCol = texCUBE(_DySky_texFogCubemap, dySkyFogPos.xyz).rgb;
    fogCol *= alpha;
    
    half fogFactor = saturate(disFog * _DySky_unionDisFogParams.z + heightFog * _DySky_unionHeightFogParams.z);
    return lerp(col, fogCol, fogFactor);
#endif    
}

// GI indirect diffuse color(from SH) + LightMap(from baking)
inline half3 DySkyGI(half atten, half3 worldPos, half3 normalWorld, half3 ambient, half2 lmap)
{
    half3 diffuse = half3(0, 0, 0);
    
#if UNITY_SHOULD_SAMPLE_SH
    diffuse += ShadeSHPerPixel(normalWorld, ambient, worldPos);
#endif

#if defined(LIGHTMAP_ON)
    // Baked lightmaps
    half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, lmap.xy);
    half3 bakedColor = DecodeLightmap(bakedColorTex);

    #ifdef DIRLIGHTMAP_COMBINED // directional lightmap, need add DIRLIGHTMAP_COMBINED to shader_feature or multi_compile
        fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, data.lightmapUV.xy);
        diffuse += DecodeDirectionalLightmap (bakedColor, bakedDirTex, normalWorld);
    #else // non-directional lightmap
        diffuse += bakedColor;
    #endif
    
    #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
        diffuse = SubtractMainLightWithRealtimeAttenuationFromLightmap(diffuse, atten, bakedColorTex, normalWorld);
    #else // simple shadow weakened
        atten =  1.0 - atten;
        atten *= atten;
        atten *= atten;
        atten = 1.0 - atten;
        diffuse *= atten;
    #endif
#endif
    return diffuse;
}

// GI indirect specular color(from IBL)
inline half3 DySkyGI_IndirectSpecular(half3 normalWorld, half3 worldViewDir, half roughness)
{
#ifdef _GLOSSYREFLECTIONS_OFF    
    // simple indirect specular
    return unity_IndirectSpecColor.rgb;
#else
    // ref: UnityGlobalIllumination.cginc and UnityImageBasedLighting.cginc
    half3 reflUVW = reflect(-worldViewDir, normalWorld);
    half perceptualRoughness = roughness * (1.7 - 0.7 * roughness);
    half mip = perceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflUVW, mip);
    return DecodeHDR(rgbm, unity_SpecCube0_HDR);
#endif    
}

// Force handles shadows in the depths of the GI function for performance reasons
#ifndef HANDLE_SHADOWS_BLENDING_IN_GI
    #define HANDLE_SHADOWS_BLENDING_IN_GI 1
#endif

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
