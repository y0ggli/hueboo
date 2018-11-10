using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatDetector : MonoBehaviour {

    public AudioSource audio;
    public delegate void OnBeatHandler();
    public event OnBeatHandler OnBeat;

    private float[] samples0Channel;
    private float[] samples1Channel;

    private float[] historyBuffer;

    public FFTWindow FFTWindow;
    public int bufffersize;


	// Use this for initialization
	void Start () {
        samples0Channel = new float[bufffersize];
        samples1Channel = new float[bufffersize];
        historyBuffer = new float[43];
    }
	
	// Update is called once per frame
	void Update () {

        float instantEnergy = GetInstantEnergy();

        float localAverageEnergy = GetLocalAverageEnergy();

        float variance = GetVariance(localAverageEnergy);

        double constantC = (-0.0025714 * variance) + 1.5142857;

        float[] shiftedHistoryBuffer = ShiftArray(historyBuffer, 1);

        shiftedHistoryBuffer[0] = instantEnergy;

        Array.Copy(shiftedHistoryBuffer, historyBuffer, historyBuffer.Length);

        if (instantEnergy > constantC * localAverageEnergy){
            if(OnBeat != null){
                OnBeat();
                //print("InstantEnergy: " + instantEnergy + " C * AverageEnergy: " + (constantC * localAverageEnergy) );
            }

        }


    }

    public float GetInstantEnergy(){
        float energy = 0;

        audio.GetSpectrumData(samples0Channel, 0, FFTWindow);
        audio.GetSpectrumData(samples1Channel, 1, FFTWindow);

        for (int i = 0; i < bufffersize; i++) {
            energy += (float)Math.Pow(samples0Channel[i], 2) + (float)Math.Pow(samples1Channel[i], 2);
        }

        return energy;
    }

    public float GetLocalAverageEnergy(){
        float averageEnergy = 0;

        for (int i = 0; i < historyBuffer.Length; i++){
            //averageEnergy += (float)Math.Pow(historyBuffer[i], 2);
            averageEnergy += historyBuffer[i];
        }

        return averageEnergy / historyBuffer.Length;

    }

    public float GetVariance(float localAverageEnergy)
    {
        float variance = 0;

        for (int i = 0; i < historyBuffer.Length; i++){
            variance += (float)Math.Pow(historyBuffer[i] - localAverageEnergy, 2);
        }

        return variance / historyBuffer.Length;
    }

    private float[] ShiftArray(float[] arr, int amount) {
        float[] result = new float[arr.Length];

        for (int i = 0; i < arr.Length - amount; i++) {
            result[i + amount] = arr[i];
        }

        return result;
    }





}
