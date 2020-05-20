using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DySkyUIMisc : MonoBehaviour {
    public DySkyController controller;
    public Toggle toggleRenderIntoRT;
    public DySkyFogController fogController;
    public Text textInfo;

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
            textInfo.text = info;
        }
    }

    public void OnProfileChanged(DySkyProfile profile)
    {
        controller.profile = profile;
    }

    public void OnFogBakeTypeChanged(Dropdown dropdown)
    {
        fogController.bakeType = (DySkyFogController.BakeType)dropdown.value;
    }
}
