/**
\mainpage Obi documentation
 
Introduction:
------------- 

Obi is a position-based dynamics framework for unity. It enables the simulation of cloth, ropes and fluid in realtime, complete with two-way
rigidbody interaction.
 
Features:
-------------------

- Particles can be pinned both in local space and to rigidbodies (kinematic or not).
- Realistic wind forces.
- Rigidbodies react to particle dynamics, and particles reach to each other and to rigidbodies too.
- Easy prefab instantiation, particle-based actors can be translated, scaled and rotated.
- Simulation can be warm-started in the editor, then all simulation state gets serialized with the object. This means
  your prefabs can be stored at any point in the simulation, and they will resume it when instantiated.
- Custom editor tools.

*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using System.Linq;

namespace Obi
{

/**
 * An ObiSolver component simulates particles and their interactions using the Oni unified physics library.
 * Several kinds of constraint types and their parameters are exposed, and several Obi components can
 * be used to feed particles and constraints to the solver.
 */
[ExecuteInEditMode]
[AddComponentMenu("Physics/Obi/Obi Solver")]
[DisallowMultipleComponent]
public sealed class ObiSolver : MonoBehaviour
{

	public enum SimulationOrder{
		FixedUpdate,
		AfterFixedUpdate,
		LateUpdate
	}

	public class ObiCollisionEventArgs : System.EventArgs{
		public ObiList<Oni.Contact> contacts = new ObiList<Oni.Contact>();	/**< collision contacts.*/
	}

	public class ObiFluidEventArgs : System.EventArgs{

		public ObiList<int> indices = new ObiList<int>();			/**< fluid particle indices.*/
		public ObiList<Vector4> vorticities = new ObiList<Vector4>();
		public ObiList<float> densities = new ObiList<float>();

	}

	public class ParticleInActor{
		public ObiActor actor;
		public int indexInActor;

		public ParticleInActor(ObiActor actor, int indexInActor){
			this.actor = actor;
			this.indexInActor = indexInActor;
		}
	}

	public const int MAX_NEIGHBOURS = 92;
	public const int CONSTRAINT_GROUPS = 12;

	public event System.EventHandler OnFrameBegin;
	public event System.EventHandler OnStepBegin;
	public event System.EventHandler OnFixedParticlesUpdated;
	public event System.EventHandler OnStepEnd;
	public event System.EventHandler OnBeforePositionInterpolation;
	public event System.EventHandler OnBeforeActorsFrameEnd;
	public event System.EventHandler OnFrameEnd;
	public event System.EventHandler<ObiCollisionEventArgs> OnCollision;
	public event System.EventHandler<ObiFluidEventArgs> OnFluidUpdated;
	
	public int maxParticles = 5000;

	[HideInInspector] [NonSerialized] public bool simulate = true;

	public uint substeps = 1;

	[Tooltip("If enabled, will force the solver to keep simulating even when not visible from any camera.")]
	public bool simulateWhenInvisible = true; 			/**< Whether to keep simulating the cloth when its not visible by any camera.*/

	[Tooltip("If enabled, the solver object transform will be used as the frame of reference for all actors using this solver, instead of the world's frame.")]
	public bool simulateInLocalSpace = false;

	[Tooltip("Determines when will the solver update particles.")]
	public SimulationOrder simulationOrder = SimulationOrder.FixedUpdate;

	public LayerMask collisionLayers = 1;

	[ChildrenOnly]
	public Oni.SolverParameters parameters = new Oni.SolverParameters(Oni.SolverParameters.Interpolation.None,
	                                                                  new Vector4(0,-9.81f,0,0));

	[HideInInspector] [NonSerialized] public List<ObiActor> actors = new List<ObiActor>();

	private int allocatedParticleCount = 0;
	[HideInInspector] [NonSerialized] public ParticleInActor[] particleToActor;
	[HideInInspector] [NonSerialized] public int[] materialIndices;
	[HideInInspector] [NonSerialized] public int[] fluidMaterialIndices;

	private int[] activeParticles;
	private List<ObiEmitterMaterial> emitterMaterials = new List<ObiEmitterMaterial>();

	[HideInInspector] [NonSerialized] public Vector4[] renderablePositions;	/**< renderable particle positions.*/
	
	// constraint parameters:
	[Header("Constraints")]
	public Oni.ConstraintParameters distanceConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters bendingConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters particleCollisionConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters collisionConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters skinConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Sequential,3);
	public Oni.ConstraintParameters volumeConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters tetherConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters pinConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,3);
	public Oni.ConstraintParameters stitchConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,2);
	public Oni.ConstraintParameters densityConstraintParameters = new Oni.ConstraintParameters(true,Oni.ConstraintParameters.EvaluationOrder.Parallel,2);

	private IntPtr oniSolver;

	private ObiCollisionEventArgs collisionArgs = new ObiCollisionEventArgs();
	private ObiFluidEventArgs fluidArgs = new ObiFluidEventArgs();

	private ObiEmitterMaterial defaultFluidMaterial;
	private UnityEngine.Bounds bounds = new UnityEngine.Bounds();
	private Matrix4x4 lastTransform;
 
 	private bool initialized = false;
	private bool isVisible = true;
	private float smoothDelta = 0.02f;
	private int renderablePositionsClients = 0;		/** counter for the amount of actors that need renderable positions.*/

	public IntPtr OniSolver
	{
		get{return oniSolver;}
	}

	public UnityEngine.Bounds Bounds
	{
		get{return bounds;}
	}

	public Matrix4x4 LastTransform
	{
		get{return lastTransform;}
	}

	public bool IsVisible
	{
		get{return isVisible;}
	}

	public int AllocParticleCount{
		get{return allocatedParticleCount;}
	}

	public bool IsUpdating{
		get{return (initialized && simulate && (simulateWhenInvisible || IsVisible));}
	}

	public void RequireRenderablePositions(){
		renderablePositionsClients++;
	}

	public void RelinquishRenderablePositions(){
		if (renderablePositionsClients > 0)
			renderablePositionsClients--;
	}

	void OnValidate(){
		if (substeps < 1) substeps = 1;
	}

	void Awake(){

		lastTransform = transform.localToWorldMatrix;

		if (Application.isPlaying) //only during game.
			Initialize();
	}

	void Start(){
		if (Application.isPlaying){
			ObiColliderBase[] colliders = FindObjectsOfType<ObiColliderBase>();
			foreach (ObiColliderBase c in colliders){
				c.RegisterInSolver(this,true);
			}
		}
	}

	void OnDestroy(){
		if (Application.isPlaying){ //only during game.
			Teardown();
			ObiColliderBase[] colliders = FindObjectsOfType<ObiColliderBase>();
			foreach (ObiColliderBase c in colliders){
				c.RemoveFromSolver(this);
			}
		}
	}

	void OnEnable(){
		if (!Application.isPlaying) //only in editor.
			Initialize();
		StartCoroutine("RunLateFixedUpdate");
		ObiArbiter.RegisterSolver(this);		
	}
	
	void OnDisable(){
		if (!Application.isPlaying) //only in editor.
			Teardown();
		StopCoroutine("RunLateFixedUpdate");
		ObiArbiter.UnregisterSolver(this);
	}
	
	public void Initialize(){

		// Tear everything down first:
		Teardown();
			
		try{

			// Create a default material:
			defaultFluidMaterial = ScriptableObject.CreateInstance<ObiEmitterMaterialFluid>();
			defaultFluidMaterial.hideFlags = HideFlags.HideAndDontSave;
	
			// Create the Oni solver:
			oniSolver = Oni.CreateSolver(maxParticles,MAX_NEIGHBOURS);
			
			actors = new List<ObiActor>();
			activeParticles = new int[maxParticles];
			particleToActor = new ParticleInActor[maxParticles];
			materialIndices = new int[maxParticles];
			fluidMaterialIndices = new int[maxParticles];
			renderablePositions = new Vector4[maxParticles];
			
			// Initialize materials:
			UpdateEmitterMaterials();
			
			// Initialize parameters:
			UpdateParameters();
			
		}catch (Exception exception){
			Debug.LogException(exception);
		}finally{
			initialized = true;
		};

	}

	private void Teardown(){
	
		if (!initialized) return;
		
		try{

			while (actors.Count > 0){
				actors[actors.Count-1].RemoveFromSolver(null);
			}
				
			Oni.DestroySolver(oniSolver);
			oniSolver = IntPtr.Zero;

			GameObject.DestroyImmediate(defaultFluidMaterial);
		
		}catch (Exception exception){
			Debug.LogException(exception);
		}finally{
			initialized = false;
		}
	}

	/**
	 * Adds the actor to this solver. Will return whether the allocation was sucessful or not.
	 */
	public bool AddActor(ObiActor actor, int numParticles){

		if (particleToActor == null || actor == null)
			return false;

		int[] allocated = new int[numParticles];
		int allocatedCount = 0;

		for (int i = 0; i < maxParticles && allocatedCount < numParticles; i++){
			if (particleToActor[i] == null){
				allocated[allocatedCount] = i;
				allocatedCount++;
			}
		}

		// could not allocate enough particles.
		if (allocatedCount < numParticles){
			return false; 
		}

		allocatedParticleCount += numParticles;

		// store per-particle actor reference:
		for (int i = 0; i < numParticles; ++i)
			particleToActor[allocated[i]] = new ParticleInActor(actor,i);

		// set the actor particle indices.
		actor.particleIndices = allocated;

        // Add the actor to the actor list:
		actors.Add(actor);

		// Update active particles. Update materials, in case the actor has a new one.
		UpdateActiveParticles();  
		UpdateEmitterMaterials();
       
		return true;

	}

	/**
	 * Removes an actor from this solver. Returns the index that was occupied by the actor in the actor list, or -1 if it was not managed by this solver.
	 */
	public int RemoveActor(ObiActor actor){
		
		if (particleToActor == null || actor == null)
			return -1;

		// Find actor index in our actors array:
		int index = actors.IndexOf(actor);

		// If we are in charce of this actor indeed, perform all steps necessary to release it.
		if (index > -1){

			allocatedParticleCount -= actor.particleIndices.Length;

			for (int i = 0; i < actor.particleIndices.Length; ++i)
				particleToActor[actor.particleIndices[i]] = null;
	
			actors.RemoveAt(index); 
	
			// Update active particles. Update materials, in case the actor had one.
			UpdateActiveParticles(); 
			UpdateEmitterMaterials();

		}
		
		return index;
	}

	/**
	 * Updates solver parameters, sending them to the Oni library.
	 */
	public void UpdateParameters(){

		Oni.SetSolverParameters(oniSolver,ref parameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Distance,ref distanceConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Bending,ref bendingConstraintParameters);
	
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.ParticleCollision,ref particleCollisionConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Collision,ref collisionConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Density,ref densityConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Skin,ref skinConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Volume,ref volumeConstraintParameters);
		
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Tether,ref tetherConstraintParameters);
	
		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Pin,ref pinConstraintParameters);

		Oni.SetConstraintGroupParameters(oniSolver,(int)Oni.ConstraintType.Stitch,ref stitchConstraintParameters);

    }

	/**
	 * Updates the active particles array.
	 */
	public void UpdateActiveParticles(){

		int numActive = 0;

		for (int i = 0; i < actors.Count; ++i){

			ObiActor currentActor = actors[i];

			if (currentActor.isActiveAndEnabled){
				for (int j = 0; j < currentActor.particleIndices.Length; ++j){
					if (currentActor.active[j]){
						activeParticles[numActive] = currentActor.particleIndices[j];
						numActive++;
					}
				}
			}
		}	

		Oni.SetActiveParticles(oniSolver,activeParticles,numActive);

	}

	public void UpdateEmitterMaterials(){

		// reset the emitter material list:
		emitterMaterials = new List<ObiEmitterMaterial>(){defaultFluidMaterial};

		// Setup all materials used by particle actors:
		foreach (ObiActor actor in actors){
			
			ObiEmitter em = actor as ObiEmitter;
				if (em == null) continue;

			int materialIndex = 0;

			if (em.EmitterMaterial != null){

				materialIndex = emitterMaterials.IndexOf(em.EmitterMaterial);

				// if the material has not been considered before:
				if (materialIndex < 0){

					materialIndex = emitterMaterials.Count;
					emitterMaterials.Add(em.EmitterMaterial);
			
					//keep an eye on material changes:
					em.EmitterMaterial.OnChangesMade += emitterMaterial_OnChangesMade;
				}
			}
			
			// Update material index for all actor particles:
			for(int i = 0; i < actor.particleIndices.Length; i++){
				fluidMaterialIndices[actor.particleIndices[i]] = materialIndex;
			}
		}

		Oni.SetFluidMaterialIndices(oniSolver,fluidMaterialIndices,fluidMaterialIndices.Length,0);
		Oni.FluidMaterial[] mArray = emitterMaterials.ConvertAll<Oni.FluidMaterial>(a => a.GetEquivalentOniMaterial(parameters.mode)).ToArray();
		Oni.SetFluidMaterials(oniSolver,mArray,mArray.Length,0);
	}

	private void emitterMaterial_OnChangesMade (object sender, ObiEmitterMaterial.MaterialChangeEventArgs e)
	{
		ObiEmitterMaterial material = sender as ObiEmitterMaterial; 
		int index = emitterMaterials.IndexOf(material);
		if (index >= 0){
			Oni.SetFluidMaterials(oniSolver,new Oni.FluidMaterial[]{material.GetEquivalentOniMaterial(parameters.mode)},1,index);
		}
	}

	public void AccumulateSimulationTime(float dt){
		Oni.AddSimulationTime(oniSolver,dt);
	}

	public void ResetSimulationTime(){
		Oni.ResetSimulationTime(oniSolver);
	}

	public void SimulateStep(float stepTime){

		if (IsUpdating){

			Oni.ClearDiffuseParticles(oniSolver);
	
			if (OnStepBegin != null)
				OnStepBegin(this,null);
	
			foreach(ObiActor actor in actors)
	            actor.OnSolverStepBegin();
	
			// Update Oni skeletal mesh skinning after updating animators:
			Oni.UpdateSkeletalAnimation(oniSolver);
	
			// Trigger event right after actors have fixed their particles in OnSolverStepBegin.
			if (OnFixedParticlesUpdated != null)
				OnFixedParticlesUpdated(this,null);
	
			ObiArbiter.StepStart();
	
			// Update the solver (this is internally split in tasks so multiple solvers can be updated in parallel)
			Oni.UpdateSolver(oniSolver, substeps, stepTime/(float)substeps); 
	
			// Wait here for all other solvers to finish, if we are the last solver to call this.
			ObiArbiter.WaitForAllSolvers();

		}

		// at the end of every physics step, grab the solver's transform:
		lastTransform = transform.localToWorldMatrix;

	} 

	public void EndFrame(float stepTime){

		foreach(ObiActor actor in actors)
            actor.OnSolverPreInterpolation();

		if (OnBeforePositionInterpolation != null)
			OnBeforePositionInterpolation(this,null);

		Oni.ApplyPositionInterpolation(oniSolver, stepTime);

		// if we need to get renderable positions back from the solver:
		if (renderablePositionsClients > 0)
		{
			Oni.GetRenderableParticlePositions(oniSolver, renderablePositions, renderablePositions.Length,0);

			// convert positions to world space if they are expressed in solver space:
			if (simulateInLocalSpace){
				Matrix4x4 l2wTransform = transform.localToWorldMatrix;
				for (int i = 0; i < renderablePositions.Length; ++i){
					renderablePositions[i] = l2wTransform.MultiplyPoint3x4(renderablePositions[i]);
				}
			}
		}

		// Trigger fluid update:
		TriggerFluidUpdateEvents();

		if (OnBeforeActorsFrameEnd != null)
			OnBeforeActorsFrameEnd(this,null);

		UpdateVisibility();
		
		foreach(ObiActor actor in actors)
            actor.OnSolverFrameEnd();

	}

	private void TriggerFluidUpdateEvents(){

		int numFluidParticles = Oni.GetConstraintCount(oniSolver,(int)Oni.ConstraintType.Density);

		if (OnFluidUpdated != null){

			fluidArgs.indices.SetCount(numFluidParticles);
			fluidArgs.vorticities.SetCount(maxParticles);
			fluidArgs.densities.SetCount(maxParticles);

			if (numFluidParticles > 0){
				Oni.GetActiveConstraintIndices(oniSolver,fluidArgs.indices.Data,numFluidParticles,(int)Oni.ConstraintType.Density);
				Oni.GetParticleVorticities(oniSolver,fluidArgs.vorticities.Data,maxParticles,0);
				Oni.GetParticleDensities(oniSolver,fluidArgs.densities.Data,maxParticles,0);
			}

			OnFluidUpdated(this,fluidArgs);
		}
	}

	private void TriggerCollisionEvents(){
	
		int numCollisions = Oni.GetConstraintCount(oniSolver,(int)Oni.ConstraintType.Collision);

		if (OnCollision != null){

			collisionArgs.contacts.SetCount(numCollisions);

			if (numCollisions > 0)
				Oni.GetCollisionContacts(oniSolver,collisionArgs.contacts.Data,numCollisions);

			OnCollision(this,collisionArgs);

		}
	}

	private bool AreBoundsValid(Bounds bounds){
		return !(float.IsNaN(bounds.center.x) || float.IsInfinity(bounds.center.x) ||
			     float.IsNaN(bounds.center.y) || float.IsInfinity(bounds.center.y) ||
			     float.IsNaN(bounds.center.z) || float.IsInfinity(bounds.center.z));
	}

	/**
	 * Checks if any particle in the solver is visible from at least one camera. If so, sets isVisible to true, false otherwise.
	 */
	public void UpdateVisibility(){

		Vector3 min = Vector3.zero, max = Vector3.zero;
		Oni.GetBounds(oniSolver,ref min, ref max);
		bounds.SetMinMax(min,max);

		if (AreBoundsValid(bounds)){

			Bounds wsBounds = simulateInLocalSpace ? bounds.Transform(transform.localToWorldMatrix) : bounds;

			foreach (Camera cam in Camera.allCameras){
	        	Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
	       		if (GeometryUtility.TestPlanesAABB(planes, wsBounds)){
					if (!isVisible){
						isVisible = true;
						foreach(ObiActor actor in actors)
							actor.OnSolverVisibilityChanged(isVisible);
					}
					return;
				}
			}
		}

		if (isVisible){
			isVisible = false;
			foreach(ObiActor actor in actors)
				actor.OnSolverVisibilityChanged(isVisible);
		}
	}
    
    void Update(){

		if (!Application.isPlaying)
			return;

		if (OnFrameBegin != null)
			OnFrameBegin(this,null);

		foreach(ObiActor actor in actors)
            actor.OnSolverFrameBegin();

		if (IsUpdating && simulationOrder != SimulationOrder.LateUpdate){
			AccumulateSimulationTime(Time.deltaTime);
		}

	}

	IEnumerator RunLateFixedUpdate() {
         while (true) {

             yield return new WaitForFixedUpdate();

			 if (Application.isPlaying && simulationOrder == SimulationOrder.AfterFixedUpdate)
             	SimulateStep(Time.fixedDeltaTime);

         }
     }

    void FixedUpdate()
    {
		if (Application.isPlaying && simulationOrder == SimulationOrder.FixedUpdate)
			SimulateStep(Time.fixedDeltaTime);
    }

	public void AllSolversStepEnd()
	{
		// Trigger solver events:
		TriggerCollisionEvents();
	
		foreach(ObiActor actor in actors)
       	 	actor.OnSolverStepEnd();

		if (OnStepEnd != null)
			OnStepEnd(this,null);

	}

	private void LateUpdate(){

		if (Application.isPlaying && simulationOrder == SimulationOrder.LateUpdate){
			 smoothDelta = Mathf.Lerp(Time.deltaTime,smoothDelta,0.95f);
			 AccumulateSimulationTime(smoothDelta);
             SimulateStep(smoothDelta);
		}

		if (!Application.isPlaying)
			return;
   
		EndFrame (simulationOrder == SimulationOrder.LateUpdate ? smoothDelta : Time.fixedDeltaTime);

		if (OnFrameEnd != null)
			OnFrameEnd(this,null);
	}

}

}
