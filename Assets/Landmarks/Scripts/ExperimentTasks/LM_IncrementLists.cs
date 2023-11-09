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

public class LM_IncrementLists : ExperimentTask
{
    [Header("Task-specific Properties")]
    public List<ObjectList> lists = new List<ObjectList>();
    private bool subsetsAdded;

    public override void startTask()
    {
        TASK_START();
        // Only add the subsets if they haven't already been added
        //if (!subsetsAdded)
        //{
        //    // Find the PermuteStartTargetPairs_subset0 and PermuteStartTargetPairs_subset1 game objects
        //    GameObject subset0 = GameObject.Find("PermuteStartTargetPairs_subset0");
        //    GameObject subset1 = GameObject.Find("PermuteStartTargetPairs_subset1");

        //    // Add the ObjectList components of these game objects to the lists property
        //    lists.Add(subset0.GetComponent<ObjectList>());
        //    lists.Add(subset1.GetComponent<ObjectList>());

        //    // Set the flag to indicate that the subsets have been added
        //    subsetsAdded = true;
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
        Increment();
    }

    public void Increment(int increment = 1)
    {
        foreach (var list in lists)
        {
            list.incrementCurrent(increment);
        }
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