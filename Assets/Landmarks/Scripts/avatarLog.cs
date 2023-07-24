using UnityEngine;
using System.Collections;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class avatarLog : MonoBehaviour
{

	[HideInInspector] public bool navLog = false;
	//private Transform avatar;
	private Transform body;
	private Transform head;

	private dbLog log;
	private Experiment manager;
	private LM_PlayerController controller;

	private string location = "CURRENTLY NOWHERE";
	private string previousLocation = "PREVIOUSLY Nowhere";
	private string KeyPress;
	private string TargetObjectVisibility;
	private GameObject[] targetObjects;
	//private Experiment vrInput;
	private ExperimentTask vrEnabled;
	private SteamVR_Input_ActionSet_landmarks vrInput;

	void Start()
	{
		manager = FindObjectOfType<Experiment>().GetComponent<Experiment>();
		log = manager.dblog;
		//avatar = transform;

		controller = manager.player.GetComponent<LM_PlayerController>();
		body = controller.collisionObject.transform;
		head = controller.cam.transform;
		targetObjects = GameObject.FindGameObjectsWithTag("Target");
		// set up vrInput if we're using VR
		if (vrEnabled) vrInput = SteamVR_Input.GetActionSet<SteamVR_Input_ActionSet_landmarks>(default);

	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha0))
		{
			Debug.Log("Someone is trying to erase us from space-time!");
			location = "PARADOX Nowhere";
		}

		if (previousLocation != location)
		{
			Debug.Log("We have left " + previousLocation + ". Now entering " + location);
		}

		// Checking if player pressed button to indicate that they've fully learned locations of all target objects 
		if (Input.GetKey(KeyCode.Space))
		{
			KeyPress = "True";
			Debug.Log("Participant thinks they've learned entire layout");
		}
		else
		{
			KeyPress = "False";
		}

		if (vrEnabled)
		{
			if (vrInput.TriggerButton.GetStateDown(SteamVR_Input_Sources.Any))
			{
				KeyPress = "True";
				Debug.Log("Participant thinks they've learned entire layout");
			}

			else
			{
				KeyPress = "False";
			}
		}

	}
	// Update is called once per frame
	void FixedUpdate()
	{
	
		foreach (GameObject targetObject in targetObjects)
        {
			IsVisibleFromCamera(targetObject, controller.cam);
			
		}
			

		// fixmefixmefixme Checking if target objects are visible as player moves around; 

		//GameObject[] targetObjects = GameObject.FindGameObjectsWithTag("Target");

		//foreach (GameObject targetObject in targetObjects)
		//{
		//bool isVisible = IsVisibleFromCamera(targetObject, controller.cam); //uses function defined below

		//if (isVisible)
		//{
		//TargetObjectVisibility = targetObject.name + "is visible";
		//Debug.Log(targetObject.name + "is visible");
		//}
		//else
		//{
		//TargetObjectVisibility = "N/A";
		//Debug.Log("N/A");
		//}


		// Log the name of the tracked object, it's body position, body rotation, and camera (head) rotation
		if (navLog)
		{
			Debug.Log("---------------------------- " + location + " -------------------------------");
			//print("AVATAR_POS	" + "\t" +  avatar.position.ToString("f3") + "\t" + "AVATAR_Body " + "\t" +  cameraCon.localEulerAngles.ToString("f3") +"\t"+ "AVATAR_Head " + cameraRig.localEulerAngles.ToString("f3"));
			log.log("Avatar: \t" + controller.name + "\t" +
					"Body Position (xyz): \t" + body.position.x + "\t" + body.position.y + "\t" + body.position.z + "\t" +
					"Body Rotation (xyz): \t" + body.eulerAngles.x + "\t" + body.eulerAngles.y + "\t" + body.eulerAngles.z + "\t" +
					"Camera Position (xyz): \t" + head.position.x + "\t" + head.position.y + "\t" + head.position.z + "\t" +
					"Camera Rotation   (xyz): \t" + head.eulerAngles.x + "\t" + head.eulerAngles.y + "\t" + head.eulerAngles.z + "\t" +
					"Location (Object/Hallway): \t" + location + "\t" +
					"Keypress(True/False): \t" + KeyPress + "\t" +
					"TargetObjectVisibility: \t" + TargetObjectVisibility + "\t"
					, 1);

		}
	}

    private void OnCollisionEnter(Collision collision)
    {
		if (collision.gameObject.tag == "LocationColliders")
		{
			location = collision.gameObject.name;
			Debug.Log("COLLIDER IS TRIGGERING!!!! At " + location);
		}
	}

    private void OnCollisionStay(Collision collision)
    {
		if (collision.gameObject.tag == "LocationColliders")
		{
			if (location == "ERRONEOUS Nowhere")
			{
				location = collision.gameObject.name;
				Debug.LogWarning("Fixing an error in current location assignment; everything is okay!");
			}
			Debug.Log("STILL at " + collision.gameObject.name);
		}
	}

    private void OnCollisionExit(Collision collision)
    {
		if (collision.gameObject.tag == "LocationColliders")
		{

			location = "EXITED TO Nowhere";
			previousLocation = collision.gameObject.name;
			Debug.Log("COLLIDER IS TRIGGERING!!!!");
		}
	}

    void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.tag == "LocationColliders")
		{
			location = other.gameObject.name;
			Debug.Log("COLLIDER IS TRIGGERING!!!! At " + location);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.tag == "LocationColliders")
		{
			if (location == "ERRONEOUS Nowhere")
			{
				location = other.gameObject.name;
				Debug.LogWarning("Fixing an error in current location assignment; everything is okay!");
			}
			Debug.Log("STILL at " + other.name);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.tag == "LocationColliders")
		{

			location = "EXITED TO Nowhere";
			previousLocation = other.gameObject.name;
			Debug.Log("COLLIDER IS TRIGGERING!!!!");
		}
	}

	private bool IsVisibleFromCamera(GameObject targetObject, Camera camera)
	{
		Renderer renderer = targetObject.GetComponent<Renderer>();
		int layerMaskTarget = 1 << 8; //1 << LayerMask.NameToLayer("Target");
		int layerMaskProp = 1 << 10; //LayerMask.NameToLayer("Prop");
		var targetObjectPosition = targetObject.transform.position;
		var cameraPosition = camera.transform.position;
		var direction = targetObjectPosition - cameraPosition;
		float angle = Vector3.Angle(direction, camera.transform.forward);
		float fieldOfViewAngle = camera.fieldOfView;

		if (angle < fieldOfViewAngle * 0.5f)
		{
			RaycastHit hit;
			if (Physics.Raycast(cameraPosition, direction.normalized, out hit, Mathf.Infinity, layerMaskTarget))
			{
				//if (hit.collider.gameObject.CompareTag("Target") && hit.collider.gameObject == targetObject)
				{
					Debug.Log("HITTING TARGET!!!!!!");
					bool isVisible = IsVisible(renderer, hit);
					if (isVisible)
					{
						TargetObjectVisibility = targetObject.name + " is visible";
						Debug.Log(targetObject.name + " is visible");
						return true;
					}
				}
			}
		}

		TargetObjectVisibility = "N/A";
		Debug.Log("N/A");
		return false;
	}

	private bool IsVisible(Renderer renderer, RaycastHit hit)
	{
		if (renderer != null && renderer.isVisible)
		{
			var obstructedObjects = Physics.RaycastAll(hit.point, hit.point - hit.collider.transform.position, Mathf.Infinity);
			foreach (var obstructedObject in obstructedObjects)
			{
				if (obstructedObject.collider.gameObject != hit.collider.gameObject && obstructedObject.collider.gameObject.CompareTag("Target"))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}


	//function to check if target object are truly visible in camera's field of view, by checking if object's renderer bounds intersect with camera's field of view 
	//private bool IsVisibleFromCamera(GameObject targetObject, Camera camera)
	//{
	//	Renderer renderer = targetObject.GetComponent<Renderer>();
	//	int layerMaskTarget = 8;
	//	int layerMaskProp = 10;
	//	var targetObjectPosition = targetObject.transform;
	//	var cameraPosition = camera.transform;
	//	var direction = targetObjectPosition.position - cameraPosition.position;
	//	float angle = Vector3.Angle(direction, transform.forward);
	//	float fieldOfViewAngle = 90f;
	//	//bool isHit = false;

	//	//if (angle < fieldOfViewAngle * 0.5f)
	//	//{
	//	RaycastHit hit;

	//	if (Physics.Raycast(transform.position, direction.normalized, out hit, layerMaskTarget)) //| layerMaskProp)) //&& (gameObject.tag == "Target"))
	//	{
	//		//if (hit.collider.gameObject.layer == layerMaskTarget)
	//		//{
	//		Debug.Log("HITTING TARGET!!!!!!");
	//		bool isVisible = true; //uses function defined below
	//		TargetObjectVisibility = targetObject.name + "is visible";
	//		Debug.Log(targetObject.name + "is visible");
	//		//}
	//	}
	//	else
	//	{
	//		TargetObjectVisibility = "N/A";
	//		Debug.Log("N/A");
	//	}
	//	return (false);

	//}	









	//}


}
//Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
//return GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
//}	






/* using UnityEngine;
using System.Collections;

public class avatarLog : MonoBehaviour {

	[HideInInspector] public bool navLog = false;
	private Transform avatar;
	private Transform cameraCon;
	private Transform cameraRig;

	private GameObject experiment;
	private dbLog log;
	private Experiment manager;
	
	public GameObject player;
	public GameObject camerarig;

	private string location = "Nowhere";
	private string previousLocation = "Nowhere";
	private string KeyPress;

	void Start () {

		cameraCon =player.transform as Transform;
		cameraRig =camerarig.transform as Transform;

		experiment = GameObject.FindWithTag ("Experiment");
		manager = experiment.GetComponent("Experiment") as Experiment;
		log = manager.dblog;
		avatar = transform;
		
	}

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
			Debug.Log("Someone is trying to erase us from space-time!");
			location = "Nowhere";
        }

        if (previousLocation != location)
        {
			Debug.Log("We have left " + previousLocation + ". Now entering " + location);
        }

    }

    // Update is called once per frame
    void FixedUpdate () {
		// Checking if player pressed button 
		if (Input.GetKey(KeyCode.Space))
		{
			KeyPress= "True";
		}
		else
		{
			KeyPress= "False";
		}


        // Log the name of the tracked object, it's body position, body rotation, and camera (head) rotation
		if (navLog){
            //print("AVATAR_POS	" + "\t" +  avatar.position.ToString("f3") + "\t" + "AVATAR_Body " + "\t" +  cameraCon.localEulerAngles.ToString("f3") +"\t"+ "AVATAR_Head " + cameraRig.localEulerAngles.ToString("f3"));
            log.log("Avatar: \t" + avatar.name + "\t" +
                    "Position (xyz): \t" + cameraCon.position.x + "\t" + cameraCon.position.y + "\t" + cameraCon.position.z + "\t" +
                    "Rotation (xyz): \t" + cameraCon.eulerAngles.x + "\t" + cameraCon.eulerAngles.y + "\t" + cameraCon.eulerAngles.z + "\t" +
                    "Camera   (xyz): \t" + cameraRig.eulerAngles.x + "\t" + cameraRig.eulerAngles.y + "\t" + cameraRig.eulerAngles.z + "\t" + "Location (Object/Hallway): \t" + location + "\t" + "Keypress(True/False): \t" + KeyPress + "\t"
                    , 1);
        }
	}
 */

/* void OnTriggerEnter(Collider other)
{
	if (other.gameObject.tag == "LocationColliders")
	{
		location = other.gameObject.name;
		Debug.Log("COLLIDER IS TRIGGERING!!!!");
	}
}

private void OnTriggerStay(Collider other)
{
	if (other.gameObject.tag == "LocationColliders")
	{
		if (location == "Nowhere")
		{
			location = other.gameObject.name;
			Debug.LogWarning("Fixing an error in current location assignment; everything is okay!");
		}
		Debug.Log("STILL at " + other.name);
	}
}

private void OnTriggerExit(Collider other)
{
	if (other.gameObject.tag == "LocationColliders")
	{

		location = "Nowhere";
		previousLocation = other.gameObject.name;
		Debug.Log("COLLIDER IS TRIGGERING!!!!");
	}
}


} */