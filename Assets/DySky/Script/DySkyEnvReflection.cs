using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(1002)]
[RequireComponent(typeof(DySkyFogController))]
public class DySkyEnvReflection : MonoBehaviour
{
    DySkyFogController fogController;

    private void Awake()
    {
        fogController = this.GetComponent<DySkyFogController>();
    }

    private void LateUpdate()
    {
        if (RenderSettings.defaultReflectionMode != UnityEngine.Rendering.DefaultReflectionMode.Custom ||
            RenderSettings.customReflection != fogController.GetCurrentCubeMap())
        {
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflection = fogController.GetCurrentCubeMap();
        }
    }
}
