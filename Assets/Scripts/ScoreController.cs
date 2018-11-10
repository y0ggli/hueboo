using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreController : MonoBehaviour {

    [HideInInspector]
    public int score;
    private TextMeshProUGUI text;

    // Use this for initialization
    void Start () {
        text = GetComponent<TextMeshProUGUI>();
        score = 0;
    }
	
	// Update is called once per frame
	void Update () {
        text.text = "Score: " + score; 
	}
}
