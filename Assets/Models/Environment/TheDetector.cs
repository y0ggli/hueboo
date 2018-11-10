using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;
using System;

[RequireComponent(typeof(ObiSolver))]
public class TheDetector : MonoBehaviour {

    public String killerTag = "Spongebob";
    public String sirupObjectName = "sirup";

    ObiSolver solver;
    Obi.ObiSolver.ObiCollisionEventArgs collisionEvent;

    void Awake()
    {
        solver = GetComponent<Obi.ObiSolver>();
    }

    void OnEnable()
    {
        solver.OnCollision += Detector;
    }

    void OnDisable()
    {
        solver.OnCollision -= Detector;
    }

    void Detector (object sender, Obi.ObiSolver.ObiCollisionEventArgs e)
    {
        foreach (Oni.Contact contact in e.contacts)
        {
            if (contact.distance < 0.01)
            {
                Component collider;
                if (ObiCollider.idToCollider.TryGetValue(contact.other, out collider))
                {
                    if(collider.tag == killerTag)
                    {
                        ObiSolver.ParticleInActor pa = solver.particleToActor[contact.particle];
                        ObiEmitter emitter = pa.actor as ObiEmitter;

                        // destory particle
                        emitter.life[pa.indexInActor] = 0;

                        if (emitter != null)
                        {
                            
                            Transform sirupObject = collider.transform.parent.Find(sirupObjectName);
                            if(sirupObject != null)
                            {

                                if(!emitter.transform.parent.Equals(sirupObject.parent))
                                {
                          
                                    // update cup
                                    TheSirup cupScript = sirupObject.GetComponent<TheSirup>();
                                    Color color = emitter.transform.GetComponent<ObiParticleRenderer>().particleColor;
                                    cupScript.addParticle(color);

                                }
                            } 
                        }
                    }
                }
            }
        }
    }

}
