using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LM_Compass : MonoBehaviour
{
    public GameObject pointer;
    public float rotationSpeedMultiplier;
    public bool interactable;
    public Transform playerCamera; // Assign your desktop player camera to this variable
    public Transform vRPlayerCamera; // Assign your VR player camera to this variable

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (interactable)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                pointer.transform.Rotate(new Vector3(0f, -1 * rotationSpeedMultiplier * Time.deltaTime, 0f), Space.Self);
                
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                pointer.transform.Rotate(new Vector3(0f, rotationSpeedMultiplier * Time.deltaTime, 0f), Space.Self);
                
            }
        }

        // Making pointer point in the direction where the player is facing 
        if (playerCamera != null)
        {
            // Get the rotation of the player camera
            Quaternion cameraRotation = playerCamera.rotation;

            // Keep the x and z-axis coordinates fixed at zero, only grab rotation values for y axis;
            Vector3 newRotation = new Vector3(0, cameraRotation.eulerAngles.y, 0);

            // Set the rotation of the pointer to match the camera's rotation (only in y axis)
            pointer.transform.rotation = Quaternion.Euler(newRotation); 
        }
        else if (vRPlayerCamera != null)
        {
            // Get the rotation of the VR player camera
            Quaternion cameraRotation = vRPlayerCamera.rotation;

            // Keep the x and z-axis coordinates fixed at zero, only grab rotation values for y axis;
            Vector3 newRotation = new Vector3(0, cameraRotation.eulerAngles.y, 0);

            // Set the rotation of the pointer to match the camera's rotation (only in y axis)
            //Note: we are multiplying y axis rotation value by 180 to ensure that red end of compass faces player camera direction instead of opposite direction
            pointer.transform.rotation = Quaternion.Euler(newRotation); 
                //Quaternion.Euler(0, 0, 0);
        }

        else
        {
            Debug.LogError("either desktop or VR player camera is not assigned to the PointerController script!");
        }
    }


    // move the pointer back to zero degrees unless random is specified
    public void ResetPointer(bool random = false)
    {
        // store the vector3 rotation of the pointer in a temporary variable
        var temp = pointer.transform.localEulerAngles;

        // set the Y value of the desired Vector3 rotation (eulers)
        if (random)
        {
            temp.y = Random.Range(0f, 360f - Mathf.Epsilon); // random from 0 to 359.999999999
        }
        else temp.y = 0; // or zero

        // set the actual pointer transform's rotation to the temporary variable
        pointer.transform.localEulerAngles = temp;
    }
}
