/*
    LM Dummy
       
    Attached object holds task components that need to be effectively ignored 
    by Tasklist but are required for the script. Thus the object this is 
    attached to can be detected by Tasklist (won't throw error), but does nothing 
    except start and end.   

    Copyright (C) 2019 Michael J. Starrett

    Navigate by StarrLite (Powered by LandMarks)
    Human Spatial Cognition Laboratory
    Department of Psychology - University of Arizona   
*/

using System.Collections;
using System.Collections.Generic;
using Landmarks.Scripts.Progress;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
public class Orient: ExperimentTask

{
    [Header("Task-specific Properties")]
    //public TextAsset sopText;
    //public float secondsBeforeResponse = 0.0f; // how long before they can submit answer

    //private GameObject location; // standing at the...
    //private GameObject orientation; // facing the...
    //private GameObject target; // point to the...
    //private float startAngle; // where is the compass pointer at the start?
    //private float answer; // what should they say?
    //private float response; // what did they say?
    //private float signedError; // how far off were they to the right or left?
    //private float absError; // how far off were they regardless of direction?
    //private string formattedQuestion;
    //public TextAsset orientText;
    private bool oriented;
    public ObjectList listOfNavStarts;
    public RectTransform rectTransform;
    public float newWidth;
    public float newHeight;
    private Vector2 originalRectTransformSize;
    private Vector3 originalRectTransformScale;
    public Vector3 newScale;
    //public Vector2 distanceFromObj;

    public Transform currentObjectTransform;
    public Vector3 newHudPosition;

    public Vector3 oldHudPosition;

    //private float startTime; // mark the start of the task timer
    ////private float orientTime; // save the time to orient (SOP only)
    //private float responseTime; // save the time to answer

    public override void startTask()
    {
        TASK_START();
        // LEAVE BLANK
    }


    public override void TASK_START()
    {
        if (!manager) Start();
        base.startTask();


        //startTime = Time.time; // records the startTime
        // ---------------------------------------------------------------------
        // Configure the Task based on selected format -------------------------
        // ---------------------------------------------------------------------
        oldHudPosition = hud.transform.position;
        originalRectTransformSize = rectTransform.sizeDelta;
        originalRectTransformScale = rectTransform.localScale;
        rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
        rectTransform.localScale = newScale;
        // Prepare SOP hud and question
        hud.showEverything(); // configure the hud for the format
        manager.environment.transform.Find("filler_props").gameObject.SetActive(true);

        // finds everything in fillerprops and sets them active                                                                               // Find all GameObjects with the "filler_props" tag
        GameObject[] targetObjects = GameObject.FindGameObjectsWithTag("Target");

        // Loop through each GameObject
        foreach (GameObject prop in targetObjects)
        {
            // Set the GameObject active
            prop.SetActive(true);

            // Get the MeshRenderer component and enable it
            MeshRenderer meshRenderer = prop.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = true;
            }
        }
        //rectTransform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
        //distanceFromObj = new Vector2(listOfNavStarts.currentObject().transform.position.x - manager.environment.transform.position.x, listOfNavStarts.currentObject().transform.position.z - manager.environment.transform.position.z);

        
        // Get the current object's in listOfNavStarts transform
        currentObjectTransform = listOfNavStarts.currentObject().transform;

        // Calculate the new position for the HUD
        newHudPosition = currentObjectTransform.position + currentObjectTransform.forward * 0.15f;

        // Set the HUD's position
        hud.transform.position = newHudPosition;

        if (LM_Progress.Instance.CheckIfResumeNavigation())
        {
            hud.setMessage($"Orient yourself to the direction at the crashing moment \n\nWhen you are ready, press the trigger button to continue.");
        }
        else
        {
            hud.setMessage($"Orient yourself in front of the {listOfNavStarts.currentObject().name}.\n\nWhen you are ready, face the target object, and \npress the trigger button to begin.");
        }
        oriented = false;
        //formattedQuestion = string.Format(orientText.ToString(), target.name); // prepare to present the question
        //hud.SecondsToShow = 999999;
        //Vector3 hudPosition = location.transform.position;
        //Debug.Log($"HUD Position is {hudPosition}");
        //hud.hudRig.transform.position = hudPosition; // setting position
        //Debug.Log($"Reset HUD Position is {hudPosition}");
        //if (vrEnabled) { hud.hudNonEssentials.SetActive(false); }
    }


    public override bool updateTask()
    {
        hud.hudPanelOFF = 0.0f;
        //if (Time.time - startTime > secondsBeforeResponse) // don't let them submit until the wait time has passed
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (!oriented)
                {
                    oriented = true; // mark them oriented; at this point, we immediately move onto the "navigate" portion of the wayfinding task.
                    //hud.setMessage(formattedQuestion);
                    //startTime = Time.time; // reset the start clock for the answer portion
                    //return false; // don't end the trial
                    return true; // in my case, we DO want to return true; get out of this "orient" middle-man task, and move onto the navigation task
                }
                //else
                //{
                //    // record response time
                //    responseTime = Time.time - startTime;
                //    return true; // end trial
                //}
            }
            if (vrEnabled)
            {
                if (vrInput.TriggerButton.GetStateDown(Valve.VR.SteamVR_Input_Sources.Any))
                {
                    if (!oriented)
                    {
                        oriented = true; // mark them oriented
                        //hud.setMessage(formattedQuestion);
                        //startTime = Time.time;
                        //return false; // don't end the trial
                        return true; // in my case, we DO want to return true; get out of this "orient" middle-man task, and move onto the navigation task
                    }
                    //else
                    //{
                    //    // record response time
                    //    responseTime = Time.time - startTime;
                    //    return true; // end trial
                    //}
                }
            }
        }
        hud.ForceShowMessage(); // keep the question up
        return false;
    }


    public override void endTask()
    {
        TASK_END();
    }


    public override void TASK_END()
    {
        base.endTask();
        rectTransform.sizeDelta = originalRectTransformSize;
        rectTransform.localScale = originalRectTransformScale;
        hud.transform.position = oldHudPosition;
        oriented = false; // reset for next SOP trial (if any)       
    }
}