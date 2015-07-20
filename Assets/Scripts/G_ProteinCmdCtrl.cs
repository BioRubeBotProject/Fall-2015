﻿using UnityEngine;
using System.Collections;

public class G_ProteinCmdCtrl : MonoBehaviour
{

	private static float _speed = 5f;	
	private static float _max = 100f;
	private static float _min = -100f;
		
	public GameObject GDP, childGDP = null;	// for use creating a child of this object
	public ParticleSystem DestructionEffect;

	private bool docked = false;		// g-protein position = receptor phosphate position?
	private bool knownOffset = false;	// is the target phosphate left or right of receptor?
	private bool spotted = false;		// found receptor phosphate?
	private bool nowHaveGTP = false;
	private bool stillHaveGDP = true;	// is the phosphate still attached?
	private bool targeting = false;		// g-protein targeting phosphate?

	private float delay;
	private float deltaDistance;		// distance between the receptor phosphate and the g-protein moving towards it
	private float randomX, randomY;		// random number between MIN/MAX_X and MIN/MAX_Y

	private Transform closestG_Protein;	// the closest g-protein to the closest receptor phosphate
	private Transform closestTarget;	// the closest receptor phosphate in relation to this g-protein

	private Vector2 randomDirection;	// new direction vector

	private Vector3 dockingPosition;	// where to station the g-protein at docking
	private Vector3 lastPosition;		// previous position while moving to phosphate

	private void Start()
	{
		lastPosition = transform.position;
		
		//Instantiate a GDP child to tag along
		childGDP = (GameObject)Instantiate (GDP, transform.position + new Vector3(0.86f, 0.13f, 0), Quaternion.identity);
		childGDP.GetComponent<CircleCollider2D> ().enabled = false;
		childGDP.GetComponent<Rigidbody2D> ().isKinematic = true;
		childGDP.transform.parent = transform;
		//transform.GetChild (2).GetComponent<SpriteRenderer> ().color = Color.white;
		//transform.GetChild (1).GetComponent<SpriteRenderer> ().color = Color.blue;
	}


	private void FixedUpdate()
	{

		if (Time.timeScale > 0) {
			if (!docked && !nowHaveGTP) { // G-protein is not docked and does not have a GTP 
				if (!targeting) { // no target in range
					delay = 0;  // reset time delay
					spotted = GameObject.FindGameObjectWithTag ("receptorPhosphate");// any targets?
					if (spotted) {

						closestTarget = FindClosest (transform, "receptorPhosphate"); // find my closest target
						closestG_Protein = FindClosest (closestTarget, "G_Protein");// anybody closer?
						if (gameObject.transform == closestG_Protein) // if I'm closest
							LockOn(); // call dibs
					}/* end if spotted */
					else
						Roam ();// no target spotted			
				} /* end if !targeting */
				else { /*target acquired*/ 
					if (!knownOffset) { // is my target dock to the left or right?
						dockingPosition = GetOffset();
						knownOffset = true;
					}/* end if unk */
					else { // I know where I'm going, give time for the ATP to clear area and destruct
						if (delay < 5) //wait 5 seconds before proceeding to target
						{
							delay += Time.deltaTime;
							Roam ();
						}
						else
						{
							docked = ProceedToTarget();// proceed to target
							if (docked) delay = 0;
						}

					}/* end proceed to target*/
				} /* end target acquired */
			}/* end not docked and does not have GTP */
			else if (stillHaveGDP)// now docked -> release GDP
			{
				Cloak();//retag objects for future reference
				StartCoroutine (ReleaseGDP ());
				StartCoroutine (Explode ()); //Destroy GDP
			} 
			else if (transform.tag == "occupiedG_Protein"){
				Debug.Log (delay);
				if ((delay += Time.deltaTime) > 5) //wait at least 5 seconds before proceeding to target
					Undock ();
			}
			else Roam ();
		}/* end running (timeScale > 0) */
	}/* end Fixed Update */	




	private void Roam()
	{
		randomX = Random.Range (_min, _max); //get random x vector coordinate
		randomY = Random.Range (_min, _max); //get random y vector coordinate
		//apply a force to the object in direction (x,y):
		GetComponent<Rigidbody2D> ().AddForce (new Vector2(randomX, randomY), ForceMode2D.Force);
	}/* end Roam */



	private Transform FindClosest(Transform my, string objTag)
	{
		float distance = Mathf.Infinity; //initialize distance to 'infinity'
		
		GameObject[] gos; //array of gameObjects to evaluate
		GameObject closestObject = null;
		//populate the array with the objects you are looking for
		gos = GameObject.FindGameObjectsWithTag(objTag);
		
		//find the nearest object ('objectTag') to me:
		foreach (GameObject go in gos)
		{	
			//calculate square magnitude between objects
			Vector3 diff = my.position - go.transform.position;
			float curDistance = diff.sqrMagnitude;
			if (curDistance < distance)
			{
				closestObject = go; //update closest object
				distance = curDistance;//update closest distance
			}
		}
		return closestObject.transform;
	}/* end FindClosest */




	private void LockOn()
	{
		targeting = true;
		transform.tag = "targeting";
		closestTarget.tag = "target";
	}/* end LockOn */

	private Vector3 GetOffset()
	{
		if (closestTarget.GetChild(0).tag == "left")
		{
			transform.GetChild(0).tag = "left"; //tag left G-Protein for GTP reference
			return closestTarget.position + new Vector3 (-0.9f, -0.16f, 0);
		}
		else
			return closestTarget.position + new Vector3 (0.9f, -0.16f, 0);
	}/* end GetOffset */

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
		if (deltaDistance < _speed * .5f) {
			transform.GetComponent<BoxCollider2D> ().enabled = false;
			transform.GetComponent<Rigidbody2D> ().isKinematic = true;
		}
		if (deltaDistance < _speed * Time.deltaTime){
			transform.position = dockingPosition;
			if (closestTarget.GetChild(0).tag == "left")
				transform.Rotate(180f,0,180f); //orientate protein for docking
		}//end if close enough
		return (transform.position==dockingPosition);
	}/* end ProceedToTarget */

	private void Cloak()
	{
		closestTarget.tag = "OccupiedReceptor";
		transform.tag = "DockedG_Protein";
	} /* end cloak */

	private IEnumerator ReleaseGDP ()
	{
		stillHaveGDP = false;
		yield return new WaitForSeconds (3f);

		childGDP.transform.parent = null;
		childGDP.transform.GetComponent<Rigidbody2D> ().isKinematic = false;
		childGDP.transform.GetComponent<CircleCollider2D> ().enabled = true;
		//transform.GetChild (1).GetComponent<SpriteRenderer> ().color = Color.red;
		//transform.GetChild (3).GetComponent<SpriteRenderer> ().color = Color.red;
	} /* end ReleaseGDP */

	private void Undock()
	{
		//yield return new WaitForSeconds (3f);
		transform.GetComponent<Rigidbody2D>().isKinematic = false;
		transform.GetComponent<BoxCollider2D>().enabled = true;
		docked = false;
		targeting = false;
		nowHaveGTP = true;
		transform.tag = "freeG_Protein";
		closestTarget.tag = "receptorPhosphate";
		closestTarget = null;
		//transform.GetChild (1).GetComponent<SpriteRenderer> ().color = Color.gray;
	}/* end Undock */


	private IEnumerator Explode()
	{
		yield return new WaitForSeconds (6f);
		//Instantiate our one-off particle system
		ParticleSystem explosionEffect = Instantiate(DestructionEffect) as ParticleSystem;
		explosionEffect.transform.position = childGDP.transform.position;
		
		//play it
		explosionEffect.loop = false;
		explosionEffect.Play();
		
		//destroy the particle system when its duration is up, right
		//it would play a second time.
		Destroy(explosionEffect.gameObject, explosionEffect.duration);
		
		//destroy our game object
		Destroy(childGDP.gameObject);
	}/* end Explode */
}