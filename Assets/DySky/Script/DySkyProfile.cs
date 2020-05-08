using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Profile", menuName = "Dynamic Skybox/New Profile", order = 1)]
public class DySkyProfile : ScriptableObject
{
    // constant
    // ====================================
    public float            kRayleigh                   = 8.4f;
    public float            kMie                        = 1.25f;

    // variable by timeline(0 - 24)
    // ====================================
    public AnimationCurve   curveScattering             = Linear24(0.25f);
    public AnimationCurve   curveRayleigh               = Linear24(1.25f);
    public AnimationCurve   curveMie                    = Linear24(5.0f);
    public Gradient         gradRayleigh                = Blend24(0.25f, 0.5f, 1.0f);
    public Gradient         gradMie                     = Blend24(1.0f, 0.7f, 0.52f);

    [Space(10)]
    public AnimationCurve   curveNightIntensity         = Linear24(1.5f);
    public AnimationCurve   curveMoonEmissionIntensity  = Linear24(10.0f);
    public AnimationCurve   curveMoonBrightRange        = Linear24(10.0f);
    public Gradient         gradMoonEmission            = Blend24(1.0f, 1.0f, 1.0f);
    public Gradient         gradMoonBright              = Blend24(0.06f, 0.18f, 0.46f);
    public AnimationCurve   curveMoonSize               = Linear24(6.0f);

    [Space(10)]
    public AnimationCurve   curveSunSize                = Linear24(3.0f);
    public Gradient         gradSunEmission             = Blend24(1.0f, 1.0f, 1.0f);
    public AnimationCurve   curveSunEmissionIntensity   = Linear24(3.0f);

    [Space(10)]
    [Range(0f, 20f)]
    public float            kCloudHeight                = 7.5f;
    public AnimationCurve   curveCloudDir               = Linear24(1.0f);
    public AnimationCurve   curveCloudSpeed             = Linear24(0.1f);
    public AnimationCurve   curveCloudDensity           = Linear24(0.5f);
    public Gradient         gradCloudMainTint           = Blend24(1.0f, 1.0f, 1.0f);
    public Gradient         gradCloudSecondaryTint      = Blend24(0.0f, 0.04f, 0.28f);

    [Space(10)]
    public AnimationCurve   curveStarfieldIntensity     = Linear24(1.0f);
    public AnimationCurve   curveGalaxyIntensity        = Linear24(0.5f);

    [Space(10)]
    public AnimationCurve   curveExposure               = Linear24(1.5f);

    [Space(10)]
    public Gradient         gradEnvAmbient              = Blend24(0.5f, 0.5f, 0.5f);
    public AnimationCurve   curveEnvAmbientIntensity    = Linear24(1.0f);

    // ====================================
    private static AnimationCurve Linear24(float initVal)
    {
        return AnimationCurve.Linear(0.0f, initVal, 24.0f, initVal);
    }

    private static Gradient Blend24(float initR, float initG, float initB)
    {
        Gradient grad = new Gradient();
        var colors = grad.colorKeys;
        colors[0].color = new Color(initR, initG, initB);
        colors[1].color = new Color(initR, initG, initB);
        grad.colorKeys = colors;
        return grad;
    }
}
