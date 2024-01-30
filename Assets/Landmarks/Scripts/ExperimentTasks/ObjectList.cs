/*
    Copyright (C) 2010  Jason Laczko

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Landmarks.Scripts.Progress;
using Newtonsoft.Json;
using Valve.Newtonsoft.Json.Utilities;

public class ObjectList : ExperimentTask
{
    [Header("Task-specific Properties")] public string parentName = "";
    public GameObject parentObject;
    public int current = 0;

    public List<GameObject> objects;
    public EndListMode EndListBehavior;

    public bool resetList;
    public bool shuffle;
    // public GameObject order; // DEPRICATED


    public override void startTask()
    {
        PopulateObjects(out var objs);

        var progress = LM_Progress.Instance;

        if (progress != null && progress.CheckIfResumeCurrentNode(this))
        {
            var objectJsonString = progress.GetCurrentNodeAttribute("objects");
            if (objectJsonString != null)
            {
                var lookUp = Serializer.ConvertToLookupDictionary(objs);
                Debug.Log("Keys: " + string.Join(",", lookUp.Keys.ToArray()));
                try
                {
                    Debug.Log("Json" + objectJsonString);
                    var objectList =
                        Serializer.Deserialize<List<Dictionary<string, string>>>(objectJsonString);

                    objs = Serializer.ConvertToGameObjectList(objectList, lookUp).ToArray();
                }
                catch (JsonReaderException e)
                {
                    Debug.LogError("Cannot deserialize object list, fail to load resume save " + e.Message);
                }
            }
        }


        if (objects.Count == 0 || resetList) {
                    shuffle = false;
        }

        if (shuffle)
        {
            Debug.Log("Shuffling objects");
            Experiment.Shuffle(objs);
        }

        if (objs == null)
        {
            Debug.LogError("Name: " + name +"No objects found for objectlist.");
        }

        if (objs != null)
        {
            progress.AddAttribute("objects",
                Serializer.Serialize(Serializer.ConvertToDictionaryList(objs)));
        }

        TASK_START();


        foreach (GameObject obj in objs)
        {
            objects.Add(obj);
            log.log("TASK_ADD	" + name + "\t" + this.GetType().Name + "\t" + obj.name + "\t" + "null", 1);
        }
    }


    public void PopulateObjects(out GameObject[] objs)
    {
        if (objects.Count == 0)
        {
			//Debug.LogError("The if statement is running");
			if (resetList)
			{
				Debug.LogError("RESETTING LIST");
				objects.Clear();
				current = 0;
			}

            if (parentObject == null & parentName == "") Debug.LogError("No objects found for objectlist.");

            // If parentObject is left blank and parentName is not, use parentName to get parentObject
            if (parentObject == null && parentName != "")
            {
                parentObject = GameObject.Find(parentName);
            }

            objs = new GameObject[parentObject.transform.childCount];

            Array.Sort(objs);

            for (int i = 0; i < parentObject.transform.childCount; i++)
            {
                objs[i] = parentObject.transform.GetChild(i).gameObject;
            }
        }
        else
        {
            objs = new GameObject[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                objs[i] = objects[i];
            }
        }
    }

    public override void TASK_ADD(GameObject go, string txt)
    {
        objects.Add(go);
    }

    public override void TASK_START()
    {
        base.startTask();
        if (!manager) Start();

        objects = new List<GameObject>();
    }

    public override bool updateTask()
    {
        return true;
    }

    public override void endTask()
    {
        //current = 0;
        TASK_END();
    }

    public override void TASK_END()
    {
        base.endTask();
    }

    public GameObject currentObject()
    {
        if (current >= objects.Count)
        {
            return null;
        }
        else
        {
            return objects[current];
        }
    }

    public new void incrementCurrent(int increment = 1)
        //TL TLDR: increments the "current" trial by adding 1
    {
        current += increment;
        //TL: When running this method: current (the trial number?) increments by 1
        // When Object.count is 0, it means the object list is uninitialized, however, resume function occur before this method is called, so it should be fine
        if (current >= objects.Count && objects.Count != 0 && EndListBehavior == EndListMode.Loop)
            //don't need to worry about this if loop, since our end behavior is set to "end" and not "loop"
        {
            Debug.Log("objects.Count: " + objects.Count);
            current = 0;
        }
    }

    //    public new void incrementCurrent()
    //    {
    //        current++;
    //
    //        if (current >= objects.Count && EndListBehavior == EndListMode.Loop)
    //        {
    //            current = 0;
    //        }
    // 	else
    // 	{
    // 		objs = new GameObject[objects.Count];
    // 		for (int i = 0; i < objects.Count; i++)
    // 		{
    // 			objs[i] = objects[i];
    // 		}
    // 	}
    //
    // 	// DEPRICATED
    // 	// if (order ) {
    // 	// 	// Deal with specific ordering
    // 	// 	ObjectOrder ordered = order.GetComponent("ObjectOrder") as ObjectOrder;
    //
    // 	// 	if (ordered) {
    // 	// 		Debug.Log("ordered");
    // 	// 		Debug.Log(ordered.order.Count);
    //
    // 	// 		if (ordered.order.Count > 0) {
    // 	// 			objs = ordered.order.ToArray();
    // 	// 		}
    // 	// 	}
    // 	// }
    //
    // 	if ( shuffle ) {
    // 		Experiment.Shuffle(objs);
    // 	}
    //
    // 	TASK_START();
    //
    // 	foreach (GameObject obj in objs) {
    //        	objects.Add(obj);
    // 		log.log("TASK_ADD	" + name  + "\t" + this.GetType().Name + "\t" + obj.name  + "\t" + "null",1 );
    // 	}
    // }
}
