using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BoosManager : MonoBehaviour {

    public GameObject boos;
    public float spawnTime = 3f;
    public GameObject[] spawnPoints;

	// Use this for initialization
	void Start () {
        InvokeRepeating("Spawn", spawnTime, spawnTime);
	}
	

    void Spawn(){
        int spawnPointIndex = Random.Range(0, spawnPoints.Length);
        GameObject spawnPoint = spawnPoints[spawnPointIndex];
        int countBoos = GameObject.FindGameObjectsWithTag("Boos").Length;
        if(countBoos < 20){
            Instantiate(boos, spawnPoint.transform.position, spawnPoint.transform.rotation);
        }


    }

}
