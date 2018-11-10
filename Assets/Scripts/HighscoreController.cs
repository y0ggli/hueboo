using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class HighscoreController : MonoBehaviour {

    private string text = "";

    // Use this for initialization
    void Start () {
        int highscore = PlayerPrefs.GetInt("Highscore", 0);

        text = highscore.ToString();
    }
	
	// Update is called once per frame
	void Update () {
        GetComponent<UnityEngine.UI.Text>().text = text;
	}

}
