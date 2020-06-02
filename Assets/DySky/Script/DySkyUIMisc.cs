using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DySkyUIMisc : MonoBehaviour {
    public DySkyController controller;
    public Toggle toggleRenderIntoRT;
    public Text textInfo;
    public DySkyFogController fogController;
    public DySkyWaterController waterController;

    void Start()
	{
        if (!controller || !toggleRenderIntoRT)
        {
            this.enabled = false;
            return;
        }
        toggleRenderIntoRT.isOn = controller.renderIntoRT;
        toggleRenderIntoRT.onValueChanged.AddListener((value) =>
        {
            controller.renderIntoRT = value;
        });
    }

    void Update()
    {
        if (textInfo)
        {
            string info = "";
            info += string.Format("FPS: {0:0.00}\n", 1f / Time.unscaledDeltaTime);
            info += string.Format("MSAA: {0}\n", QualitySettings.antiAliasing);

            float utcTime24 = controller.GetCurrentUTCTime24();
            int hour = (int)utcTime24;
            int minute = (int)((utcTime24 - hour) * 60);
            info += string.Format("UTC Time: {0:00}:{1:00}\n", hour, minute);

            textInfo.text = info;
        }
    }

    public void OnProfileChanged(DySkyProfile profile)
    {
        controller.profile = profile;
    }

    public void OnFogBakeTypeChanged(Dropdown dropdown)
    {
        if (fogController)
            fogController.bakeType = (DySkyFogController.BakeType)dropdown.value;
    }

    public void OnWaterQualityChanged(Dropdown dropdown)
    {
        if (waterController)
            waterController.SetQualityLevel((DySkyWaterController.Quality)dropdown.value);
    }
}
