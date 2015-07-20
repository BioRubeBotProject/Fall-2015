﻿using UnityEngine;
using System.Collections;

using UnityEngine;
using System.Collections;

public class GTP_CmdCtrl : MonoBehaviour
{

	static float _speed = 5f;	
	static float _max = 150f;
	static float _min = -150f;

	
	public ParticleSystem DestructionEffect;
	
	private bool docked = false;		// g-protein position = receptor phosphate position?
	private bool knownOffset = false;	// is the target phosphate left or right of receptor?
	private bool spotted = false;		// found receptor phosphate?
	private bool targeting = false;		// g-protein targeting phosphate?
	
	private float delay = 0f;
	private float holdTime = 250f;		// fixed update runs every 20ms, hold time = 5 seconds
	private float deltaDistance;		// distance between the receptor phosphate and the g-protein moving towards it
	private float randomX, randomY;		// random number between MIN/MAX_X and MIN/MAX_Y
	
	private Transform closestTarget;	// the closest receptor phosphate in relation to this g-protein
	private Transform closestGTP;		// the closest g-protein to the closest receptor phosphate
	
	private Vector2 randomDirection;	// new direction vector
	private Vector3 dockingPosition;	// where to station the g-protein at docking
	private Vector3 lastPosition;		// previous position while moving to phosphate




	private void Start()/*constructor*/
	{
		//initialize:
		closestTarget = transform;
		closestGTP = transform;
		lastPosition = transform.position;			
	}
	public void FixedUpdate() /*main*/
	{
		if (Time.timeScale > 0 && !docked)
		{ 
			if (!targeting)
			{
				delay = 0;  //reset time delay ()
				spotted = GameObject.FindGameObjectWithTag ("DockedG_Protein");//search for a target object (objectA)
				if (spotted)
				{
					closestTarget = FindClosest ("DockedG_Protein"); //find my closest target
					closestGTP = FindClosest ("GTP");//find the closest object (of my type) to the target
					if (transform == closestGTP) 
						LockOn(); //if I'm closest, 'call dibs'
				}//end if spotted
				
				else Roam ();//no target spotted
			}//end if !targeting
			
			else /*target acquired*/
			{
				if (!knownOffset)/*is my target dock to the left or right*/
				{
					dockingPosition = GetOffset ();
					knownOffset = true;
				}/* end if unk */
				else
				{
					//Debug.Log ("knownOffset: " + knownOffset);
					if ((delay+=Time.deltaTime) < 5) //wait 5 seconds before moving toward target
							Roam (); 
					else
						//Debug.Log("move");
						docked = ProceedToTarget ();//head towards and dock with target
				}
			}//end targeting
		}//end running and not docked
		
		else if (transform.tag != "dockedGTP") /*docked*/
		{ 
			//throw in another position reset to compensate for any late hits
			transform.position = GetOffset();
			Cloak();//retag objects for future reference
		}
	}//END FIXED UPDATE


	private void Roam()
	{
		randomX = Random.Range (_min,_max); //get random x vector coordinate
		randomY = Random.Range (_min, _max); //get random y vector coordinate
		//apply a force to the object in direction (x,y):
		GetComponent<Rigidbody2D> ().AddForce (new Vector2(randomX, randomY), ForceMode2D.Force);
	}//END ROAM



	private Transform FindClosest(string objTag)
	{
		//Debug.Log ("Enter FindClosest - objTag = " + objTag);
		float distance = Mathf.Infinity; //initialize distance to 'infinity'
		
		GameObject[] gos; //array of gameObjects to evaluate
		GameObject closestObject = null;
		//populate the array with the objects you are looking for
		gos = GameObject.FindGameObjectsWithTag(objTag);
		
		//find the nearest object ('objectTag') to me:
		foreach (GameObject go in gos)
		{	
			//calculate square magnitude between objects
			Vector3 diff = transform.position - go.transform.position;
			float curDistance = diff.sqrMagnitude;
			if (curDistance < distance)
			{
				closestObject = go; //update closest object
				distance = curDistance;//update closest distance
			}
		}
		//Debug.Log ("Exit - closestObject.tag = "+closestObject.tag);
		return closestObject.transform;
	}//END FIND CLOSEST




	private void LockOn()
	{
		targeting = true;
		transform.tag = "targetingG_protein";
		closestTarget.tag = "G_ProteinTarget";
	}



	private Vector3 GetOffset()
	{	

		//Debug.Log (closestTarget.GetChild (0).tag);
		//Debug.Log (closestTarget.position);
		if (closestTarget.GetChild(0).tag == "left")
			return closestTarget.position + new Vector3 (-0.86f, 0.13f, 0);
		else
			return closestTarget.position + new Vector3 (0.86f, 0.13f, 0);
	}




	private bool ProceedToTarget()
	{
		//Unity manual says if the distance between the two objects is < _speed * Time.deltaTime,
		//protein position will equal docking...doesn't seem to work, so it's hard coded below
		transform.position = Vector2.MoveTowards (transform.position, dockingPosition, _speed *Time.deltaTime);
		
		if (!docked && Vector2.Distance (transform.position, lastPosition) < _speed * Time.deltaTime)
			Roam ();//if I didn't move...I'm stuck.  Give me a push (roam())
		lastPosition = transform.position;//breadcrumb trail
		//check to see how close to the phosphate and disable collider when close
		deltaDistance = Vector3.Distance (transform.position, dockingPosition);
		//once in range, station object at docking position
		if (deltaDistance < _speed * Time.deltaTime) {
			transform.GetComponent<CircleCollider2D> ().enabled = false;
			transform.GetComponent<Rigidbody2D>().isKinematic = true;
			transform.position = dockingPosition;
			transform.parent = closestTarget;
		}//end if close enough
		return (transform.position==dockingPosition);
	}//END MOVE TO RECEPTOR

	private void Cloak()
	{
		//Debug.Log (closestTarget.tag);
		closestTarget.tag = "occupiedG_Protein";
		//Debug.Log (closestTarget.tag);
		transform.tag = "dockedGTP";
	}

}