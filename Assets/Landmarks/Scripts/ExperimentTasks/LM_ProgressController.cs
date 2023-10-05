using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class LM_ProgressController : MonoBehaviour
{
    public static LM_ProgressController Instance { get; private set; }
    [SerializeField] public string rootTaskName = "LM_Timeline";
    [SerializeField] public string applicationName = "Landmarks";
    [NotEditable] public string currentSaveFile;
    [NotEditable] public string lastSaveFile;
    [NotEditable] public List<string> lastSaveStack;
    [NotEditable] public bool isLastSaveCompleted;
    [FormerlySerializedAs("configPath")] [NotEditable] public string savePath;
    
    [SerializeField] private bool deleteCurrentSaveFileOnEditorQuit = false;
    private FileStream currentSaveFileStream;
    private StreamWriter currentSaveWriter;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("Singleton ProgressController created");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        savePath = GetSystemConfigFolder();
        lastSaveFile = GetLastSaveFile(savePath);
        lastSaveStack = File.ReadAllText(lastSaveFile).Split('\n').ToList();
        isLastSaveCompleted = CheckIfLastSaveIsCompleted();
        currentSaveFile = CreateSaveFile(savePath);
        currentSaveFileStream = new FileStream(currentSaveFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        if (currentSaveFileStream == null)
        {
            Debug.LogError("Save file not created: " + currentSaveFile);
            throw new Exception("Save file not created: " + currentSaveFile);
        }
        currentSaveWriter = new StreamWriter(currentSaveFileStream);

        Debug.Log("Last save file: " + string.Join(",", lastSaveStack));
    }



    private IEnumerator WriteToCurrentSaveFile(string text)
    {
        if (currentSaveFile == null || currentSaveFileStream == null) yield break;
        currentSaveWriter.WriteLine(text);
        currentSaveWriter.Flush();
    }

    public void RecordTaskStart(string taskName)
    {
        StartCoroutine(WriteToCurrentSaveFile("(" + taskName));
    }

    public void RecordTaskEnd(string taskName)
    {
        StartCoroutine(WriteToCurrentSaveFile(taskName + ")"));
    }

    public void GetFirstTaskInLastSave()
    {
        // Get the first line of the last save stack
        if (lastSaveStack.Count == 0) return;
        var lastTask = lastSaveStack[0];
        // match "(taskName"
        if (!lastTask.StartsWith("(")) return;
        var taskName = lastTask.Substring(1);
        Debug.Log("Last task: " + taskName);
    }

    public void MarkTaskComplete(string taskName)
    {
        // Get the first line of the last save stack
        if (lastSaveStack.Count == 0) return;
        var firstTask = lastSaveStack[0];
        // match "(taskName"
        if (!firstTask.StartsWith("(" + taskName)) return;
        // remove the first line
        lastSaveStack.RemoveAt(0);
    }

    private bool CheckIfLastSaveIsCompleted()
    {
        // Get the first line of the last save stack
        return lastSaveStack.Count >= 1
               && lastSaveStack[0].StartsWith("(" + rootTaskName)
               && lastSaveStack[lastSaveStack.Count - 1].StartsWith(rootTaskName + ")");
    }

    private bool SkipTheWholeTask(string taskName)
    {
        // Get the first line of the last save stack
        if (lastSaveStack.Count == 0) return false;
        var firstTask = lastSaveStack[0];
        // match "(taskName"
        if (!firstTask.StartsWith("(" + taskName)) return false;
        // try to find the end of the task
        var endTask = lastSaveStack.FindIndex((line) => line.StartsWith(taskName + ")"));
        if (endTask == -1) return false;
        // remove the whole task
        lastSaveStack.RemoveRange(0, endTask + 1);
        return true;
    }

    public void DeleteSaveFile(string saveFile)
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