using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidLightController : MonoBehaviour {

    public GameObject otherLight;
    private Light light;
	// Use this for initialization
	void Start () {
        light = GetComponent<Light>();
    }
	
	// Update is called once per frame
	void Update () {
        light.color = otherLight.GetComponent<Light>().color;

    }
}
