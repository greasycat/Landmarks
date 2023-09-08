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
using UnityEngine;

public class AssignSubsets : ExperimentTask
{
    [Header("Task-specific Properties")]
    public NavigationTask navigationTaskReference;
    public NavigationTask navigationTaskReference2;
    public LM_IncrementLists incrementListsReference;
    public InstructionsTask instructionsTaskReference;
    public InstructionsTask instructionsTaskReference2;
    public MoveObject moveObjectReference;
    public MoveObject moveObjectReference2;
    //public TL_WayfindingTrialsCounter tL_WayfindingTrialsCounter;


    public override void startTask()
    {
        TASK_START();
        // Find the PermuteStartTargetPairs_subset0 and PermuteStartTargetPairs_subset1 game objects
        //GameObject subset0 = GameObject.Find("PermuteStartTargetPairs_subset0");
        //GameObject subset1 = GameObject.Find("PermuteStartTargetPairs_subset1");
        GameObject subset0 = GameObject.FindGameObjectWithTag("StartingLocations");
        GameObject subset1 = GameObject.FindGameObjectWithTag("NavigationLocations");

        // Assign the ObjectList components of these game objects to the NavigationTask script
        navigationTaskReference.listOfNavStarts = subset0.GetComponent<ObjectList>();
        navigationTaskReference.destinations = subset1.GetComponent<ObjectList>();

        // Assign the ObjectList components of these game objects to the NavigationTask script (repeat trials)
        navigationTaskReference2.listOfNavStarts = subset0.GetComponent<ObjectList>();
        navigationTaskReference2.destinations = subset1.GetComponent<ObjectList>();

        // Add the ObjectList components of these game objects to the LM_IncrementLists script
        incrementListsReference.lists.Add(subset0.GetComponent<ObjectList>());
        incrementListsReference.lists.Add(subset1.GetComponent<ObjectList>());

        // Assign the ObjectList components of these game objects to the InstructionsTask script
        instructionsTaskReference.objects = subset1.GetComponent<ObjectList>();
        Debug.Log("Assigned subsets to NavigationTask, IncrementLists, InstructionsTask");

        // Assign the ObjectList components of these game objects to the InstructionsTask script (repeat trials)
        instructionsTaskReference2.objects = subset1.GetComponent<ObjectList>();
        Debug.Log("Assigned subsets to NavigationTask, IncrementLists, InstructionsTask");

        // Assign the ObjectList components of these game objects to the MoveObject script
        moveObjectReference.destinations = subset0.GetComponent<ObjectList>();

        // Assign the ObjectList components of these game objects to the MoveObject script (repeat trials)
        moveObjectReference2.destinations = subset0.GetComponent<ObjectList>();

        // Assign the ObjectList components of these game objects to the MoveObject script
        //tL_WayfindingTrialsCounter.objectListReference = subset1.GetComponent<ObjectList>();


    }


    public override void TASK_START()
    {
        if (!manager) Start();
        base.startTask();

        if (skip)
        {
            log.log("INFO    skip task    " + name, 1);
            return;
        }

        // WRITE TASK STARTUP CODE HERE
    }


    public override bool updateTask()
    {
        return true;

        // WRITE TASK UPDATE CODE HERE
    }


    public override void endTask()
    {
        TASK_END();

        // LEAVE BLANK
    }


    public override void TASK_END()
    {
        base.endTask();

        // WRITE TASK EXIT CODE HERE
    }

}