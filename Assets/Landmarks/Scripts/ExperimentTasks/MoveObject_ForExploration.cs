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
using Landmarks.Scripts.Progress;

public class MoveObject_ForExploration : ExperimentTask {

	[Header("Task-specific Properties")]
	public GameObject start;
	public GameObject destination;
	public ObjectList destinations;
	public bool useLocalRotation = true;
	
	public bool swap;
	private static Vector3 position;
	private static Quaternion rotation;
	public ObjectList navTrialDestinations; // list of destinations that will pop up in the test phase; drag an object list here
	public GameObject currentNavTrialDestination; // current trial destination
	//public bool isExplorationPhase = true;


	public override void startTask () {
		TASK_START();
	}	

	public override void TASK_START()
	{
		base.startTask();
		
		if (!manager) Start();
		
		
		if (skip) {
			log.log("INFO	skip task	" + name,1 );
			return;
		}
		
  //      if (destinations)
  //      {
  //          destination = destinations.currentObject();
  //      }

		//if (isExplorationPhase == false)
		//{
		//	Debug.Log("we are in the wayfinding phase");
		//	currentNavTrialDestination = navTrialDestinations.currentObject();//listing current trial destination

  //          //checking if footprints have spawned near nav trial destination, and if so, to spawn to another location in the list
  //          if (destination.name == currentNavTrialDestination.name)
  //          {
  //              Debug.Log("Footprints location same as trial destination!! Reassigning footprints Location");
  //              int position = destinations.objects.IndexOf(destination);

  //              for (int i = 0; i < destinations.objects.Count; i++)
  //              {
  //                  int newIndex = Random.Range(i, destinations.objects.Count);
  //                  if (newIndex == position)
  //                  {
  //                      int newIndex2 = Random.Range(i, destinations.objects.Count);
  //                      destination = destinations.objects[newIndex2];
  //                  }
  //                  else
  //                  {
  //                      destination = destinations.objects[newIndex];
  //                  }

  //              }
  //          }
  //      }
		//isExplorationPhase = false;
		Debug.Log("we are in the exploration phase");

        
		position = start.transform.position;
		if (useLocalRotation) rotation = start.transform.localRotation;
        else rotation = start.transform.rotation;

        start.transform.position = destination.transform.position;
        log.log("TASK_ROTATE\t" + start.name + "\t" + this.GetType().Name + "\t" + start.transform.localEulerAngles.ToString("f1"),1);

        if (useLocalRotation) start.transform.localRotation = destination.transform.localRotation;
        else start.transform.rotation = destination.transform.rotation;
        log.log("TASK_POSITION\t" + start.name + "\t" + this.GetType().Name + "\t" + start.transform.transform.position.ToString("f1"),1);


        LM_Progress.Instance.ResumeLastPlayerPositionToNavStart(start.transform);
		
		if (swap) {
			destination.transform.position = position;
			if (useLocalRotation) destination.transform.localRotation = rotation;
			else destination.transform.rotation = rotation;
		}
	}
	
	public override bool updateTask () {
	    return true;
	}
	public override void endTask() {
		TASK_END();
	}
	
	public override void TASK_END() {
		base.endTask();
		
		if ( destinations ) {
			if (canIncrementLists)
			{
				destinations.incrementCurrent();
				destination = destinations.currentObject();
			}
		}
	}
}
