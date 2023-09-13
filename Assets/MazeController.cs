using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeController : MonoBehaviour
{
    [SerializeField] private string defaultName = "Hallway";
    public string region;
    public static MazeController instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        region = defaultName;
    }

    public void EnterRegion(string newRegion)
    {
        region = newRegion;
    }

    public void ExitRegion()
    {
        region = defaultName;
    }
}
