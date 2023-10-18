using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Valve.VR;
using Valve.VR.InteractionSystem;
using UnityStandardAssets.Vehicles.Ball;
using static UnityEngine.GraphicsBuffer;
using System.Linq;

public enum HideTargetOnStart_Exploration
{
    Off,
    SetInactive,
    SetInvisible,
    DisableCompletely,
    Mask,
    SetProbeTrial
}

public class ExplorationTask : ExperimentTask
{
    [Header("Task-specific Properties")]
    public ObjectList destinations;
    public GameObject currentTarget;
    public ObjectList listOfNavStarts;
    //public GameObject startingLocation;
    public TextAsset NavigationInstruction;
    public float desiredSphereYPosition = 1.5f;


    // Manipulate trial/task termination criteria
    [Tooltip("in meters")]
    public float distanceAllotted = Mathf.Infinity;
    [Tooltip("in seconds")]
    public float timeAllotted = Mathf.Infinity;
    public float timeRemaining;
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
    public float unmaskStartObjectFor;
    public GameObject targetMaskPrefab;
    public float showTargetAfterSeconds;
    public bool hideNonTargets;
    public bool insideRedSphereCollider = false;

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
    private LM_DecisionPoint[] decisionPoints;
    //private InterfacePivot[] interfacePivots;

    List<GameObject> targetObjectColliders = new List<GameObject>();

    // 4/27/2022 Added for Loop Closure Task
    public float allowContinueAfter = Mathf.Infinity; // flag to let participants press a button to continue without necessarily arriving
    public bool onlyContinueFromTargets;
    public bool haptics;
    private float clockwiseTravel = 0; // relative to the origin (0,0,0) in world space
    public bool logStartEnd;
    private Vector3 startXYZ;
    private Vector3 endXYZ;

    ////For target object bobbing animation
    //public bool targetObjectBobbingAnimation; //checkbox - should be ticked during exploration, not during navigation 
    //public float bobbingSpeed = 1f;
    //public float bobbingHeight = 0.5f;
    //private Vector3 originalPosition;
    //private float bobbingTimer = 0f;

    //For distance to closest target logging
    private Vector2 endXZ;

    //For distance to border logging
    public string borderObjectTag = "BorderObjects"; // Tag for the border objects
    private List<float> distancesToBorder = new List<float>(); // List to store frame by frame distances to border
    private List<GameObject> borderObjects2 = new List<GameObject>();
    public Vector2 playerBorderSumAndMeasurements;

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
        //TL Comments: In each "TASK" (InstructionsTask, NavigationTask, etc.), we'll have a base.{method}, such as: base.startTask, base.updateTask, base.endTask, etc. Base refers to the parent class that this script derives from, aka: ExperimentTask.cs. We call the appropriate methods in any script derived from ExperimentTask, by using base(dot).
        //fixmefixmefixme: calc. the shortest distance b/w player controller & tagged target object, and store that name as avariable. Once that variable is indexed, once we do the masking stuff, do this masking stuff for all target obj's EXCEPT this specific target object

        // foreach (GameObject obj in listOfNavStarts.objects.Distinct<GameObject>())
        // {
        //     if (obj.activeSelf == false) { continue; }
        //     obj.SetActive(true);
        //     foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
        //     {
        //         child.gameObject.SetActive(true);
        //     }
        // }


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

        // update the trial count on the overlay
        //if (overlayTargetObject != null & currentTarget != null) overlayTargetObject.text = string.Format("{0}", currentTarget.name);

        // Debug.Log ("Find " + current.name);

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

                foreach (GameObject target in destinations.objects.Distinct<GameObject>())
                {
                    // turning off the target object meshrenderers
                    MeshRenderer[] meshRenderers = target.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer meshRenderer in meshRenderers) meshRenderer.enabled = false;
                    // Instantiate a new instance of your red sphere
                    int numSpheres = 0;
                    if (numSpheres < destinations.objects.Distinct<GameObject>().Count())
                    // if the # of spheres is less than 9...
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
                StartCoroutine(UnmaskStartObjectFor());
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

            // startTime = Current time in seconds
            startTime = Time.time;
            Debug.Log("The time is" + startTime);

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


            // Look for any LM_Decsion Points we will want to track
            if (FindObjectsOfType<LM_DecisionPoint>().Length > 0)
            {
                decisionPoints = FindObjectsOfType<LM_DecisionPoint>();

                // Clear any decisions on LM_DecisionPoints
                foreach (var pt in decisionPoints)
                {
                    pt.ResetDecisionPoint();
                }
            }
            ////if (FindObjectsOfType<InterfacePivot>().Length > 0)
            //{
            //    interfacePivots = FindObjectsOfType<InterfacePivot>();
            //    foreach (var pts in interfacePivots)
            //    {
            //        pts.ResetVariables();
            //    }
            //}

            if (logStartEnd) startXYZ = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;

            if (vrEnabled & haptics) SteamVR_Actions.default_Haptic.Execute(0f, 2.0f, 65f, 1f, SteamVR_Input_Sources.Any);

            //foreach (var obj in GameObject.FindGameObjectsWithTag("BorderObjects")) {
            //    Debug.Log("BORDER OBJECT -------------------------------" + obj.name);
            //    borderObjects2.Add(obj);
            //}

        }

        ////find starting location of player (where the trial started)

        if (!listOfNavStarts)
        {
            Debug.LogWarning("No trial start locations specified; task will run as" +
                " free exploration with specified time Alloted or distance alloted" +
                " (whichever is less)");

            // Make a dummy placeholder for exploration task to avoid throwing errors
            var tmp_2 = new List<GameObject>();
            tmp_2.Add(gameObject);
            gameObject.AddComponent<ObjectList>();
            gameObject.GetComponent<ObjectList>().objects = tmp_2;


            listOfNavStarts = gameObject.GetComponent<ObjectList>();

        }
        //startingLocation = listOfNavStarts.currentObject();
    }

    public override bool updateTask()
    {
        base.updateTask();
        Debug.Log ($"Start Timer: { startTime}");
        // use experiment.cs to get each target object on which the desired collider SHOULD be attached
        for (int i = 0; i < manager.targetObjects.transform.childCount; i++)
        {
            targetObjectColliders.Add(manager.targetObjects.transform.GetChild(i).gameObject);
        }

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
                default:
                    Debug.Log("No hidden targets identified");
                    currentTarget.SetActive(true);
                    currentTarget.GetComponentInChildren<MeshRenderer>().enabled = true;
                    currentTarget.GetComponent<Collider>().enabled = true;
                    break;
            }
        }

        // Keep updating the distance traveled and kill task if they reach max
        playerDistance += Vector3.Distance(avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position, playerLastPosition);
        // Subtract the counter-clockwise angle since the last frame to get clockwise movement
        clockwiseTravel -= Vector3Angle2D(playerLastPosition, avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position);
        playerLastPosition = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;


        // Keep updating the calculations for borderDistances
        //logging distance to border at each frame, then finding the average value for the entire trial 
        //float closestDistance;
        //GameObject closestBorderObject;

        //var playerBorderDistances = new Dictionary<string, float>();
        //foreach (GameObject borderObject in borderObjects2)
        //{
        //    Vector3 closestBorderPoint = borderObject.GetComponent<Collider>().ClosestPointOnBounds(manager.player.transform.position);
        //    float player2borderDist = Vector3Distance2D(closestBorderPoint, manager.player.transform.position);
        //    playerBorderDistances.Add(borderObject.name, player2borderDist);

        //    //float distance = Vector3.Distance(avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position, borderObject.transform.position); //calculating distance from player to each border object

        //    //if (distance < closestDistance) //initially setting the closest object & distance as the first border object's distance & name, then updating and replacing this if smaller distance value is found as script iterates through list of border objects
        //    //{
        //    //    closestDistance = distance;
        //    //    Debug.Log("closestDistance:" + closestDistance);
        //    //    closestBorderObject = borderObject;
        //    //    Debug.Log("closestBorderObject:" + closestBorderObject);
        //    //}
        //}
        //var closestBorder = playerBorderDistances.OrderBy(kvp => kvp.Value).First();
        //Debug.Log(closestBorder.Key + " is the closest border object, located " + closestBorder.Value + "m, orthongonally from the player");
        //playerBorderSumAndMeasurements += new Vector2(closestBorder.Value, 1);


        //if (closestBorderObject != null)
        //{
        //    distancesToBorder.Add(closestDistance); //adding value to frame by frame distances to border list 

        //}

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


        float distanceRemaining = distanceAllotted - playerDistance;
        timeRemaining = timeAllotted - (Time.time - startTime);
        // If we have a place to output ongoing trial info (time/dist remaining), use it
        if (printRemainingTimeTo != null)
        {
            printRemainingTimeTo.text = string.Format(baseText, Mathf.Round(distanceRemaining), Mathf.Round(timeRemaining));
        }

        // End the trial if they reach the max distance allotted
        if (!isScaled & playerDistance >= distanceAllotted) return true;
        else if (isScaled & scaledPlayerDistance >= distanceAllotted) return true;
        // End the trial if they reach the max time allotted
        if (Time.time - startTime >= timeAllotted)
        {
            Debug.Log("Time has run out!");
            Debug.Log("Time Alloted is:" + timeAllotted);
            Debug.Log("Diff. b/w start time and current time" + (Time.time - startTime));
            Debug.Log("The current time is" + (Time.time));
            Debug.Log("The start time is" + startTime);
            return true;
        }
       
        if (killCurrent == true)
        {
            return KillCurrent();
        }

        // In order to allow manual progression, allowContinueAfter must not be zero and enough time must have passed
        if (allowContinueAfter != Mathf.Infinity && Time.time - startTime > allowContinueAfter)
        //fixme 06-01: when the obj is masked ->...
        {
            //// They either need to be allowed to continue from anywhere or be at one of the targets to provide input
            //if (!onlyContinueFromTargets || (onlyContinueFromTargets &&
            //                                //(manager.triggeredFromTargetNamed != "" || manager.collidingWithTargetNamed != "")
            //                                ))
            
                // Take some response input
                if (insideRedSphereCollider)
                {
                    if (vrEnabled)
                    {
                        if (vrInput.TriggerButton.GetStateDown(SteamVR_Input_Sources.Any))
                        {
                            Debug.Log("Participant ended the trial");
                            log.log("INPUT_EVENT    Player Arrived at Destination    1", 1);
                            //hud.hudPanel.SetActive(false);
                            //hud.setMessage("");
                            if (haptics) SteamVR_Actions.default_Haptic.Execute(0f, 2.0f, 65f, 1f, SteamVR_Input_Sources.Any);
                            return true;
                        }
                    }
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        Debug.Log("Participant ended the trial");
                        log.log("INPUT_EVENT    Player Arrived at Destination    1", 1);
                        //hud.hudPanel.SetActive(false);
                        //hud.setMessage("");
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

        // foreach (GameObject obj in listOfNavStarts.objects.Distinct<GameObject>())
        // {
        //     if (obj.activeSelf == false) { continue; }
        //     obj.SetActive(true);
        //     foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
        //     {
        //         child.gameObject.SetActive(true);
        //     }
        // }
        // Find all instances of InterfacePivot and call stroyAllInterfaces() on each one
        // Find all instances of InterfacePivot 
        //InterfacePivot[] interfacePivots = FindObjectsOfType<InterfacePivot>();
        //foreach (InterfacePivot pivot in interfacePivots)
        //{
        //    // Set the taskHasEnded flag to true and call LogData()
        //    pivot.taskHasEnded = true;
        //    pivot.LogData();
        //    // Destroy the pivot instance
        //    Destroy(pivot.gameObject);
        //}
        //foreach (InterfacePivot pivot in interfacePivots)
        //{
        //    // Access the variables you want to log from the pivot instance
        //    int totalInterfaceVisits = InterfacePivot.totalInterfaceVisits;
        //    int targetRepeatVisits = pivot.targetRepeatVisits;
        //    float visitTime = pivot.visitTime;
        //    string correctObject = pivot.correctObject;
        //    int correctPosition = pivot.correctPosition;
        //    string selectedObject = pivot.selectedObject;
        //    int selectedPosition = pivot.selectedPosition;
        //    bool responded = pivot.responded;
        //    bool correct = pivot.correct;
        //    float buttonResponseTime = pivot.buttonResponseTime;
        //    float visitDuration = pivot.visitDuration;

        //    // Log the variables using the taskLog variable from ExperimentTask
        //    taskLog.AddData(pivot.name + "_totalInterfaceVisits", totalInterfaceVisits.ToString());
        //    taskLog.AddData(pivot.name + "_targetRepeatVisits", targetRepeatVisits.ToString());
        //    taskLog.AddData(pivot.name + "_visitTime", visitTime.ToString());
        //    taskLog.AddData(pivot.name + "_correctObject", correctObject);
        //    taskLog.AddData(pivot.name + "_correctPosition", correctPosition.ToString());
        //    taskLog.AddData(pivot.name + "_selectedObject", selectedObject);
        //    taskLog.AddData(pivot.name + "_selectedPosition", selectedPosition.ToString());
        //    taskLog.AddData(pivot.name + "_responded", responded.ToString());
        //    taskLog.AddData(pivot.name + "_correct", correct.ToString());
        //    taskLog.AddData(pivot.name + "_buttonResponseTime", buttonResponseTime.ToString());
        //    taskLog.AddData(pivot.name + "_visitDuration", visitDuration.ToString());
        //    pivot.destroyInterface();
        //}

        GameObject[] spheres = GameObject.FindGameObjectsWithTag("RedSphereTag");
        if (spheres.Length > 0)
        {
            for (int i = 0; i < spheres.Length; i++)
            {
                Destroy(spheres[i]);
            }
        }

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

        // re-enable everything on the gameobject we just finished finding
        MeshRenderer[] meshRenderers = currentTarget.GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers != null)
        {
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = true;
            }
        }

        // re-enable everything on the gameobject we just finished finding
        //currentTarget.GetComponentInChildren<MeshRenderer>().enabled = true; June 1st edits, we took this out because it was breaking the task 
        //currentTarget.GetComponent<Collider>().enabled = true; June 1st edits, we took this out because it was breaking the task 
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

        ////calculating average distance to border for trial 
        ////private void CalculateAverageDistance()
        //float totaldistancesToBorder = 0f;

        //foreach (float distanceToBorder in distancesToBorder)
        //{
        //    totaldistancesToBorder += distanceToBorder;
        //}

        //var avgDist2border = playerBorderSumAndMeasurements[0] / playerBorderSumAndMeasurements[1];
        //Debug.Log("Sum of all Dist measurements: " + playerBorderSumAndMeasurements[0] + "\t Total Measurements: " + playerBorderSumAndMeasurements[1] + "\t Avg Dist: " + avgDist2border);

        //float averageDistanceToBorder = totaldistancesToBorder / distancesToBorder.Count;
        //Debug.Log("Average Distance: " + averageDistanceToBorder);

        //find finishing location of player (closest target object & distance to that target object)
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Target");
        GameObject closest_obj = null;
        float closestobjDistance = Mathf.Infinity;
        endXZ = new Vector2(endXYZ.x, endXYZ.z);

        foreach (GameObject obj in objs)
        {
            Vector3 objPosition = obj.transform.position;
            Vector2 objPositionXZ = new Vector2(objPosition.x, objPosition.z);
            float dist2obj = Vector2.Distance(endXZ, objPositionXZ);

            if (dist2obj < closestobjDistance)
            {
                closestobjDistance = dist2obj;
                closest_obj = obj;
            }
        }

        if (closest_obj != null)
        {
            Debug.Log("Closest target: " + closest_obj.name);
            Debug.Log("Distance to closest target: " + closestobjDistance);
        }



        // More concise LM_TrialLog logging
        //taskLog.AddData(transform.name + "_start", startingLocation.name);
        taskLog.AddData(transform.name + "_target", currentTarget.name);
        taskLog.AddData(transform.name + "_actualPath", perfDistance.ToString());
        taskLog.AddData(transform.name + "_optimalPath", optimalDistance.ToString());
        taskLog.AddData(transform.name + "_excessPath", excessPath.ToString());
        taskLog.AddData(transform.name + "_clockwiseTravel", clockwiseTravel.ToString());
        taskLog.AddData(transform.name + "_duration", navTime.ToString());
        //taskLog.AddData(transform.name + "averageDistToBorder", avgDist2border.ToString());
        taskLog.AddData(transform.name + "_trialEndClosestTarget", closest_obj.name);
        taskLog.AddData(transform.name + "_trialEndClosestTargetDist", closestobjDistance.ToString());
        //taskLog.AddData("testtesttest" + "_correctPosition", interfacePivots.correctPosition.ToString());

        if (logStartEnd)
        {

            taskLog.AddData(transform.name + "_startX", startXYZ.x.ToString());
            taskLog.AddData(transform.name + "_startZ", startXYZ.z.ToString());
            taskLog.AddData(transform.name + "_endX", endXYZ.x.ToString());
            taskLog.AddData(transform.name + "_endZ", endXYZ.z.ToString());

        }

        // Record any decisions made along the way
        if (decisionPoints != null)
        {
            foreach (LM_DecisionPoint nexus in decisionPoints)
            {
                taskLog.AddData(nexus.name + "_initialChoice", nexus.initialChoice);
                taskLog.AddData(nexus.name + "_finalChoice", nexus.currentChoice);
                taskLog.AddData(nexus.name + "_totalChoices", nexus.totalChoices.ToString());

                nexus.ResetDecisionPoint();
            }
        }
        //if (interfacePivots != null)
        //{
        //    // Passing in static variables by accessing them directly using the class name.
        //    //int totalInterfaceVisits = InterfacePivot.totalInterfaceVisits;
        //    //List<string> unvisitedTargets = InterfacePivot.unvisitedTargets;
        //    //taskLog.AddData(transform.name, totalInterfaceVisits.ToString());
        //    //float _allTargetsVisitedTime = InterfacePivot.allTargetsVisitedTime;

        //    foreach (InterfacePivot loggedVariables in interfacePivots)
        //    {

        //        //taskLog.AddData("_totalInterfaceVisits", totalInterfaceVisits.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_targetRepeatVisits", loggedVariables.targetRepeatVisits.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_responded", loggedVariables.responded.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_correct", loggedVariables.correct.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_visitTime", loggedVariables.visitTime.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_correctObject", loggedVariables.correctObject.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_correctPosition", loggedVariables.correctPosition.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_selectedObject", loggedVariables.selectedObject.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_selectedPosition", loggedVariables.selectedPosition.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_buttonResponseTime", loggedVariables.buttonResponseTime.ToString());
        //        //taskLog.AddData(loggedVariables.name + "_visitDuration", loggedVariables.visitDuration.ToString());
        //        ////fixmefixmefixme taskLog.AddData(loggedVariables.name + "_allTargetsVisitedTime", _allTargetsVisitedTime.ToString());
        //        loggedVariables.ResetVariables();
        //    }
        //}

        // Hide the overlay by setting back to empty string
        //if (overlayTargetObject != null) overlayTargetObject.text = "";
        // If we created a dummy Objectlist for exploration, destroy it
        Destroy(GetComponent<ObjectList>());

        if (canIncrementLists)
        {
            destinations.incrementCurrent();
        }
        currentTarget = destinations.currentObject();

        timeRemaining = 0f;
    }

    //fixme COMMENTED THIS CHUNK OUT AS IT WAS INTERFERING WITH TRIAL PROGRESSION
    public override bool OnControllerColliderHit(GameObject hit)
    {
    //    if ((hit == currentTarget | hit.transform.parent.gameObject == currentTarget) &
    //        hideTargetOnStart != HideTargetOnStart.DisableCompletely & hideTargetOnStart != HideTargetOnStart.SetInactive)
    //    {
    //        if (showScoring)
    //        {
    //            score = score + scoreIncrement;
    //            hud.setScore(score);
    //        }
    //        return true;
    //    }

        return false;
    }

    public IEnumerator UnmaskStartObjectFor() // handles the "showing and hiding" the starting target object itself
    {

        var startFromTarget = manager.targetObjects.transform.Find(listOfNavStarts.currentObject().name).gameObject;
        startFromTarget.SetActive(true);
        foreach (var mr in startFromTarget.transform.GetComponentsInChildren<MeshRenderer>())
        {
            mr.gameObject.SetActive(true); // Start GO: ON
            mr.enabled = true; // Start GO meshrenderer: ON
        }
        transform.Find(startFromTarget.name + "_mask").gameObject.SetActive(false); // Target Red sphere mask: OFF
        yield return new WaitForSeconds(unmaskStartObjectFor);
        foreach (var mr in startFromTarget.transform.GetComponentsInChildren<MeshRenderer>())
        {
            mr.gameObject.SetActive(false);  // Start GO: OFF
            mr.enabled = false; // Start GO meshrenderer: OFF
            // is this line causing the error??? On subsequent trials, we're UNABLE to get the meshrenderer component, which could be due to 2 reasons:
            // 1. Starting Target Gameobject was set to false in the previous trial, and never reset
            // 2. Starting Target Gameoobject's meshrenderer was set to false in the previous trial, and never reset
        }
        transform.Find(startFromTarget.name + "_mask").gameObject.SetActive(true); // Target Red sphere mask: ON
        //END RESULT AFTER FIRST RUN-THRU:
        //  To reset ->...
        //  Starting GameObject: ACTIVE(.setActive(true))
        //  Starting GameObject Meshrenderer: ENABLED (.enabled = true;)
    }



}







//fixme Old Task Script Version Below 
////using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using TMPro;
//using Valve.VR;
//using Valve.VR.InteractionSystem;

//public enum HideTargetOnStart
//{
//    Off,
//    SetInactive,
//    SetInvisible,
//    DisableCompletely,
//    Mask,
//    SetProbeTrial
//}

//public class NavigationTask : ExperimentTask
//{
//    [Header("Task-specific Properties")]
//    public ObjectList destinations;
//	public GameObject currentTarget;

//    public TextAsset NavigationInstruction;
//    public List<GameObject> TargetObjectColliders = new List<GameObject>();
//    public bool isPlayerInside = false;

//    // Manipulate trial/task termination criteria
//    [Tooltip("in meters")]
//    public float distanceAllotted = Mathf.Infinity;
//    [Tooltip("in seconds")]
//    public float timeAllotted = Mathf.Infinity;
//    [Tooltip("Do we want time or distance remaining to be broadcast somewhere?")]
//    public TextMeshProUGUI printRemainingTimeTo;
//    private string baseText;

//    // Use a scoring/points system (not currently configured)
//    [HideInInspector] private int score = 0;
//    [HideInInspector] public int scoreIncrement = 50;
//    [HideInInspector] public int penaltyRate = 2000;
//    [HideInInspector] private float penaltyTimer = 0;
//    [HideInInspector] public bool showScoring;

//    // Handle the rendering of the target objects (default: always show)
//    public HideTargetOnStart hideTargetOnStart;
//    [Tooltip("negative values denote time before targets are hidden; 0 is always on; set very high for no targets")]
//    public float showTargetAfterSeconds;
//    //public TextMeshProUGUI overlayTargetObject;

//    // Manipulate the rendering of the non-target environment objects (default: always show)
//    public bool hideNonTargets;

//    // for compass assist
//    public LM_Compass assistCompass;
//    [Tooltip("negative values denote time before compass is hidden; 0 is always on; set very high for no compass")]
//    public float SecondsUntilAssist = Mathf.Infinity;
//    public Vector3 compassPosOffset; // where is the compass relative to the active player snappoint
//    public Vector3 compassRotOffset; // compass rotation relative to the active player snap point


//    // For logging output
//    private float startTime;
//    private Vector3 playerLastPosition;
//    private float playerDistance = 0;
//    private Vector3 scaledPlayerLastPosition;
//    private float scaledPlayerDistance = 0;
//    private float optimalDistance;
//    private LM_DecisionPoint[] decisionPoints;
//    private Collider testPC;


//    // 4/27/2022 Added for Loop Closure Task
//    public float allowContinueAfter = Mathf.Infinity; // flag to let participants press a button to continue without necessarily arriving
//    public bool haptics;
//    private float clockwiseTravel = 0; // relative to the origin (0,0,0) in world space
//    public bool logStartEnd;
//    private Vector3 startXYZ;
//    private Vector3 endXYZ;

//    public override void startTask ()
//	{
//		TASK_START();
//		avatarLog.navLog = true;
//        if (isScaled) scaledAvatarLog.navLog = true;
//    }

//	public override void TASK_START()
//	{
//		if (!manager) Start();
//        base.startTask();

//        if (skip)
//        {
//            log.log("INFO    skip task    " + name, 1);
//            return;
//        }

//        if (!destinations)
//        {
//            Debug.LogWarning("No target objects specified; task will run as" +
//                " free exploration with specified time Alloted or distance alloted" +
//                " (whichever is less)");

//            // Make a dummy placeholder for exploration task to avoid throwing errors
//            var tmp = new List<GameObject>();
//            tmp.Add(gameObject);
//            gameObject.AddComponent<ObjectList>();
//            gameObject.GetComponent<ObjectList>().objects = tmp;


//            destinations = gameObject.GetComponent<ObjectList>();

//        }

//        hud.showEverything();
//		hud.showScore = showScoring;

//        currentTarget = destinations.currentObject();

//        // update the trial count on the overlay
//        //if (overlayTargetObject != null & currentTarget != null) overlayTargetObject.text = string.Format("{0}", currentTarget.name);

//        // Debug.Log ("Find " + current.name);

//        // if it's a target, open the door to show it's active
//        if (currentTarget.GetComponentInChildren<LM_TargetStore>() != null)
//        {
//            currentTarget.GetComponentInChildren<LM_TargetStore>().OpenDoor();
//        }

//		if (NavigationInstruction)
//		{
//			string msg = NavigationInstruction.text;
//			if (destinations != null) msg = string.Format(msg, currentTarget.name);
//			hud.setMessage(msg);
//   		}
//		else
//		{
//            hud.SecondsToShow = 0;
//            //hud.setMessage("Please find the " + current.name);
//		}

//        // Handle if we're hiding all the non-targets
//        if (hideNonTargets)
//        {
//            foreach (GameObject item in destinations.objects)
//            {
//                if (item.name != currentTarget.name)
//                {
//                    item.SetActive(false);
//                }
//                else item.SetActive(true);
//            }
//        }


//        // Handle if we're hiding the target object
//        if (hideTargetOnStart != HideTargetOnStart.Off)
//        {
//            if (hideTargetOnStart == HideTargetOnStart.SetInactive)
//            {
//                currentTarget.GetComponent<Collider>().enabled = false;
//            }
//            else if (hideTargetOnStart == HideTargetOnStart.SetInvisible)
//            {
//                currentTarget.GetComponent<MeshRenderer>().enabled = false;
//            }
//            else if (hideTargetOnStart == HideTargetOnStart.DisableCompletely)
//            {
//                //fixme - at some point should write LM methods to turn off objects, their renderers, their colliders, and/or their lights (including children)
//                // currentTarget.GetComponent<Collider>().enabled = false;
//                // currentTarget.GetComponent<MeshRenderer>().enabled = false;
//                // var halo = (Behaviour) currentTarget.GetComponent("Halo");
//                // if(halo != null) halo.enabled = false;
//                currentTarget.SetActive(false);
//            }
//            else if (hideTargetOnStart == HideTargetOnStart.SetProbeTrial)
//            {
//                currentTarget.GetComponent<Collider>().enabled = false;
//                currentTarget.GetComponent<MeshRenderer>().enabled = false;

//            }

//        }
//        else
//        {
//            currentTarget.SetActive(true); // make sure the target is visible unless the bool to hide was checked
//            try
//            {
//                currentTarget.GetComponent<MeshRenderer>().enabled = true;
//            }
//            catch (System.Exception ex)
//            {
//                Debug.LogException(ex);
//            }
//        }

//        // save the original string so we can reformat each frame
//        if (printRemainingTimeTo != null) baseText = printRemainingTimeTo.text;

//        // startTime = Current time in seconds
//        startTime = Time.time;

//        // Get the avatar start location (distance = 0)
//        playerDistance = 0.0f;
//        clockwiseTravel = 0.0f;
//        playerLastPosition = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;
//        if (isScaled)
//        {
//            scaledPlayerDistance = 0.0f;
//            scaledPlayerLastPosition = scaledAvatar.transform.position;
//        }

//        // Calculate optimal distance to travel (straight line)
//        if (isScaled)
//        {
//            optimalDistance = Vector3.Distance(scaledAvatar.transform.position, currentTarget.transform.position);
//        }
//        else optimalDistance = Vector3.Distance(avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position, currentTarget.transform.position);


//        // Grab our LM_Compass object and move it to the player snapPoint
//        if (assistCompass != null)
//        {
//            assistCompass.transform.parent = avatar.GetComponentInChildren<LM_SnapPoint>().transform;
//            assistCompass.transform.localPosition = compassPosOffset;
//            assistCompass.transform.localEulerAngles = compassRotOffset;
//            assistCompass.gameObject.SetActive(false);
//        }

//        //// MJS 2019 - Move HUD to top left corner
//        //hud.hudPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1);
//        //hud.hudPanel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.9f);


//        // Look for any LM_Decsion Points we will want to track
//        if (FindObjectsOfType<LM_DecisionPoint>().Length > 0)
//        {
//            decisionPoints = FindObjectsOfType<LM_DecisionPoint>();

//            // Clear any decisions on LM_DecisionPoints
//            foreach (var pt in decisionPoints)
//            {
//                pt.ResetDecisionPoint();
//            }
//        }

//        if (logStartEnd) startXYZ = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;

//        if (vrEnabled & haptics) SteamVR_Actions.default_Haptic.Execute(0f, 2.0f, 65f, 1f, SteamVR_Input_Sources.Any);
//    }

//    public override bool updateTask ()
//	{
//		base.updateTask();

//        if (skip)
//        {
//            //log.log("INFO    skip task    " + name,1 );
//            return true;
//        }

//        if (score > 0) penaltyTimer = penaltyTimer + (Time.deltaTime * 1000);

//		if (penaltyTimer >= penaltyRate)
//		{
//			penaltyTimer = penaltyTimer - penaltyRate;
//			if (score > 0)
//			{
//				score = score - 1;
//				hud.setScore(score);
//			}
//		}

//        //show target after set time
//        if (hideTargetOnStart != HideTargetOnStart.Off && Time.time - startTime > showTargetAfterSeconds)
//        {

//            switch (hideTargetOnStart)
//            {
//                case HideTargetOnStart.SetInactive:
//                    currentTarget.GetComponent<Collider>().enabled = true;
//                    break;
//                case HideTargetOnStart.SetInvisible:
//                    currentTarget.GetComponent<MeshRenderer>().enabled = true;
//                    break;
//                case HideTargetOnStart.DisableCompletely:
//                    //fixme - at some point should write LM methods to turn off objects, their renderers, their colliders, and/or their lights (including children)
//                    currentTarget.GetComponent<Collider>().enabled = true;
//                    currentTarget.GetComponent<MeshRenderer>().enabled = true;
//                    var halo = (Behaviour)currentTarget.GetComponent("Halo");
//                    if (halo != null) halo.enabled = true;
//                    break;
//                case HideTargetOnStart.SetProbeTrial:
//                    currentTarget.GetComponent<Collider>().enabled = true;
//                    currentTarget.GetComponent<MeshRenderer>().enabled = true;
//                    break;
//                default:
//                    Debug.Log("No hidden targets identified");
//                    currentTarget.SetActive(true);
//                    currentTarget.GetComponent<MeshRenderer>().enabled = true;
//                    currentTarget.GetComponent<Collider>().enabled = true;
//                    break;
//            }
//        }

//        // Keep updating the distance traveled and kill task if they reach max
//        playerDistance += Vector3.Distance(avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position, playerLastPosition);
//        clockwiseTravel += Vector3Angle2D(playerLastPosition, avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position);
//        playerLastPosition = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;

//        if (isScaled)
//        {
//            scaledPlayerDistance += Vector3.Distance(scaledAvatar.transform.position, scaledPlayerLastPosition);
//            scaledPlayerLastPosition = scaledAvatar.transform.position;
//        }

//        // handle the compass objects render (visible or not)
//        if (assistCompass != null)
//        {
//            // Keep the assist compass pointing at the target (even if it isn't visible)
//            var targetDirection = 2 * assistCompass.transform.position - currentTarget.transform.position;
//            targetDirection = new Vector3(targetDirection.x, assistCompass.pointer.transform.position.y, targetDirection.z);
//            assistCompass.pointer.transform.LookAt(targetDirection, Vector3.up);
//            // Show assist compass if and when it is needed
//            if (assistCompass.gameObject.activeSelf == false & SecondsUntilAssist >= 0 & (Time.time - startTime > SecondsUntilAssist))
//            {
//                assistCompass.gameObject.SetActive(true);
//            }
//        }


//        float distanceRemaining = distanceAllotted - playerDistance;
//        float timeRemaining = timeAllotted - (Time.time - startTime);
//        // If we have a place to output ongoing trial info (time/dist remaining), use it
//        if (printRemainingTimeTo != null) 
//        {
//            printRemainingTimeTo.text = string.Format(baseText, Mathf.Round(distanceRemaining), Mathf.Round(timeRemaining));
//        }

//        // End the trial if they reach the max distance allotted
//        if (!isScaled & playerDistance >= distanceAllotted) return true;
//        else if (isScaled & scaledPlayerDistance >= distanceAllotted) return true;
//        // End the trial if they reach the max time allotted
//        if (Time.time - startTime >= timeAllotted) return true;


//        if (killCurrent == true)
//		{
//			return KillCurrent ();
//		}

//        // if we're letting them say when they think they've arrived (only applicable if inside target object colliders)
//        //testPC = avatar.GetComponent<LM_PlayerController>().collisionObject;


//        //foreach (GameObject targetobject in TargetObjectColliders)
//        //{
//        //    Collider targetobjectcollider = targetobject.GetComponent<Collider>();
//        //    Bounds targetobjectbounds = targetobjectcollider.bounds;

//        //    if (targetobjectbounds.Contains(playerLastPosition))
//        //    {
//        //        isPlayerInside = true;
//        //    }
//        //    //else
//        //    //{
//        //    //isPlayerInside = false;
//        //    //}



//        //}
//        //return false;
//        ////if ()
//        ////void OnTriggerEnter(Collider other) 
//        ////{ if (other.gameObject.tag == "TargetObjectColliders")

//        //if (isPlayerInside == true) 
//        //{ 
//            if (Time.time - startTime > allowContinueAfter)          
//                {
//                    if (vrEnabled)
//                    {
//                        if (vrInput.TriggerButton.GetStateDown(SteamVR_Input_Sources.Any))
//                        {
//                            Debug.Log("Participant ended the trial");
//                            log.log("INPUT_EVENT    Player Arrived at Destination    1", 1);
//                            hud.hudPanel.SetActive(false);
//                            hud.setMessage("");

//                            if (haptics) SteamVR_Actions.default_Haptic.Execute(0f, 2.0f, 65f, 1f, SteamVR_Input_Sources.Any);

//                            return true;
//                        }
//                    }
//                    if (Input.GetKeyDown(KeyCode.Return))
//                    {
//                        Debug.Log("Participant ended the trial");
//                        log.log("INPUT_EVENT    Player Arrived at Destination    1", 1);
//                        hud.hudPanel.SetActive(false);
//                        hud.setMessage("");
//                        return true;
//                    }
//                }
//                return false;

//	}

//    //}

//	public override void endTask()
//	{
//		TASK_END();
//		//avatarController.handleInput = false;
//	}

//	public override void TASK_PAUSE()
//	{
//		avatarLog.navLog = false;
//        if (isScaled) scaledAvatarLog.navLog = false;
//		//base.endTask();
//		log.log("TASK_PAUSE\t" + name + "\t" + this.GetType().Name + "\t" ,1 );
//		//avatarController.stop();

//		hud.setMessage("");
//		hud.showScore = false;

//	}

//	public override void TASK_END()
//	{
//		base.endTask();
//        if (printRemainingTimeTo != null) printRemainingTimeTo.text = baseText;
//        var navTime = Time.time - startTime;

//        if (logStartEnd) endXYZ = avatar.GetComponent<LM_PlayerController>().collisionObject.transform.position;

//        //avatarController.stop();
//        avatarLog.navLog = false;
//        if (isScaled) scaledAvatarLog.navLog = false;

//        // close the door if the target was a store and it is open
//        // if it's a target, open the door to show it's active
//        if (currentTarget.GetComponentInChildren<LM_TargetStore>() != null)
//        {
//            currentTarget.GetComponentInChildren<LM_TargetStore>().CloseDoor();
//        }

//        // re-enable everything on the gameobject we just finished finding
//        currentTarget.GetComponent<MeshRenderer>().enabled = true;
//        currentTarget.GetComponent<Collider>().enabled = true; // is this the root cause of problem??
//        var halo = (Behaviour) currentTarget.GetComponent("Halo");
//        if(halo != null) halo.enabled = true;



//        hud.setMessage("");
//		hud.showScore = false;

//        hud.SecondsToShow = hud.GeneralDuration;

//        if (assistCompass != null)
//        {
//            // Hide the assist compass
//            assistCompass.gameObject.SetActive(false);
//        }

//        // Move hud back to center and reset
//        hud.hudPanel.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
//        hud.hudPanel.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);

//        float perfDistance;
//        if (isScaled)
//        {
//            perfDistance = scaledPlayerDistance;
//        }
//        else perfDistance = playerDistance;

//        var excessPath = perfDistance - optimalDistance;

//        // set impossible values if the nav task was skipped
//        if (skip)
//        {
//            navTime = float.NaN;
//            perfDistance = float.NaN;
//            optimalDistance = float.NaN;
//            excessPath = float.NaN;
//        }


//        // log.log("LM_OUTPUT\tNavigationTask.cs\t" + masterTask.name + "\t" + this.name + "\n" +
//        // 	"Task\tBlock\tTrial\tTargetName\tOptimalPath\tActualPath\tExcessPath\tRouteDuration\n" +
//        // 	masterTask.name + "\t" + masterTask.repeatCount + "\t" + parent.repeatCount + "\t" + currentTarget.name + "\t" + optimalDistance + "\t"+ perfDistance + "\t" + excessPath + "\t" + navTime
//        //     , 1);

//        // More concise LM_TrialLog logging
//        taskLog.AddData(transform.name + "_target", currentTarget.name);
//        taskLog.AddData(transform.name + "_actualPath", perfDistance.ToString());
//        taskLog.AddData(transform.name + "_optimalPath", optimalDistance.ToString());
//        taskLog.AddData(transform.name + "_excessPath", excessPath.ToString());
//        taskLog.AddData(transform.name + "_clockwiseTravel", clockwiseTravel.ToString());
//        taskLog.AddData(transform.name + "_duration", navTime.ToString());

//        if (logStartEnd)
//        {

//            taskLog.AddData(transform.name + "_startX", startXYZ.x.ToString());
//            taskLog.AddData(transform.name + "_startZ", startXYZ.z.ToString());
//            taskLog.AddData(transform.name + "_endX", endXYZ.x.ToString());
//            taskLog.AddData(transform.name + "_endZ", endXYZ.z.ToString());

//        }

//        // Record any decisions made along the way
//        if (decisionPoints != null)
//        {
//            foreach (LM_DecisionPoint nexus in decisionPoints)
//            {
//                taskLog.AddData(nexus.name + "_initialChoice", nexus.initialChoice);
//                taskLog.AddData(nexus.name + "_finalChoice", nexus.currentChoice);
//                taskLog.AddData(nexus.name + "_totalChoices", nexus.totalChoices.ToString());

//                nexus.ResetDecisionPoint();
//            }
//        }

//        // Hide the overlay by setting back to empty string
//        //if (overlayTargetObject != null) overlayTargetObject.text = "";

//        // If we created a dummy Objectlist for exploration, destroy it
//        Destroy(GetComponent<ObjectList>());

//        if (canIncrementLists)
//		{
//			destinations.incrementCurrent();
//		}
//        currentTarget = destinations.currentObject();
//    }

//	public override bool OnControllerColliderHit(GameObject hit)
//	{
//		if ((hit == currentTarget | hit.transform.parent.gameObject == currentTarget) & 
//            hideTargetOnStart != HideTargetOnStart.DisableCompletely & hideTargetOnStart != HideTargetOnStart.SetInactive)
//		{
//			if (showScoring)
//			{
//				score = score + scoreIncrement;
//				hud.setScore(score);
//			}
//			return true;
//		}

//		return false;
//	}
//}

