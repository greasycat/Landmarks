using UnityEngine;
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

    
    void OnTriggerEnter(Collider other)
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


}