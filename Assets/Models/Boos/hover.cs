using UnityEngine;
using System;
using System.Collections;

public class hover : MonoBehaviour
{

    private float floatStrength = .1f;

    float originalY;
    float rand;

    void Start()
    {
        this.originalY = this.transform.position.y;
        this.rand = UnityEngine.Random.Range(0, 6.0f);
    }

    void Update()
    {
        
        transform.position = new Vector3(
            transform.position.x,
            (float) Math.Sin((Time.time + this.rand)) * floatStrength,
            transform.position.z
            );
            
    }
}