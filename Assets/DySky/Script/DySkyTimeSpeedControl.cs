using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DySkyTimeSpeedControl : MonoBehaviour {
    public DySkyController controller;
    public DySkyTimeElapse timeElapse;
    public Slider slider;
    public Text text;
    public Toggle toggle;

    void Start()
	{
        if (!controller || !timeElapse || !slider || !text || !toggle)
        {
            this.enabled = false;
            return;
        }
        slider.value = (timeElapse.speed + 3600) / 7200f;
        slider.onValueChanged.AddListener((value) =>
        {
            UpdateTime(value);
        });
        toggle.onValueChanged.AddListener((value) =>
        {
            UpdateTime(slider.value);
        });
        UpdateTime(slider.value);
    }	

    void UpdateTime(float progress01)
    {
        if (toggle.isOn)
        {
            timeElapse.enabled = true;
            timeElapse.speed = Mathf.RoundToInt(Mathf.Lerp(-3600, 3600, progress01));
            text.text = string.Format("{0} s/rs", timeElapse.speed);
        }
        else
        {
            timeElapse.enabled = false;
            controller.timeline = Mathf.Lerp(0, 24, progress01);
            int hour = (int)controller.timeline;
            int minute = (int)((controller.timeline - hour) * 60);
            text.text = string.Format("{0:00}:{1:00}", hour, minute);
        }
    }
}
