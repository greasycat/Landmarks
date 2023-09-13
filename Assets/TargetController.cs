using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetController : MonoBehaviour
{
    public static TargetController instance;

    [Tooltip("The main experiment")]
    [SerializeField] public Experiment experiment;
    
    [Tooltip("The 'PlayController' Object")]
    [SerializeField] public GameObject player;
    
    [Tooltip("The 'TaskList' which contains all the tasks")]
    [SerializeField] public TaskList taskList;
    
    [Tooltip("The names of the tasks where the visibility script is disabled")]
    [SerializeField] public List<string> exceptTasks;
    
    [Tooltip("The names of the tasks in which only the target painting is displayed")]
    [SerializeField] public List<string> targetOnlyTask;

    [Tooltip("The Navigation object")] [SerializeField]
    public NavigationTask navigationTask;
    
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
}