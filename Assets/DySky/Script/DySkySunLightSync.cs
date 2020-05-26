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
    Transform sunTrans;
    private void Awake()
    {
        if (!sunLight)
        {
            this.enabled = false;
            return;
        }
        skyController = this.GetComponent<DySkyController>();
        sunTrans = sunLight.transform;
    }

    private void LateUpdate()
    {
        if (!sunLight) return;

        sunTrans.forward = skyController.GetCurrentDirectionalLightForward();
        sunLight.color = skyController.GetCurrentDirectionalLightColor();
        sunLight.intensity = skyController.GetCurrentDirectionalLightIntensity();

        // supplement main light lost in sunrise and sunset
        RenderSettings.ambientSkyColor *= 2 - sunLight.intensity;
        RenderSettings.ambientEquatorColor *= 2 - sunLight.intensity;
        RenderSettings.ambientGroundColor *= 2 - sunLight.intensity;
    }

}
