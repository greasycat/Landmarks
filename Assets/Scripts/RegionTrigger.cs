using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionTrigger : MonoBehaviour
{
    // Start is called before the first frame update

    private string _regionName;
    private void Start()
    {
        _regionName = transform.parent.name;
    }

    // Update is called once per frame

    private void OnTriggerEnter(Collider other)
    {
        MazeController.instance.EnterRegion(_regionName);
    }

    private void OnTriggerExit(Collider other)
    {
        MazeController.instance.ExitRegion();
    }
}
