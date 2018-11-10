using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class TheGreatPour : MonoBehaviour {

    public int content = 10;

    private float maxDiff;

    private Transform top;
    private Transform floor;
    private ObiEmitter emitter;

	void Start ()
    {
     
        top = transform.Find("top");
        floor = transform.Find("floor");
        emitter = transform.Find("emitter").GetComponent<Obi.ObiEmitter>();

        maxDiff = Vector3.Distance(top.position, floor.position);

        // update bottle color
        Color color = emitter.transform.GetComponent<ObiParticleRenderer>().particleColor;
        transform.Find("sirup").GetComponent<Renderer>().material.color = color;

    }
	
	void Update ()
    {

        float diff = Math.Max(0, floor.position.y - top.position.y + .05f);
        float speed = (float) Map(diff, 0, maxDiff, 0, 5);
        emitter.speed = speed;

    }

    double Map(double x, double in_min, double in_max, double out_min, double out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

}
