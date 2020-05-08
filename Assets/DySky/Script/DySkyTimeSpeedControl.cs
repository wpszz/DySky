using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DySkyTimeSpeedControl : MonoBehaviour {
    public DySkyTimeElapse timeElapse;
    public Slider slider;
    public Text text;

    void Start()
	{
        if (!timeElapse || !slider || !text)
        {
            this.enabled = false;
            return;
        }
        slider.value = (timeElapse.speed + 3600) / 7200f;
        slider.onValueChanged.AddListener((value) =>
        {
            timeElapse.speed = Mathf.RoundToInt(Mathf.Lerp(-3600, 3600, value));
            text.text = string.Format("Time Speed: {0} s/rs", timeElapse.speed);
        });
    }	
}
