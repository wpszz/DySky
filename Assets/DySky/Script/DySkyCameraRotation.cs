using UnityEngine;
using System.Collections;

public class DySkyCameraRotation : MonoBehaviour {
	private Vector3 eulerAngle;
	public float speed = 100;

	void Start()
	{
        eulerAngle = transform.localEulerAngles;
	}
	
	void LateUpdate()
	{
        float deltaPitch = 0f;
        float deltaYaw = 0f;
        if (Input.touchCount == 1)
        {
            deltaPitch = -Input.GetTouch(0).deltaPosition.x * speed * Time.deltaTime;
            deltaYaw = Input.GetTouch(0).deltaPosition.y * speed * Time.deltaTime;
        }
        else if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            deltaPitch = -Input.GetAxis("Mouse Y") * speed * Time.deltaTime;
            deltaYaw = Input.GetAxis("Mouse X") * speed * Time.deltaTime;
        }
        eulerAngle.x = Repeat360(eulerAngle.x + deltaPitch);
        eulerAngle.y = Repeat360(eulerAngle.y + deltaYaw);
        transform.localEulerAngles = eulerAngle;
	}
	
	private float Repeat360(float v)
	{
        if (v > 360) v -= 360;
        if (v < -360) v += 360;
        return v;
	}
}
