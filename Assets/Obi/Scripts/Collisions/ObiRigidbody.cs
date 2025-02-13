﻿using UnityEngine;
using System;
using System.Collections;

namespace Obi{

	/**
	 * Small helper class that lets you specify Obi-only properties for rigidbodies.
	 */

	[ExecuteInEditMode]
	[RequireComponent(typeof(Rigidbody))]
	public class ObiRigidbody : ObiRigidbodyBase
	{
		private Rigidbody unityRigidbody;

		public void Awake(){
			unityRigidbody = GetComponent<Rigidbody>();
			base.Awake();
		}

		public override void UpdateIfNeeded(){

			if (dirty){

				velocity = unityRigidbody.velocity;
				angularVelocity = unityRigidbody.angularVelocity;

				adaptor.Set(unityRigidbody,kinematicForParticles);
				Oni.UpdateRigidbody(oniRigidbody,ref adaptor);

				dirty = false;

			}

		}

		/**
		 * Reads velocities back from the solver.
		 */
		public override void UpdateVelocities(){

			if (!dirty){

				// kinematic rigidbodies are passed to Obi with zero velocity, so we must ignore the new velocities calculated by the solver:
				if (unityRigidbody.isKinematic || !kinematicForParticles){

					Oni.GetRigidbodyVelocity(oniRigidbody,ref oniVelocities);	
					unityRigidbody.velocity += oniVelocities.linearVelocity - velocity;
					unityRigidbody.angularVelocity += oniVelocities.angularVelocity - angularVelocity;

				}

				dirty = true;

			}

		}
	}
}

