using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollow : MonoBehaviour {

    public float smooth;
    private GameObject target;
    private float zOffSet;
    private Camera camera;

	// Use this for initialization
    void Start()
    {
        target = GameObject.Find("Player"); //nom gameobject, NO tag
        camera = GetComponent<Camera>();
        zOffSet = transform.position.z;
    }
	
	void FixedUpdate () {
        if (target != null)
        {
            transform.position = Vector3.Lerp(transform.position, target.transform.position, smooth);
            transform.position = new Vector3(transform.position.x, transform.position.y, zOffSet);
        }

        //TODO: Improve zoom in zoom out
        float d = Input.GetAxis("Mouse ScrollWheel");
        if (d < 0f)
        {
            camera.orthographicSize += -d*5;
        }
        else if (d > 0f)
        {
            camera.orthographicSize -= d*5;
        }


    }
}
