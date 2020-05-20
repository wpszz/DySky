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

    DySkyController skyController;
    Cubemap dynamicFogCubemap;

    float prevBakeTime = -1;

    static class Uniforms
    {
        internal static readonly int _DySky_texFogCubemap           = Shader.PropertyToID("_DySky_texFogCubemap");

        internal static readonly int _DySky_unionHeightFogParams    = Shader.PropertyToID("_DySky_unionHeightFogParams");
        internal static readonly int _DySky_unionDisFogParams       = Shader.PropertyToID("_DySky_unionDisFogParams");
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

        Shader.SetGlobalVector(Uniforms._DySky_unionHeightFogParams, new Vector4(heightFogStart, 1f / heightFogDistance, heightFogDensity, 0f));
        Shader.SetGlobalVector(Uniforms._DySky_unionDisFogParams, new Vector4(disFogStart, 1f / disFogDistance, disFogDensity, 0f));
    }

    public void BakeFogCubeMap()
    {
        if (!dynamicFogCubemap)
        {
            dynamicFogCubemap = new Cubemap(128, TextureFormat.ARGB32, false);
            Shader.SetGlobalTexture(Uniforms._DySky_texFogCubemap, dynamicFogCubemap);
        }
        BakeFogCubeMap(dynamicFogCubemap);
    }

    public bool BakeFogCubeMap(Cubemap cubemap)
    {
        if (!matFogBake) return false;
        if (!cubemap) return false;
        if (!Camera.main) return false;

        Transform transSkydome = skyController.transSkydome;
        MeshRenderer skydomeMR = transSkydome.GetComponent<MeshRenderer>();

        int oldCM = Camera.main.cullingMask;
        DepthTextureMode oldDTM = Camera.main.depthTextureMode;
        int oldLayer = transSkydome.gameObject.layer;
        Material oldMat = skydomeMR.sharedMaterial;
        bool success = false;
        try
        {
            const int layer_temp = 31;
            Camera.main.cullingMask = 1 << layer_temp;
            transSkydome.gameObject.layer = layer_temp;
            skydomeMR.sharedMaterial = matFogBake;
            Camera.main.RenderToCubemap(cubemap);
            success = true;
        }
        catch(System.Exception e)
        {
            Debug.LogError(e);
        }
        skydomeMR.sharedMaterial = oldMat;
        transSkydome.gameObject.layer = oldLayer;
        Camera.main.depthTextureMode = oldDTM;
        Camera.main.cullingMask = oldCM;
        return success;
    }

    /*
    public RenderTexture BakeFogRT(int size = 256)
    {
        RenderTexture rt = new RenderTexture(new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGB32, 0));


        return rt;
    }
    */
}
