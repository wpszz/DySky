using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(1001)]
[RequireComponent(typeof(DySkyController))]
public class DySkyFogController : MonoBehaviour
{
    public enum BakeType
    {
        Static = 0,
        EveryFrame = 1,        
        EverySecond = 2,
    }

    public Material matFogBake;
    public Cubemap staticFogCubemap;

    [Space(10)]
    public BakeType bakeType = BakeType.Static;

    [Range(-50f, 50f)]
    public float heightFogStart = 5f;
    [Range(1f, 100f)]
    public float heightFogDistance = 20f;
    [Range(0f, 2f)]
    public float heightFogDensity = 1f;

    [Range(-50f, 50f)]
    public float disFogStart = 5f;
    [Range(1f, 500f)]
    public float disFogDistance = 20f;
    [Range(0f, 2f)]
    public float disFogDensity = 1f;

    public bool mipmap = false;

    DySkyController skyController;
    Cubemap dynamicFogCubemap;

    float prevBakeTime = -1;

    static class Uniforms
    {
        internal static readonly int _DySky_texFogCubemap           = Shader.PropertyToID("_DySky_texFogCubemap");

#if __DY_SKY_FOG_FORMULA_OPTIMIZE_QUADRATIC
        internal static readonly int _DySky_unionFogCoef            = Shader.PropertyToID("_DySky_unionFogCoef");
        internal static readonly int _DySky_unionFogConstant        = Shader.PropertyToID("_DySky_unionFogConstant");
#else
        internal static readonly int _DySky_unionHeightFogParams    = Shader.PropertyToID("_DySky_unionHeightFogParams");
        internal static readonly int _DySky_unionDisFogParams       = Shader.PropertyToID("_DySky_unionDisFogParams");
#endif
    }

    private void Awake()
    {
        skyController = this.GetComponent<DySkyController>();
    }

    private void OnDestroy()
    {
        if (dynamicFogCubemap)
            GameObject.DestroyImmediate(dynamicFogCubemap);
    }

    private void OnEnable()
    {
        Shader.EnableKeyword("DY_SKY_FOG_ENABLE");
    }

    private void OnDisable()
    {
        Shader.DisableKeyword("DY_SKY_FOG_ENABLE");
    }

    private void Start()
    {
        if (bakeType == BakeType.Static)
            Shader.SetGlobalTexture(Uniforms._DySky_texFogCubemap, staticFogCubemap);
    }

    private void LateUpdate()
    {
        if (bakeType != BakeType.Static)
        {
            if (bakeType == BakeType.EveryFrame)
            {
                BakeFogCubeMap();
            }
            else if (bakeType == BakeType.EverySecond && Time.time - prevBakeTime > 1.0f)
            {
                prevBakeTime = Time.time;
                BakeFogCubeMap();
            }
        }
#if __DY_SKY_FOG_FORMULA_OPTIMIZE_QUADRATIC
        float a1 = heightFogStart;
        float b1 = 1f / heightFogDistance;
        float c1 = a1 * b1 + 1.0f;
        float d1 = heightFogDensity;
        float a2 = disFogStart;
        float b2 = 1f / disFogDistance;
        float c2 = a2 * b2 + 1.0f;
        float d2 = disFogDensity;
        Shader.SetGlobalVector(Uniforms._DySky_unionFogCoef, new Vector4(-b1 * b1 * d1, -2.0f * b1 * c1 * d1, -b2 * b2 * d2, 2.0f * b2 * c2 * d2));
        Shader.SetGlobalFloat(Uniforms._DySky_unionFogConstant, (1.0f - c1 * c1) * d1 + (1.0f - c2 * c2) * d2);
#else
        Shader.SetGlobalVector(Uniforms._DySky_unionHeightFogParams, new Vector4(heightFogStart, 1f / heightFogDistance, heightFogDensity, 0f));
        Shader.SetGlobalVector(Uniforms._DySky_unionDisFogParams, new Vector4(disFogStart, 1f / disFogDistance, disFogDensity, 0f));
#endif
    }

    public void BakeFogCubeMap()
    {
        if (!dynamicFogCubemap)
        {
            dynamicFogCubemap = new Cubemap(128, TextureFormat.ARGB32, mipmap);
            Shader.SetGlobalTexture(Uniforms._DySky_texFogCubemap, dynamicFogCubemap);
        }
        BakeFogCubeMap(dynamicFogCubemap);
    }

    public bool BakeFogCubeMap(Cubemap cubemap)
    {
        if (!matFogBake) return false;
        if (!cubemap) return false;
        if (!Camera.main) return false;

        const int layer_temp = 31;

        Transform transSkydome = skyController.transSkydome;
        MeshRenderer skydomeMR = transSkydome.GetComponent<MeshRenderer>();

        int oldLayer = transSkydome.gameObject.layer;
        Material oldMat = skydomeMR.sharedMaterial;

        Camera bakeCamera = new GameObject("DySkyBakeFogCamera").AddComponent<Camera>();
        bakeCamera.CopyFrom(Camera.main);
        bakeCamera.enabled = false;
        bakeCamera.depthTextureMode = DepthTextureMode.None;
        bakeCamera.cullingMask = 1 << layer_temp;

        transSkydome.gameObject.layer = layer_temp;
        skydomeMR.sharedMaterial = matFogBake;

        bool success = false;
        try
        {
            bakeCamera.RenderToCubemap(cubemap);
            success = true;
        }
        catch(System.Exception e)
        {
            Debug.LogError(e);
        }
        skydomeMR.sharedMaterial = oldMat;
        transSkydome.gameObject.layer = oldLayer;

        GameObject.DestroyImmediate(bakeCamera.gameObject);
        return success;
    }

    public Cubemap GetCurrentCubeMap()
    {
        return dynamicFogCubemap ? dynamicFogCubemap : staticFogCubemap;
    }

    /*
    public RenderTexture BakeFogRT(int size = 256)
    {
        RenderTexture rt = new RenderTexture(new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGB32, 0));


        return rt;
    }
    */
}
