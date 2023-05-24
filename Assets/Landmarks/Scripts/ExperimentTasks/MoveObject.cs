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

public class MoveObject : ExperimentTask {

	[Header("Task-specific Properties")]
	public GameObject start;
	public GameObject destination;
	public ObjectList destinations;
	public bool useLocalRotation = true;
	
	public bool swap;
	private static Vector3 position;
	private static Quaternion rotation;
	public ObjectList navTrialDestinations; //list of trial destinations
	public GameObject currentNavTrialDestination; //current trial destination 


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
		
		if ( destinations ) {
			destination = destinations.currentObject();		
		}
		
		//currentNavTrialDestination = navTrialDestinations.currentObject();//listing current trial destination 

		////checking if footprints have spawned near nav trial destination, and if so, to spawn to another location in the list
		//if (destination.name == currentNavTrialDestination.name)
  //      {
		//	Debug.Log("Footprints location same as trial destination!! Reassigning footprints Location"); 
		//	int position = destinations.objects.IndexOf(destination);
			
		//		for (int i=0; i < destinations.objects.Count; i ++)
		//	{ 	int newIndex = Random.Range(i, destinations.objects.Count);

		//		if (newIndex == position)
  //              {
		//			int newIndex2 = Random.Range(i, destinations.objects.Count);
		//			destination = destinations.objects[newIndex2];
  //              }

  //              else
  //              {
		//			destination = destinations.objects[newIndex];
		//		}
				
  //          }
  //      }

		//trialIndex = List.Index(navTrialDestinations, navTrialDestinations.currentObject); //finding index of current object in navTrialDestinations list 
		//position = start.transform.position;

		/* //checking if footprints have spawned near nav trial destination, and if so, spawn to next location
		if (destination.name == currentNavTrialDestination.name){
			destination = destinations.currentObject()!=navTrialDestinations.currentObject.name; // need to select any object from destinations list 
			//that is not current index, and assign this as the new object for the destination variable  

			//how to write, spawn at any other spawn location except this one? Instantiate(start,start.transform.position, )
			//Vector3.Distance(start.transform.position,currentNavTrialDestination.transform.position) <= 2){

		} */

		if (useLocalRotation) rotation = start.transform.localRotation;
        else rotation = start.transform.rotation;

		
			start.transform.position = destination.transform.position;
			log.log("TASK_ROTATE\t" + start.name + "\t" + this.GetType().Name + "\t" + start.transform.localEulerAngles.ToString("f1"),1);

			if (useLocalRotation) start.transform.localRotation = destination.transform.localRotation;
			else start.transform.rotation = destination.transform.rotation;
			log.log("TASK_POSITION\t" + start.name + "\t" + this.GetType().Name + "\t" + start.transform.transform.position.ToString("f1"),1);
		
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
