using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BlitEventTrigger = DySkyPreFrameBuffers.BlitEventTrigger;
using ColorPrecision = DySkyPreFrameBuffers.ColorPrecision;
using DepthPrecision = DySkyPreFrameBuffers.DepthPrecision;

public class DySkyUIMisc : MonoBehaviour {
    public DySkyController controller;
    public Toggle toggleRenderIntoRT;
    public Text textInfo;
    public DySkyFogController fogController;
    public DySkyWaterController waterController;

    List<float> listFps = new List<float>();

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
            float fps = 1f / Time.unscaledDeltaTime;
            float minFps = fps;
            listFps.Add(fps);
            if (listFps.Count >= 60) listFps.RemoveAt(0);
            foreach (var tmp in listFps) minFps = Mathf.Min(minFps, tmp);

            string info = "";
            info += string.Format("FPS: {0:0.00}/{1:0.00}\n", fps, minFps);
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

    public void OnSkyBakeTypeChanged(Dropdown dropdown)
    {
        if (controller)
            controller.bakeType = (DySkyController.BakeType)dropdown.value;
    }

    public void OnWaterQualityChanged(Dropdown dropdown)
    {
        if (waterController)
            waterController.SetQualityLevel((DySkyWaterController.Quality)dropdown.value);
    }

    struct PreFrameBufferSetting
    {
        public BlitEventTrigger evt;
        public ColorPrecision color;
        public DepthPrecision depth;
    }

    List<PreFrameBufferSetting> settings = new List<PreFrameBufferSetting>()
    {
        new PreFrameBufferSetting {evt = BlitEventTrigger.Off, color = ColorPrecision.HalfRGB111110Float, depth = DepthPrecision.HalfRHalf},
        new PreFrameBufferSetting {evt = BlitEventTrigger.AfterForwardOpaque, color = ColorPrecision.FullRGB111110Float, depth = DepthPrecision.FullRHalf},
        new PreFrameBufferSetting {evt = BlitEventTrigger.AfterForwardOpaque, color = ColorPrecision.HalfRGB111110Float, depth = DepthPrecision.HalfRHalf},
        new PreFrameBufferSetting {evt = BlitEventTrigger.AfterForwardOpaque, color = ColorPrecision.HalfRGB111110Float, depth = DepthPrecision.HalfR16},
        new PreFrameBufferSetting {evt = BlitEventTrigger.AfterForwardOpaque, color = ColorPrecision.Off, depth = DepthPrecision.HalfRHalf},
        new PreFrameBufferSetting {evt = BlitEventTrigger.AfterForwardOpaque, color = ColorPrecision.HalfRGB111110Float, depth = DepthPrecision.Off},
    };

    public void OnPreFrameBufferChanged(Dropdown dropdown)
    {
        PreFrameBufferSetting setting = settings[dropdown.value];
        DySkyPreFrameBuffers.BindCamera(Camera.main, setting.evt, setting.color, setting.depth);
        if (waterController)
            waterController.Refresh();
    }
}
