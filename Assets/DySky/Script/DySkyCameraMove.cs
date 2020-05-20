using UnityEngine;
using System.Collections;

public class DySkyCameraMove : MonoBehaviour {
	public float speed = 10f;

	void LateUpdate()
	{
        float deltaX = 0f;
        float deltaY = 0f;
        float deltaZ = 0f;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
        if (Input.GetKey(KeyCode.A)) deltaX = -speed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.D)) deltaX = speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) deltaY = speed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.E)) deltaY = -speed * Time.deltaTime;
        if (Input.GetKey(KeyCode.W)) deltaZ = speed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.S)) deltaZ = -speed * Time.deltaTime;
#else
        if (Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Moved)
        {
            deltaX = Input.GetTouch(1).deltaPosition.x * speed * Time.deltaTime * 0.1f;
            deltaZ = -Input.GetTouch(1).deltaPosition.y * speed * Time.deltaTime * 0.1f;
        }
#endif
        transform.position += transform.right * deltaX + transform.up * deltaY + transform.forward * deltaZ;
	}

}
