using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePointController : MonoBehaviour {


    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Boos")){
            //print("at bar");
            other.gameObject.GetComponent<BoosController>().isAtBar = true;
        }
       
    }

    void OnTriggerExit(Collider other)
    {

        if (other.gameObject.CompareTag("Boos")){ 
            other.gameObject.GetComponent<BoosController>().isAtBar = false;
        }
       
    }
}
