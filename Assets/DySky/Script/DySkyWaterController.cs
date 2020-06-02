using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(1001)]
public class DySkyWaterController : MonoBehaviour
{
    public enum Quality
    {
        Low = 0,
        Mid = 1,
        High = 2,
    }

    [SerializeField]
    Quality quality = Quality.High;
    [SerializeField]
    Material sharedMaterial;

    private void Awake()
    {
        if (!sharedMaterial) this.enabled = false;
    }

    private void OnEnable()
    {
        SetupWaterKeys(sharedMaterial, quality);
    }

    private void OnDisable()
    {
        SetupWaterKeys(sharedMaterial, Quality.Low);
    }

#if UNITY_EDITOR
    private void LateUpdate()
    {
        SetupWaterKeys(sharedMaterial, quality);
    }
#endif

    public void SetQualityLevel(Quality quality)
    {
        this.quality = quality;
        SetupWaterKeys(sharedMaterial, quality);
    }

    private static void SetupWaterKeys(Material material, Quality quality)
    {
        if (!material || !material.shader) return;

        switch (quality)
        {
            case Quality.High:
                Shader.EnableKeyword("DY_SKY_GRAB_PASS_ENABLE");
                Shader.EnableKeyword("DY_SKY_SOFT_EDGE_ENABLE");
                material.shader.maximumLOD = 400;
                if (Camera.main) Camera.main.depthTextureMode |= DepthTextureMode.Depth;
                break;
            case Quality.Mid:
                Shader.EnableKeyword("DY_SKY_GRAB_PASS_ENABLE");
                Shader.DisableKeyword("DY_SKY_SOFT_EDGE_ENABLE");
                material.shader.maximumLOD = 300;
                if (Camera.main) Camera.main.depthTextureMode &= ~DepthTextureMode.Depth;
                break;
            default:
                Shader.DisableKeyword("DY_SKY_GRAB_PASS_ENABLE");
                Shader.DisableKeyword("DY_SKY_SOFT_EDGE_ENABLE");
                material.shader.maximumLOD = 200;
                if (Camera.main) Camera.main.depthTextureMode &= ~DepthTextureMode.Depth;
                break;
        }
    }
}
