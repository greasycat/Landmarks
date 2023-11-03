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
using Valve.Newtonsoft.Json.Utilities;

public class ObjectList : ExperimentTask {

    [Header("Task-specific Properties")]

    public string parentName = "";
	public GameObject parentObject;
	public int current = 0;
	
	public List<GameObject> objects;
	public EndListMode EndListBehavior; 
	public bool shuffle;
    // public GameObject order; // DEPRICATED

    
	public override void startTask () {
		
        PopulateObjects(out var objs);
		
		if (progress.CheckIfResumeCurrentNode(this))
		{
			var objectString = progress.GetCurrentNodeAttribute("objects");
			if (objectString != null)
			{
				var objectList = objs.ToList();
				var orderedNames = objectString.Split(',').ToList();
				objs = objectList.OrderBy(obj => orderedNames.IndexOf(obj.name)).ToArray();
			}
		}
		else if (shuffle)
		{
			Debug.Log("Shuffling objects");
			Experiment.Shuffle(objs);				
		}
		
		if (progress != null)
		{
			progress.AddAttribute("objects", string.Join(",", objs.Select(obj => obj.name)));
		}
		else
		{
			Debug.LogWarning("No progress object found for task " + name);
		}
		TASK_START();
	 
		foreach (GameObject obj in objs) {	             
        	objects.Add(obj);
			log.log("TASK_ADD	" + name  + "\t" + this.GetType().Name + "\t" + obj.name  + "\t" + "null",1 );
		}
	}

	public void PopulateObjects(out GameObject[] objs)
	{
		
        if (objects.Count == 0)
        {
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
	
	public override void TASK_ADD(GameObject go, string txt) {
		objects.Add(go);
	}
	
	public override void TASK_START()
	{
		base.startTask();		
		if (!manager) Start();

		objects = new List<GameObject>();
	}
	
	public override bool updateTask () {
	    return true;
	}
	public override void endTask() {
		//current = 0;
		TASK_END();
	}
	
	public override void TASK_END() {
		base.endTask();
	}
	
	public GameObject currentObject() {
		if (current >= objects.Count) {
			return null;
		} else {
			return objects[current];
		}
	}
	
	public new void incrementCurrent() 
	{
		current++;
		
		if (current >= objects.Count && EndListBehavior == EndListMode.Loop) {
			current = 0;
		}
	}
}
