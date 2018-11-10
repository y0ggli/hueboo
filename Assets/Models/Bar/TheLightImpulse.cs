using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheLightImpulse : MonoBehaviour {

    public BeatDetector beatDetector;

    private Light light;

    private float HIGH;
    private float LOW;

    void Start () {
        beatDetector.OnBeat += OnBeat;
        light = GetComponent<Light>();

        HIGH = light.intensity;
        LOW = HIGH / 2;
    }
	
	void Update () {
        if (light.intensity > LOW) light.intensity -= .08f;
	}

    void OnBeat()
    {
        light.intensity = HIGH;
    }

}
