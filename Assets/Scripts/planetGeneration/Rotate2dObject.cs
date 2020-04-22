using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate2dObject : MonoBehaviour
{
    public float rotationSpeed = 0f; 
    private Transform planet;

    // Start is called before the first frame update
    void Start()
    {
        planet = this.gameObject.transform;
    }

    // Update is called once per frame
    void Update()
    {
        planet.Rotate(new Vector3(0, Time.deltaTime * rotationSpeed, 0));
    }
}
