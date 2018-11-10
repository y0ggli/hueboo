using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class BoosController : MonoBehaviour {

    private GameObject movePoint;
    public float speed;
    public bool isAtBar;
    public Color[] colors;
    private Renderer rend;
    public int maxColorDifferenceRGB = 20;
    public int maxFillDifference = 30;
    private GameObject score;
    private bool alreadyScored;

    // Use this for initialization
    void Start () {
        movePoint = GameObject.FindGameObjectWithTag("MovePoint");
        rend = GetComponent<Renderer>();
        rend.materials[1].color = colors[UnityEngine.Random.Range(0, colors.Length)];
        score = GameObject.FindGameObjectWithTag("Score");
    }
	
	// Update is called once per frame
	void Update () {
        float step = speed * Time.deltaTime;
        if (!isAtBar){
            //print("move to bar");
            transform.position = Vector3.MoveTowards(transform.position, movePoint.transform.position, step);

        }
        Vector3 direction = (movePoint.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, step);
    }

    protected void LateUpdate()
    {
        transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject cup = collision.gameObject;
        

        if(collision.gameObject.CompareTag("Cup")){
            GameObject other = cup.transform.Find("sirup").gameObject;
            if (IsFilled(other) && SameColor(rend.materials[1].color, other.GetComponent<Renderer>().material.color)){

                GetComponent<AudioSource>().Play();

                GetComponent<TheHover>().enabled = false;
                GetComponent<Rigidbody>().drag = 0;
                Vector3 direction = movePoint.transform.position - transform.position;
                direction.Normalize();
                GetComponent<Rigidbody>().velocity = new Vector3(-direction.x * 2, 2, -direction.z * 2);
                Invoke("destroy", 30); // destroy after 30 s

                cup.transform.Find("sirup").GetComponent<TheSirup>().Reset();
                cup.GetComponent<TheResetable>().Reset(0);

                if (!alreadyScored && score != null)
                {
                    alreadyScored = true;
                    print("update Score");
                    score.GetComponent<ScoreController>().score += 10;
                }
            }
        }
    }

    private bool SameColor(Color expected, Color actual){
        Color32 exp = expected;
        Color32 act = actual;
        print(exp.r + " " + act.r);
        print(exp.g + " " + act.g);
        print(exp.b + " " + act.b);
        return Math.Abs(exp.r - act.r) < maxColorDifferenceRGB && Math.Abs(exp.g - act.g) < maxColorDifferenceRGB && Math.Abs(exp.b - act.b) < maxColorDifferenceRGB;
    }

    private bool IsFilled(GameObject cup){
        return cup.GetComponent<TheSirup>().maxSize - cup.GetComponent<TheSirup>().currentSize < maxFillDifference;
    }

    private void destroy()
    {
        Destroy(gameObject);
    }

}


