using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheDeathZone : MonoBehaviour {

    void OnCollisionEnter(Collision collision)
    {

        TheResetable resetable = collision.transform.GetComponent<TheResetable>();
        if (resetable != null) resetable.Reset();

    }

}
