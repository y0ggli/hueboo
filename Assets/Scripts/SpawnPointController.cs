using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointController : MonoBehaviour {

    	
	// Update is called once per frame
	void Update () {
        transform.RotateAround(Vector3.zero, Vector3.up, 23 * Time.deltaTime);
    }

}
