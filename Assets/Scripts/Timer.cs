using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour {

    public float timeLeft;
    private TextMeshProUGUI text;
    private AudioSource audio;

	// Use this for initialization
	void Start () {
        text = GetComponent<TextMeshProUGUI>();
        StartCoroutine("Countdown");
        audio = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {

        int minutes = Mathf.FloorToInt(timeLeft / 60);
        int seconds = Mathf.FloorToInt(timeLeft % 60);
        text.text = minutes.ToString("0") + ":" + seconds.ToString("00");



        if (timeLeft <= 0)
        {
            StopCoroutine("Countdown");
            text.text = "STOP";
            GameObject scoreObject = GameObject.FindGameObjectWithTag("Score");
            int score = scoreObject.GetComponent<ScoreController>().score;
            PlayerPrefs.SetInt("Score", score);
            if(PlayerPrefs.HasKey("Highscore")){
                int highscore = PlayerPrefs.GetInt("Highscore");
                if(score > highscore){
                    PlayerPrefs.SetInt("Highscore", score);
                }
            } else {
                PlayerPrefs.SetInt("Highscore", score);
                //print("old Highscore");
            }
            SceneManager.LoadScene("MenuScene");

        }

    }

    IEnumerator Countdown(){

        while(true) {
            yield return new WaitForSeconds(1);
            timeLeft--;
            if (timeLeft <= 10)
            {
                audio.Play();
            }
        }
    }
}
