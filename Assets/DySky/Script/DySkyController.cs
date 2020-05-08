using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(1000)]
public class DySkyController : MonoBehaviour
{
    public Transform transSkydome;
    public DySkyProfile profile;
    public Texture2D texSun;
    public Texture2D texMoon;
    public Texture2D texCloudNoise;
    public Cubemap texStarfield;

    [Space(10)]
    [Tooltip("Day-Night cycle timeline")]
    [Range(0.0f, 24.0f)]
    public float timeline = 8.0f;

    [Tooltip("East-West longitude")]
    [Range(-180f, 180f)]
    public float longitude = 0.0f;
    [Tooltip("North-South latitude")]
    [Range(-90f, 90f)]
    public float latitude = 0.0f;

    [Space(10)]
    [Range(-0.5f, 0.5f)]
    public float moonPhaseX = 0.18f;
    [Range(-0.5f, 0.5f)]
    public float moonPhaseY = -0.1f;
    [Range(0.0f, 1.0f)]
    public float moonPhaseW = 0.0f;

    [Space(10)]
    [Range(-180f, 180f)]
    public float galaxyX = 45.0f;
    [Range(-180f, 180f)]
    public float galaxyY = -.30f;
    [Range(-180f, 180f)]
    public float galaxyZ = 15.0f;

    [Space(10)]
    [Range(1f, 179f)]
    public float fov = 60.0f;

    const float Inv24 = 1f / 24f;

    static class Uniforms
    {
        internal static readonly int _DySky_kRayleigh               = Shader.PropertyToID("_DySky_kRayleigh");
        internal static readonly int _DySky_kMie                    = Shader.PropertyToID("_DySky_kMie");
        internal static readonly int _DySky_tScattering             = Shader.PropertyToID("_DySky_tScattering");
        internal static readonly int _DySky_tRayleigh               = Shader.PropertyToID("_DySky_tRayleigh");
        internal static readonly int _DySky_tMie                    = Shader.PropertyToID("_DySky_tMie");
        internal static readonly int _DySky_cRayleigh               = Shader.PropertyToID("_DySky_cRayleigh");
        internal static readonly int _DySky_cMie                    = Shader.PropertyToID("_DySky_cMie");
        internal static readonly int _DySky_tNightIntensity         = Shader.PropertyToID("_DySky_tNightIntensity");
        internal static readonly int _DySky_tMoonEmissionIntensity  = Shader.PropertyToID("_DySky_tMoonEmissionIntensity");
        internal static readonly int _DySky_tMoonBrightRange        = Shader.PropertyToID("_DySky_tMoonBrightRange");
        internal static readonly int _DySky_cMoonEmission           = Shader.PropertyToID("_DySky_cMoonEmission");
        internal static readonly int _DySky_cMoonBright             = Shader.PropertyToID("_DySky_cMoonBright");
        internal static readonly int _DySky_unionMoonPhaseSize      = Shader.PropertyToID("_DySky_unionMoonPhaseSize");

        internal static readonly int _DySky_texSun                  = Shader.PropertyToID("_DySky_texSun");
        internal static readonly int _DySky_texMoon                 = Shader.PropertyToID("_DySky_texMoon");
        internal static readonly int _DySky_texCloudNoise           = Shader.PropertyToID("_DySky_texCloudNoise");
        internal static readonly int _DySky_texStarfield            = Shader.PropertyToID("_DySky_texStarfield");

        internal static readonly int _DySky_mSunSpace               = Shader.PropertyToID("_DySky_mSunSpace");
        internal static readonly int _DySky_tSunSize                = Shader.PropertyToID("_DySky_tSunSize");
        internal static readonly int _DySky_cSunEmission            = Shader.PropertyToID("_DySky_cSunEmission");

        internal static readonly int _DySky_unionCloudHeightDirSpeed= Shader.PropertyToID("_DySky_unionCloudHeightDirSpeed");
        internal static readonly int _DySky_tCloudDensity           = Shader.PropertyToID("_DySky_tCloudDensity");
        internal static readonly int _DySky_cCloudMainTint          = Shader.PropertyToID("_DySky_cCloudMainTint");
        internal static readonly int _DySky_cCloudSecondaryTint     = Shader.PropertyToID("_DySky_cCloudSecondaryTint");

        internal static readonly int _DySky_mStarfieldSpace         = Shader.PropertyToID("_DySky_mStarfieldSpace");
        internal static readonly int _DySky_tStarfieldIntensity     = Shader.PropertyToID("_DySky_tStarfieldIntensity");
        internal static readonly int _DySky_tGalaxyIntensity        = Shader.PropertyToID("_DySky_tGalaxyIntensity");

        internal static readonly int _DySky_tExposure               = Shader.PropertyToID("_DySky_tExposure");

        internal static readonly int _DySky_mMVP                    = Shader.PropertyToID("_DySky_mMVP");
    }

    private void Awake()
    {
        if (!transSkydome || !profile)
        {
            this.enabled = false;
            return;
        }
    }

    private void Start()
    {
        InitDySkyUniforms();
    }

    private void InitDySkyUniforms()
    {
        Shader.SetGlobalTexture(Uniforms._DySky_texSun, texSun);
        Shader.SetGlobalTexture(Uniforms._DySky_texMoon, texMoon);
        Shader.SetGlobalTexture(Uniforms._DySky_texCloudNoise, texCloudNoise);
        Shader.SetGlobalTexture(Uniforms._DySky_texStarfield, texStarfield);

    }

    private void LateUpdate()
    {
        float progress = timeline * Inv24;

        UpdateSkydomePos();
        UpdateDySkyUniforms(progress, timeline);
        UpdateEnvironmentLighting(progress, timeline);
    }

    private void UpdateSkydomePos()
    {
        if (Camera.main)
            this.transform.position = Camera.main.transform.position;
        transSkydome.localPosition = Vector3.zero;
        transSkydome.localRotation = Quaternion.identity;
        //transSkydome.localScale = Vector3.one;
    }

    private void UpdateDySkyUniforms(float progress01, float timeline24)
    {
        Shader.SetGlobalFloat(Uniforms._DySky_kRayleigh,                profile.kRayleigh);
        Shader.SetGlobalFloat(Uniforms._DySky_kMie,                     profile.kMie);
        Shader.SetGlobalFloat(Uniforms._DySky_tScattering,              profile.curveScattering.Evaluate(timeline24));
        Shader.SetGlobalFloat(Uniforms._DySky_tRayleigh,                profile.curveRayleigh.Evaluate(timeline24));
        Shader.SetGlobalFloat(Uniforms._DySky_tMie,                     profile.curveMie.Evaluate(timeline24));
        Shader.SetGlobalColor(Uniforms._DySky_cRayleigh,                profile.gradRayleigh.Evaluate(progress01));
        Shader.SetGlobalColor(Uniforms._DySky_cMie,                     profile.gradMie.Evaluate(progress01));
        Shader.SetGlobalFloat(Uniforms._DySky_tNightIntensity,          profile.curveNightIntensity.Evaluate(timeline24));
        Shader.SetGlobalFloat(Uniforms._DySky_tMoonEmissionIntensity,   profile.curveMoonEmissionIntensity.Evaluate(timeline24));
        Shader.SetGlobalFloat(Uniforms._DySky_tMoonBrightRange,         profile.curveMoonBrightRange.Evaluate(timeline24));
        Shader.SetGlobalColor(Uniforms._DySky_cMoonEmission,            profile.gradMoonEmission.Evaluate(progress01));
        Shader.SetGlobalColor(Uniforms._DySky_cMoonBright,              profile.gradMoonBright.Evaluate(progress01));
        Shader.SetGlobalVector(Uniforms._DySky_unionMoonPhaseSize,      new Vector4(moonPhaseX, moonPhaseY, profile.curveMoonSize.Evaluate(timeline24), moonPhaseW));

        // +Z-Axis point to the East
        float sunPhase = progress01 * 360.0f - 90.0f;
        Shader.SetGlobalMatrix(Uniforms._DySky_mSunSpace,               Matrix4x4.Rotate(Quaternion.Euler(0.0f, longitude, latitude) * Quaternion.Euler(sunPhase, 180.0f, 0.0f)));
        Shader.SetGlobalFloat(Uniforms._DySky_tSunSize,                 profile.curveSunSize.Evaluate(timeline24));
        Shader.SetGlobalColor(Uniforms._DySky_cSunEmission,             profile.gradSunEmission.Evaluate(progress01) * profile.curveSunEmissionIntensity.Evaluate(timeline24));

        float cloudDir = profile.curveCloudDir.Evaluate(timeline24);
        Shader.SetGlobalVector(Uniforms._DySky_unionCloudHeightDirSpeed,
            new Vector4(profile.kCloudHeight, Mathf.Sin(cloudDir), Mathf.Cos(cloudDir), profile.curveCloudSpeed.Evaluate(timeline24)));
        Shader.SetGlobalFloat(Uniforms._DySky_tCloudDensity,            Mathf.Lerp(25.0f, 0.0f, profile.curveCloudDensity.Evaluate(timeline24)));
        Color cCloudMainTint = profile.gradCloudMainTint.Evaluate(progress01);
        Color cCloudSecondaryTint = profile.gradCloudSecondaryTint.Evaluate(progress01);
        Shader.SetGlobalColor(Uniforms._DySky_cCloudMainTint,           QualitySettings.activeColorSpace == ColorSpace.Gamma ? cCloudMainTint : cCloudMainTint.linear);
        Shader.SetGlobalColor(Uniforms._DySky_cCloudSecondaryTint,      QualitySettings.activeColorSpace == ColorSpace.Gamma ? cCloudSecondaryTint : cCloudSecondaryTint.linear);

        Shader.SetGlobalMatrix(Uniforms._DySky_mStarfieldSpace,         Matrix4x4.Rotate(Quaternion.Euler(galaxyX, galaxyY, galaxyZ)));
        Shader.SetGlobalFloat(Uniforms._DySky_tStarfieldIntensity,      profile.curveStarfieldIntensity.Evaluate(timeline24));
        Shader.SetGlobalFloat(Uniforms._DySky_tGalaxyIntensity,         profile.curveGalaxyIntensity.Evaluate(timeline24));

        Shader.SetGlobalFloat(Uniforms._DySky_tExposure,                profile.curveExposure.Evaluate(timeline24));

        if (Camera.main)
        {
            Matrix4x4 M = transSkydome.localToWorldMatrix;
            Matrix4x4 V = Camera.main.worldToCameraMatrix;
            Matrix4x4 P = Matrix4x4.Perspective(fov, Camera.main.aspect, 0.01f, 1000f);
            // fixed for DX engine
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                P = GL.GetGPUProjectionMatrix(P, true);
            Shader.SetGlobalMatrix(Uniforms._DySky_mMVP, P * V * M);
        }
    }

    private void UpdateEnvironmentLighting(float progress01, float timeline24)
    {
        RenderSettings.ambientIntensity = profile.curveEnvAmbientIntensity.Evaluate(timeline24);
        RenderSettings.ambientSkyColor = profile.gradEnvAmbient.Evaluate(progress01);
    }
}
