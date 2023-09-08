using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveObjectFromList : ExperimentTask
{
    [Header("Task-specific Properties")]
    public ObjectList listToRemoveCurrentFrom;
    public ObjectList listToDefineCurrent;

    public override void startTask()
    {
        TASK_START();

        // LEAVE BLANK
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
        var tagforremoval = new GameObject();
        foreach (var obj in listToRemoveCurrentFrom.objects)
        {
            if (obj.GetHashCode() == listToDefineCurrent.currentObject().GetHashCode())
            {
                tagforremoval = obj;
            }
        }
        listToRemoveCurrentFrom.objects.Remove(tagforremoval);
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
