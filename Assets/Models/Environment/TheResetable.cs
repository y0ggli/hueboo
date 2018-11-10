using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheResetable : MonoBehaviour {

    private Rigidbody defaultRigidbody;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;

    public float defaultDelay = 2.0f;

    void Start ()
    {
        defaultRigidbody = GetComponent<Rigidbody>();
        defaultPosition = transform.position;
        defaultRotation = transform.localRotation;
    }

    public void Reset()
    {
        Reset(defaultDelay);
    }

    public void Reset(float delay)
    {
        Invoke("reset", delay);
    }

    private void reset()
    {

        transform.position = defaultPosition;
        transform.localRotation = defaultRotation;

        if (defaultRigidbody != null)
        {
            defaultRigidbody.velocity = Vector3.zero;
            defaultRigidbody.angularVelocity = Vector3.zero;
        }

    }
}
