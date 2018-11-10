using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightColorManager : MonoBehaviour {

    public Color[] colors;
    private new Light light;
    public BeatDetector beatDetector;
    private int position;
    private Color beatedColor;
    private Color currentColor;
    public float smoothnessChange;

    // Use this for initialization
    void Start () {
        light = GetComponent<Light>();
        beatDetector.OnBeat += OnBeat;
        position = 0;
        currentColor = light.color;
        beatedColor = currentColor;
        //light.color = currentColor;
    }

    private void Update()
    {
        currentColor = Color.Lerp(currentColor, beatedColor, smoothnessChange * Time.deltaTime);
        light.color = currentColor;

    }

    void OnBeat(){
        //print("Beat");
        position = (position + 1) % colors.Length;
        beatedColor = colors[position];
    }


}
