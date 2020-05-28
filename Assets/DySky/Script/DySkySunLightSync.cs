using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(1001)]
[RequireComponent(typeof(DySkyController))]
public class DySkySunLightSync : MonoBehaviour
{
    public Light sunLight;

    DySkyController skyController;
    private void Awake()
    {
        skyController = this.GetComponent<DySkyController>();
        if (!sunLight)
        {
            this.enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        if (!sunLight) return;

        sunLight.transform.forward = skyController.GetCurrentDirectionalLightForward();
        sunLight.color = skyController.GetCurrentDirectionalLightColor();
        sunLight.intensity = skyController.GetCurrentDirectionalLightIntensity();

        // supplement main light lost in sunrise and sunset
        RenderSettings.ambientSkyColor *= 2 - sunLight.intensity;
        RenderSettings.ambientEquatorColor *= 2 - sunLight.intensity;
        RenderSettings.ambientGroundColor *= 2 - sunLight.intensity;
    }

}
