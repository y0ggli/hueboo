using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;
using System.Linq;

public class TheSirup : MonoBehaviour {

    [HideInInspector]
    public int maxSize = 100;

    [HideInInspector]
    public float currentSize = 0;

    [HideInInspector]
    public Dictionary<Color, int> fluidValues = new Dictionary<Color, int>();

    [HideInInspector]
    public bool needsUpdate = true;

    private float maxDiff;

    private Transform top;
    private Transform floor;
    private ObiEmitter emitter;
    private ObiParticleRenderer renderer;

    void Start () {

        top = transform.parent.Find("top");
        floor = transform.parent.Find("floor");
        emitter = transform.parent.Find("emitter").GetComponent<Obi.ObiEmitter>();
        renderer = transform.parent.Find("emitter").GetComponent<ObiParticleRenderer>();

        maxDiff = Vector3.Distance(top.position, floor.position);

    }

    void Update () {

        float diff = Math.Max(0, floor.position.y - top.position.y + .1f);
        float fill = maxSize / currentSize;
        float speed = (float) Map(diff, 0, maxDiff * fill, 0, 8);

        // pour sirup if on an angle
        if(speed > 0 && currentSize > 0)
        {

            if ( equalize(Math.Max(0, currentSize - speed) ))
            {
                emitter.speed = speed;
            }

            needsUpdate = true;

        }

        emitter.speed = Math.Max(0, emitter.speed - .1f);


        if (needsUpdate)
        {

            // take color average
            Color color = new Color(0, 0, 0, 0);
            foreach (var pair in fluidValues)
            {
                color = mixTwo(color, pair.Key, pair.Key.a * (pair.Value / currentSize) );
            }

            // update block color
            GetComponent<Renderer>().material.color = color;

            // set size
            float level = Math.Min(1, currentSize / maxSize);
            transform.localScale = new Vector3(1, level, 1);

            // set emitter color if it is a color
            if(color.a > 0.1) renderer.particleColor = color;

            // update
            needsUpdate = false;

        }

    }

    private bool equalize(float newLevel)
    {

        float overhead = currentSize - newLevel;

        if (overhead > .1f)
        {
            
            float modifyer = overhead / currentSize;

            foreach (Color color in fluidValues.Keys.ToList())
            {
                fluidValues[color] = (int) (fluidValues[color] - fluidValues[color] * modifyer);
                if (fluidValues[color] < 1) fluidValues.Remove(color);
            }

            currentSize = newLevel;

            return true;

        }

        return false;
        
    }

    public void addParticle(Color color)
    {

        currentSize += 1;

        if (currentSize > maxSize)
        {
            equalize(maxSize - 3);
            emitter.speed = 1;
        }

        int currentCount;
        fluidValues.TryGetValue(color, out currentCount);
        fluidValues[color] = currentCount + 1;

        needsUpdate = true;

    }

    public void Reset()
    {
        currentSize = 0;
        fluidValues.Clear();
        needsUpdate = true;
    }

    Color mixTwo(Color a, Color b, float bA)
    {

        // from: https://stackoverflow.com/a/727339/3137109

        var r = new Color();
        r.a = 1 - (1 - bA) * (1 - a.a); 
        r.r = b.r * bA / r.a + a.r * a.a * (1 - bA) / r.a;
        r.g = b.g * bA / r.a + a.g * a.a * (1 - bA) / r.a; 
        r.b = b.b * bA / r.a + a.b * a.a * (1 - bA) / r.a; 

        return r;

    }

    double Map(double x, double in_min, double in_max, double out_min, double out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

}
