using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class LM_ProgressController : MonoBehaviour
{
    public LM_ProgressController Instance { get; private set; }
    [SerializeField] private readonly string _rootTaskName = "LM_Timeline";
    [SerializeField] private string applicationName = "Landmarks";
    [SerializeField] private string currentSaveFile;
    private string configPath;

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
        configPath = GetSystemConfigFolder();
        CreateSaveFile();
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
    public void CreateSaveFile()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var saveFile = Path.Combine(configPath, "save_" + timestamp + ".txt");
        File.Create(saveFile);
        if (!File.Exists(saveFile))
        {
            Debug.LogError("Save file not created: " + saveFile);
            return;
        }
        Debug.Log("Save file created: " + saveFile);
        currentSaveFile = saveFile;
    }
    
    public string GetLastSaveFile()
    {
        var files = Directory.GetFiles(configPath);
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

    public void DeleteSaveFile(string saveFile)
    {
        var path = Path.Combine(configPath, saveFile);
        File.Delete(path);
        Debug.Log("Save file deleted: " + path);
    }

    public void DeleteAllSaveFiles()
    {
        var files = Directory.GetFiles(configPath);
        foreach (var file in files)
        {
            File.Delete(file);
            Debug.Log("Save file deleted: " + file);
        }
    }
    
    
    public IEnumerator WriteToCurrentSaveFile(string text)
    {
        yield return new WaitForSeconds(0.1f);
        if (currentSaveFile == null) yield break;
        File.AppendAllText(currentSaveFile, text+"\n");
    }
    
    public static void PrintAllTaskInOrder()
    {
        var root = GameObject.Find("LM_Timeline");
        var rootTask = root.GetComponent<TaskList>();
        rootTask.Traverse((obj) => Debug.Log(obj.name), (_)=>false);
    }

    public static void PrintAllNonTrialTask()
    {
        var root = GameObject.Find("LM_Timeline");
        var rootTask = root.GetComponent<TaskList>();
        rootTask.Traverse((obj) =>
        {
            Debug.Log(obj.name);
        }, (task) =>
            {
                if (task.GetType() != typeof(TaskList)) return false;
                var taskList = task as TaskList;
                if (taskList == null) return false;
                return taskList.taskListType == Role.trial;
            });
    }
}