using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Valve.VR;
using Valve.VR.InteractionSystem;
using UnityStandardAssets.Vehicles.Ball;
using static UnityEngine.GraphicsBuffer;
using System.Linq;

public enum HideTargetOnStart_ExplorationTask
{
    Off,
    SetInactive,
    SetInvisible,
    DisableCompletely,
    Mask,
    MaskAllExceptStarting,
    SetProbeTrial
}

public class ExplorationTask_v2 : ExperimentTask
{
    [Header("Task-specific Properties")]
    public ObjectList destinations; // objectlist of all the possible target objects
    public GameObject currentTarget; // gameobject that I need to navigate to
    public ObjectList listOfNavStarts; // objectlist of our starting locations
    public TextAsset NavigationInstruction;
    public float desiredSphereYPosition = 1.5f;


    // Manipulate trial/task termination criteria
    [Tooltip("in meters")]
    public float distanceAllotted = Mathf.Infinity;
    public float distanceRemaining;
    [Tooltip("in seconds")]
    public float timeAllotted = Mathf.Infinity;
    public float timeRemaining;
    public float runningStopwatchTime;
    public float initialTimeAllotted;
    public float extraTimePassed;
    public float initialExplorationTime;
    [Tooltip("Do we want time or distance remaining to be broadcast somewhere?")]
    public TextMeshProUGUI printRemainingTimeTo;
    private string baseText;

    // Use a scoring/points system (not currently configured)
    [HideInInspector] private int score = 0;
    [HideInInspector] public int scoreIncrement = 50;
    [HideInInspector] public int penaltyRate = 2000;
    [HideInInspector] private float penaltyTimer = 0;
    [HideInInspector] public bool showScoring;

    // Handle the rendering of the target objects (default: always show)
    public HideTargetOnStart hideTargetOnStart;
    public GameObject targetMaskPrefab;
    public float unmaskStartObjectFor;
    public float showTargetAfterSeconds;
    public bool hideNonTargets;
    public bool insideRedSphereCollider = false;
    public bool resetObjForNextTrial = false;

    // for compass assist
    public LM_Compass assistCompass;
    [Tooltip("negative values denote time before compass is hidden; 0 is always on; set very high for no compass")]
    public float SecondsUntilAssist = Mathf.Infinity;
    public Vector3 compassPosOffset; // where is the compass relative to the active player snappoint
    public Vector3 compassRotOffset; // compass rotation relative to the active player snap point

    // For logging output
    public float startTime;
    private Vector3 playerLastPosition;
    private float playerDistance = 0;
    private Vector3 scaledPlayerLastPosition;
    private float scaledPlayerDistance = 0;
    private float optimalDistance;

    // 4/27/2022 Added for Loop Closure Task
    public float allowContinueAfter = Mathf.Infinity; // flag to let participants press a button to continue without necessarily arriving
    public bool onlyContinueFromTargets;
    public bool haptics;
    private float clockwiseTravel = 0; // relative to the origin (0,0,0) in world space
    public bool logStartEnd;
    private Vector3 startXYZ;
    private Vector3 endXYZ;

    public float GetStartTime()
    {
        return startTime;
    }

    public override void startTask()
    {
        TASK_START();
        avatarLog.navLog = true;
        if (isScaled) scaledAvatarLog.navLog = true;
    }

    public override void TASK_START()
    {
        if (!manager) Start();
        base.startTask();

        // startTime = Current time in seconds... since the start of landmarks
        startTime = Time.time;
        Debug.Log("The time is" + startTime);
        initialTimeAllotted = timeAllotted;

        if (skip)
        {
            log.log("INFO    skip task    " + name, 1);
            return;
        }

        if (!destinations)
        {
            Debug.LogWarning("No target objects specified; task will run as" +
                " free exploration with specified time Alloted or distance alloted" +
                " (whichever is less)");

            // Make a dummy placeholder for exploration task to avoid throwing errors
            var tmp = new List<GameObject>();
            tmp.Add(gameObject);
            gameObject.AddComponent<ObjectList>();
            gameObject.GetComponent<ObjectList>().objects = tmp;
            destinations = gameObject.GetComponent<ObjectList>();

        }

        hud.showEverything();
        hud.showScore = showScoring;
        currentTarget = destinations.currentObject();

        // if it's a target, open the door to show it's active
        if (currentTarget.GetComponentInChildren<LM_TargetStore>() != null)
        {
            currentTarget.GetComponentInChildren<LM_TargetStore>().OpenDoor();
        }

        if (NavigationInstruction)
        {
            string msg = NavigationInstruction.text;
            if (destinations != null) msg = string.Format(msg, currentTarget.name);
            hud.setMessage(msg);
        }
        else
        {
            hud.SecondsToShow = 0;
            //hud.setMessage("Please find the " + current.name);
        }

        // Handle if we're hiding all the non-targets
        if (hideNonTargets)
        {
            foreach (GameObject item in destinations.objects)
            {
                if (item.name != currentTarget.name)
                {
                    item.SetActive(false);
                }
                else item.SetActive(true);
            }
        }

        // Handle if we're hiding the target object
        if (hideTargetOnStart != HideTargetOnStart.Off)
        {
            if (hideTargetOnStart == HideTargetOnStart.SetInactive)
            {
                currentTarget.GetComponent<Collider>().enabled = false;
            }
            else if (hideTargetOnStart == HideTargetOnStart.SetInvisible)
            {
                currentTarget.GetComponentInChildren<MeshRenderer>().enabled = false;
            }
            else if (hideTargetOnStart == HideTargetOnStart.DisableCompletely)
            {
                //fixme - at some point should write LM methods to turn off objects, their renderers, their colliders, and/or their lights (including children)
                // currentTarget.GetComponent<Collider>().enabled = false;
                // currentTarget.GetComponentInChildren<MeshRenderer>().enabled = false;
                // var halo = (Behaviour) currentTarget.GetComponent("Halo");
                // if(halo != null) halo.enabled = false;
                // fixme: adding this doesn't work either --> currentTarget.GetComponentInChildren<MeshRenderer>().enabled = false;
                currentTarget.SetActive(false); //fixme
                                                //fixme all the gameobject's (and their 'child' gameobjects) are visible even with this .SetActive(false); line
            }
            else if (hideTargetOnStart == HideTargetOnStart.SetProbeTrial)
            {
                currentTarget.GetComponent<Collider>().enabled = false;
                currentTarget.GetComponentInChildren<MeshRenderer>().enabled = false;
            }
            else if (hideTargetOnStart == HideTargetOnStart.Mask)
            {
                
                int numSpheres = 0;
                foreach (GameObject target in destinations.objects.Distinct<GameObject>())
                {
                    // Hide the objects so they don't poke out of the masks
                    MeshRenderer[] meshRenderers = target.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer meshRenderer in meshRenderers) meshRenderer.enabled = false;

                    // Instantiate a new instance of your red sphere
                    if (numSpheres < destinations.objects.Count)
                    {
                        GameObject newSphere = Instantiate(targetMaskPrefab,
                        position: new Vector3(target.transform.position.x, desiredSphereYPosition, target.transform.position.z),
                        rotation: Quaternion.identity,
                        parent: transform);
                        newSphere.name = target.name + "_mask";
                        newSphere.tag = "RedSphereTag"; // after spawning in these spheres, tag them with "RedSphereTag"
                        numSpheres++;
                    }
                }
                // Handle if we're temporarily showing the start
                if (unmaskStartObjectFor > 0 && unmaskStartObjectFor < Mathf.Infinity)
                {
                    Debug.Log("_____Unmasking the start location");
                    StartCoroutine(UnmaskStartObjectFor());
                }
            }

            else
            {
                currentTarget.SetActive(true); // make sure the target is visible unless the bool to hide was checked
                try
                {
                    currentTarget.GetComponentInChildren<MeshRenderer>().enabled = true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            // save the original string so we can reformat each frame
            if (printRemainingTimeTo != null) baseText = printRemainingTimeTo.text;

            // Get the avatar start location (distance = 0)
            playerDistance = 0.0f;
            clockwiseTravel = 0.0f;
            playerLastPosition = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;
            if (isScaled)
            {
                scaledPlayerDistance = 0.0f;
                scaledPlayerLastPosition = scaledAvatar.transform.position;
            }

            // Calculate optimal distance to travel (straight line)
            if (isScaled)
            {
                optimalDistance = Vector3.Distance(scaledAvatar.transform.position, currentTarget.transform.position);
            }
            else optimalDistance = Vector3.Distance(avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position, currentTarget.transform.position);

            // Grab our LM_Compass object and move it to the player snapPoint
            if (assistCompass != null)
            {
                assistCompass.transform.parent = avatar.GetComponentInChildren<LM_SnapPoint>().transform;
                assistCompass.transform.localPosition = compassPosOffset;
                assistCompass.transform.localEulerAngles = compassRotOffset;
                assistCompass.gameObject.SetActive(false);
            }

            //// MJS 2019 - Move HUD to top left corner
            //hud.hudPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1);
            //hud.hudPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.9f);

            if (logStartEnd) startXYZ = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;

            if (vrEnabled & haptics) SteamVR_Actions.default_Haptic.Execute(0f, 2.0f, 65f, 1f, SteamVR_Input_Sources.Any);

        }
    }

    public override bool updateTask()
    {
        base.updateTask();
        Debug.Log($"Start Timer: {startTime}");

        //// use experiment.cs to get each target object on which the desired collider SHOULD be attached
        //for (int i = 0; i < manager.targetObjects.transform.childCount; i++)
        //{
        //    targetObjectColliders.Add(manager.targetObjects.transform.GetChild(i).gameObject);
        //}

        if (skip)
        {
            //log.log("INFO    skip task    " + name,1 );
            return true;
        }

        if (score > 0) penaltyTimer = penaltyTimer + (Time.deltaTime * 1000);

        if (penaltyTimer >= penaltyRate)
        {
            penaltyTimer = penaltyTimer - penaltyRate;
            if (score > 0)
            {
                score = score - 1;
                hud.setScore(score);
            }
        }

        //show target after set time
        if (hideTargetOnStart != HideTargetOnStart.Off && Time.time - startTime > showTargetAfterSeconds)
        {
            switch (hideTargetOnStart)
            {
                case HideTargetOnStart.SetInactive:
                    currentTarget.GetComponent<Collider>().enabled = true;
                    break;
                case HideTargetOnStart.SetInvisible:
                    currentTarget.GetComponentInChildren<MeshRenderer>().enabled = true;
                    break;
                case HideTargetOnStart.DisableCompletely:
                    //fixme - at some point should write LM methods to turn off objects, their renderers, their colliders, and/or their lights (including children)
                    currentTarget.GetComponent<Collider>().enabled = true;
                    currentTarget.GetComponentInChildren<MeshRenderer>().enabled = true;
                    var halo = (Behaviour)currentTarget.GetComponent("Halo");
                    if (halo != null) halo.enabled = true;
                    break;
                case HideTargetOnStart.SetProbeTrial:
                    currentTarget.GetComponent<Collider>().enabled = true;
                    currentTarget.GetComponentInChildren<MeshRenderer>().enabled = true;
                    break;

                case HideTargetOnStart.Mask:
                    break;
            }
        }

        // Keep updating the distance traveled and kill task if they reach max
        playerDistance += Vector3.Distance(avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position, playerLastPosition);
        // Subtract the counter-clockwise angle since the last frame to get clockwise movement
        clockwiseTravel -= Vector3Angle2D(playerLastPosition, avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position);
        playerLastPosition = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;

        if (isScaled)
        {
            scaledPlayerDistance += Vector3.Distance(scaledAvatar.transform.position, scaledPlayerLastPosition);
            scaledPlayerLastPosition = scaledAvatar.transform.position;
        }

        // handle the compass objects render (visible or not)
        if (assistCompass != null)
        {
            // Keep the assist compass pointing at the target (even if it isn't visible)
            var targetDirection = 2 * assistCompass.transform.position - currentTarget.transform.position;
            targetDirection = new Vector3(targetDirection.x, assistCompass.pointer.transform.position.y, targetDirection.z);
            assistCompass.pointer.transform.LookAt(targetDirection, Vector3.up);
            // Show assist compass if and when it is needed
            if (assistCompass.gameObject.activeSelf == false & SecondsUntilAssist >= 0 & (Time.time - startTime > SecondsUntilAssist))
            {
                assistCompass.gameObject.SetActive(true);
            }
        }

        distanceRemaining = distanceAllotted - playerDistance;
        if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.PageDown))
        {
            timeAllotted = Mathf.Infinity;
            Debug.Log("INFINITY time for exploration phase");
            log.log("INPUT_EVENT    Passive Exploration Condition - Infinite Time   1", 1);
        }
        timeRemaining = timeAllotted - (Time.time - startTime);
        runningStopwatchTime = Time.time - startTime;
        if (Time.time - startTime >= timeAllotted)
        {
            Debug.Log("INFINITY Ending exploration phase automatically");
            Debug.Log("Time has run out");
            Debug.Log("Diff b/w start time and current time " + (Time.time - startTime));
            Debug.Log("The current time is" + Time.time);
            Debug.Log("The start time is" + startTime);
            return true;
        }
        if (timeAllotted == Mathf.Infinity && Time.time - startTime > initialTimeAllotted)
        {
            extraTimePassed = (Time.time - startTime) - initialTimeAllotted;
            Debug.Log($"INFINITY Extra time passed: {extraTimePassed}");
        }
        if (Input.GetKeyDown(KeyCode.Backslash) || Input.GetKeyDown(KeyCode.End))
        {
            Debug.Log("INFINITY Pressing PPP END");
            log.log("INPUT_EVENT    Player Ended ExplorationTask    1", 1);
            if (timeAllotted == Mathf.Infinity && Time.time - startTime > initialTimeAllotted)
            {
                extraTimePassed = (Time.time - startTime) - initialTimeAllotted;
                Debug.Log($"Extra time passed: {extraTimePassed}");
            }
            return true;
        }

        // If we have a place to output ongoing trial info (time/dist remaining), use it
        if (printRemainingTimeTo != null)
        {
            printRemainingTimeTo.text = string.Format(baseText, Mathf.Round(distanceRemaining), Mathf.Round(timeRemaining));
        }

        // End the trial if they reach the max distance allotted
        if (!isScaled & playerDistance >= distanceAllotted) return true;
        else if (isScaled & scaledPlayerDistance >= distanceAllotted) return true;
        // End the trial if they reach the max time allotted

        if (killCurrent == true)
        {
            return KillCurrent();
        }

        bool allowContinue;
        //Debug.Log("Currently hitting target named: " + manager.currentlyAtTarget.name);
        if (onlyContinueFromTargets && manager.currentlyAtTarget.name == "")
        {
            allowContinue = false;
        }
        else allowContinue = true;


        if (Input.GetKeyDown(KeyCode.T))
        {
            initialExplorationTime = Time.time - startTime;
            Debug.Log($"Player Finished Initial Learning (Time): {initialExplorationTime}");
            log.log("INPUT_EVENT    Initial ExplorationTask Learning Time   1", 1);
        }

        // fixme - Issue with this method of ending the trial breaking HUD (normal collisions ok)
        if (allowContinue && Time.time - startTime > allowContinueAfter)
        {
            if (insideRedSphereCollider)
            {
                if (vrEnabled)
                {
                    if (vrInput.TriggerButton.GetStateDown(SteamVR_Input_Sources.Any))
                    {
                        Debug.Log("Participant ended the trial");
                        log.log("INPUT_EVENT    Player Arrived at Destination    1", 1);
                        hud.hudPanel.SetActive(false);
                        hud.setMessage("");

                        if (haptics) SteamVR_Actions.default_Haptic.Execute(0f, 2.0f, 65f, 1f, SteamVR_Input_Sources.Any);

                        return true;
                    }
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    Debug.Log("Participant ended the trial");
                    log.log("INPUT_EVENT    Player Arrived at Destination    1", 1);
                    hud.hudPanel.SetActive(false);
                    hud.setMessage("");
                    return true;
                }
            }
        }
        return false;
    }

    public override void endTask()
    {
        TASK_END();
        //avatarController.handleInput = false;
    }

    public override void TASK_PAUSE()
    {
        avatarLog.navLog = false;
        if (isScaled) scaledAvatarLog.navLog = false;
        //base.endTask();
        log.log("TASK_PAUSE\t" + name + "\t" + this.GetType().Name + "\t", 1);
        //avatarController.stop();

        hud.setMessage("");
        hud.showScore = false;

    }

    public override void TASK_END()
    {
        base.endTask();
        
        if (printRemainingTimeTo != null) printRemainingTimeTo.text = baseText;
        var navTime = Time.time - startTime;

        if (logStartEnd) endXYZ = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;

        //avatarController.stop();
        avatarLog.navLog = false;
        if (isScaled) scaledAvatarLog.navLog = false;

        // close the door if the target was a store and it is open
        // if it's a target, open the door to show it's active
        if (currentTarget.GetComponentInChildren<LM_TargetStore>() != null)
        {
            currentTarget.GetComponentInChildren<LM_TargetStore>().CloseDoor();
        }


        Collider[] colliders = currentTarget.GetComponentsInChildren<Collider>();
        if (colliders != null)
        {
            foreach (Collider collider in colliders)
            {
                collider.enabled = true;
            }
        }


        var halo = (Behaviour)currentTarget.GetComponent("Halo");
        if (halo != null) halo.enabled = true;

        hud.setMessage("");
        hud.showScore = false;

        hud.SecondsToShow = hud.GeneralDuration;

        if (assistCompass != null)
        {
            // Hide the assist compass
            assistCompass.gameObject.SetActive(false);
        }

        // Move hud back to center and reset
        hud.hudPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        hud.hudPanel.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);

        float perfDistance;
        if (isScaled)
        {
            perfDistance = scaledPlayerDistance;
        }
        else perfDistance = playerDistance;

        var excessPath = perfDistance - optimalDistance;

        // set impossible values if the nav task was skipped
        if (skip)
        {
            navTime = float.NaN;
            perfDistance = float.NaN;
            optimalDistance = float.NaN;
            excessPath = float.NaN;
        }


        // log.log("LM_OUTPUT\tNavigationTask.cs\t" + masterTask.name + "\t" + this.name + "\n" +
        // 	"Task\tBlock\tTrial\tTargetName\tOptimalPath\tActualPath\tExcessPath\tRouteDuration\n" +
        // 	masterTask.name + "\t" + masterTask.repeatCount + "\t" + parent.repeatCount + "\t" + currentTarget.name + "\t" + optimalDistance + "\t"+ perfDistance + "\t" + excessPath + "\t" + navTime
        //     , 1);

        // More concise LM_TrialLog logging
        taskLog.AddData(transform.name + "_target", currentTarget.name);
        taskLog.AddData(transform.name + "_actualPath", perfDistance.ToString());
        taskLog.AddData(transform.name + "_optimalPath", optimalDistance.ToString());
        taskLog.AddData(transform.name + "_excessPath", excessPath.ToString());
        taskLog.AddData(transform.name + "_clockwiseTravel", clockwiseTravel.ToString());
        taskLog.AddData(transform.name + "_duration", navTime.ToString());
        //taskLog.AddData("testtesttest" + "_correctPosition", interfacePivots.correctPosition.ToString());

        if (logStartEnd)
        {

            taskLog.AddData(transform.name + "_startX", startXYZ.x.ToString());
            taskLog.AddData(transform.name + "_startZ", startXYZ.z.ToString());
            taskLog.AddData(transform.name + "_endX", endXYZ.x.ToString());
            taskLog.AddData(transform.name + "_endZ", endXYZ.z.ToString());

        }

        timeRemaining = 0f;
        runningStopwatchTime = 0f;
    }


    public override bool OnControllerColliderHit(GameObject hit)
    {
        return false;
    }

    public IEnumerator UnmaskStartObjectFor() // handles the "showing and hiding" the actual target object itself... NOT instantiating the red spheres portion!
    {
        var startFromTarget = manager.targetObjects.transform.Find(listOfNavStarts.currentObject().name).gameObject;
        // get the starting target gameobject
        startFromTarget.SetActive(true);
        
        //if (startFromTarget.GetComponentInChildren<InterfacePivot>() != null)
        //    // if you CAN find a InterfacePivot script on the (active) starting gameobject, disable it
        //{
        //    startFromTarget.GetComponentInChildren<InterfacePivot>().gameObject.SetActive(false);
        //}

        foreach (var mr in startFromTarget.transform.GetComponentsInChildren<MeshRenderer>())
        // for each of the meshrenderer components within the starting object gameobject's...
        {
            mr.gameObject.SetActive(true); // set the entire gameobject active
            mr.enabled = true; // and enable it's meshrenderer 
        }
        transform.Find(startFromTarget.name + "_mask").gameObject.SetActive(false);
        // After spawning in the spheres, whose names are {Starting Target Object Name_mask}, turn off the sphere gameobject
        yield return new WaitForSeconds(unmaskStartObjectFor);
        // show the physical starting target object for a certain duration

        foreach (var mr in startFromTarget.transform.GetComponentsInChildren<MeshRenderer>())
        // for each of the meshrenderer components within the starting object gameobject's...
        {
            mr.gameObject.SetActive(false);
            // set the entire gameobject active
            mr.enabled = false;
            // fixmefixmefixme: is this line causing the error??? On subsequent trials, we're UNABLE to get the meshrenderer component, which could be due to 2 reasons:
            // 1. Starting Target Gameobject was set to false in the previous trial, and never reset
            // 2. Starting Target Gameoobject's meshrenderer was set to false in the previous trial, and never reset
        }
        transform.Find(startFromTarget.name + "_mask").gameObject.SetActive(true);
        // set the spheres active
        //listOfNavStarts.currentObject().transform.Find(targetMaskPrefab.name).gameObject.SetActive(true);
        //fixmefixmefixme: END RESULT AFTER FIRST RUN-THRU:
        //  Starting GameObject: Inactive (.setActive(false))
        //  Starting GameObject Meshrenderer: Disabled (.enabled = false;)
        //  Red Sphere GameObject: Active (.setActive(true))
        //  To reset ->...
        //  Starting GameObject: ACTIVE(.setActive(true))
        //  Starting GameObject Meshrenderer: ENABLED (.enabled = true;)
        //  Red Sphere GameObject: inactive (.setActive(false))

    }
}