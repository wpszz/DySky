using UnityEngine;
using System.Collections;

[RequireComponent(typeof(DySkyController))]
public class DySkyTimeElapse : MonoBehaviour {

    [Tooltip("Time(sec) elapse per-engine-second")]
    [Range(-3600, 3600)]
    public int speed = 1;

    private DySkyController controller;
    private float startSeconds;
    private float curSeconds;
    void Start()
	{
        controller = this.GetComponent<DySkyController>();
        startSeconds = controller.timeline * 3600f;
        curSeconds = startSeconds;
    }
	
	void Update()
	{
        curSeconds += Time.deltaTime * speed;
        curSeconds = Mathf.Repeat(curSeconds, 86400f);
        controller.timeline = curSeconds / 3600f;
    }
}
