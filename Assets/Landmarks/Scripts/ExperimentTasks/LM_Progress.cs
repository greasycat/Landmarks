using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class LM_Progress : MonoBehaviour
{
    public static LM_Progress Instance { get; private set; }

    [SerializeField] public string rootTaskName = "LM_Timeline";
    [SerializeField] public string applicationName = "Landmarks";
    [SerializeField] public bool resumeLastSave = true;

    [NotEditable] public string currentSaveFile;
    [NotEditable] public string lastSaveFile;
    [NotEditable] public List<string> lastSaveStack;
    [NotEditable] public bool isLastSaveCompleted;
    [NotEditable] public string savePath;

    [NotEditable] public string currentTaskName = "";
    [NotEditable] public int currentTaskIndex = -1;

    [SerializeField] private bool deleteCurrentSaveFileOnEditorQuit = false;
    [SerializeField] private List<string> partiallyCompletedTrials;


    //**************************************************************
    // Initialize singleton instance
    //**************************************************************

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        savePath = GetSystemConfigFolder();
        lastSaveFile = GetLastSaveFile(savePath);
        lastSaveStack = File.ReadAllText(lastSaveFile).Split('\n').ToList();
        currentSaveFile = CreateSaveFile(savePath);
    }


    //**************************************************************
    // Task-related functions
    //**************************************************************
    public void RecordTaskStart(ExperimentTask task)
    {
        var startMarker = "(" + task.name;
        WriteToCurrentSaveFileSync(startMarker);

        if (task is TaskList taskList)
        {
            if (taskList.taskListType == Role.trial)
            {
                partiallyCompletedTrials = GetAllChildrenTask(currentTaskIndex, task.name);
            }
        }

        ShiftCurrentIndex(1, task.name);
    }


    public void RecordTaskEnd(ExperimentTask task)
    {
        var endMarker = task.name + ")";
        WriteToCurrentSaveFileSync(endMarker);
        ShiftCurrentIndex(1, task.name);
    }

    public bool Skippable(ExperimentTask task)
    {
        if (!resumeLastSave) return false;
        if (lastSaveStack.Count == 0) return false;
        if (currentTaskIndex == -1) return false;
        if (currentTaskIndex >= lastSaveStack.Count) return false;

        if (task is TaskList taskList)
        {
            if (taskList.taskListType == Role.trial)
            {
                // join the list of partially completed trials
                var partiallyCompletedTrialsString = string.Join("\n", partiallyCompletedTrials);
                Debug.Log(partiallyCompletedTrialsString);
                
            }
        }


        return false;
    }

    //**************************************************************
    // Save-related functions
    //**************************************************************
    private void ShiftCurrentIndex(int shift, string taskName)
    {
        currentTaskIndex += shift;
        currentTaskName = taskName;
    }

    private List<string> GetAllChildrenTask(int startIndex, string taskName)
    {
        //find the end of the current task in stack
        //return the list of task between
        var i = startIndex;
        for (; i < lastSaveStack.Count; i++)
        {
            var task = lastSaveStack[i];
            if (task.StartsWith($"{taskName})"))
            {
                break;
            }
        }
        return lastSaveStack.GetRange(startIndex, i - startIndex);
    }

    //**************************************************************
    // IO functions
    //**************************************************************
    private void WriteToCurrentSaveFileSync(string text)
    {
        if (string.IsNullOrEmpty(currentSaveFile))
        {
            Debug.LogError("Save file has not been created: " + currentSaveFile + " writing:" + text);
        }

        using (var writer = new StreamWriter(currentSaveFile, true))
        {
            Debug.Log("Writing to save file: " + currentSaveFile + " writing:" + text);
            writer.WriteLine(text);
            writer.Close();
        }
    }


    private void DeleteSaveFile(string saveFile)
    {
        var path = Path.Combine(savePath, saveFile);
        File.Delete(path);
        Debug.Log("Save file deleted: " + path);
    }

    public void DeleteAllSaveFiles()
    {
        var files = Directory.GetFiles(savePath);
        foreach (var file in files)
        {
            File.Delete(file);
            Debug.Log("Save file deleted: " + file);
        }
    }

    public static string GetLastSaveFile(string filepath)
    {
        var files = Directory.GetFiles(filepath);
        var latest = DateTime.MinValue;
        var latestFile = "";
        foreach (var file in files)
        {
            var timestamp = File.GetCreationTime(file);
            if (timestamp <= latest) continue;

            latest = timestamp;
            latestFile = file;
        }

        return latestFile;
    }

    public string GetSystemConfigFolder()
    {
        var configFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        // create a path at the config folder
        var path = Path.Combine(configFolder, applicationName);

        // create the folder if it doesn't exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        Debug.Log("Config folder: " + path);
        return path;
    }

    //function to create a save file with current timestamp
    public static string CreateSaveFile(string folderPath)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var saveFile = Path.Combine(folderPath, "save_" + timestamp + ".txt");
        File.Create(saveFile).Dispose();
        if (!File.Exists(saveFile))
        {
            Debug.LogError("Save file not created: " + saveFile);
            throw new Exception("Save file not created: " + saveFile);
        }

        Debug.Log("Save file created: " + saveFile);
        return saveFile;
    }

    //**************************************************************
    // Debug functions
    //**************************************************************

    public static void PrintAllTaskInOrder()
    {
        var root = GameObject.Find("LM_Timeline");
        var rootTask = root.GetComponent<TaskList>();
        rootTask.Traverse((obj) => Debug.Log(obj.name), (_) => false);
    }

    public static void PrintAllNonTrialTask()
    {
        var root = GameObject.Find("LM_Timeline");
        var rootTask = root.GetComponent<TaskList>();
        rootTask.Traverse((obj) =>
        {
            // Debug.Log(obj.name);
        }, (task) =>
        {
            if (task.GetType() != typeof(TaskList)) return false;
            var taskList = task as TaskList;
            if (taskList == null) return false;
            return taskList.taskListType == Role.trial;
        });
    }

    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        if (deleteCurrentSaveFileOnEditorQuit)
        {
            DeleteSaveFile(currentSaveFile);
        }
#endif
    }
}