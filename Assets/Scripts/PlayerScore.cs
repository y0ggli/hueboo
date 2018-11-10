using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerScore : MonoBehaviour {

    private string text;

    // Use this for initialization
    void Start () {
        int score = PlayerPrefs.GetInt("Score", 0);

        text = score.ToString();
    }

    // Update is called once per frame
    void Update () {
        GetComponent<UnityEngine.UI.Text>().text = text;
    }
}
