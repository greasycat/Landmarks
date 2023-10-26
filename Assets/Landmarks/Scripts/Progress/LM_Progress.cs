using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Landmarks.Scripts.Debugging;
using UnityEngine;

namespace Landmarks.Scripts.Progress
{
    public class LM_Progress : MonoBehaviour
    {
        public static LM_Progress Instance { get; private set; }

        // Config variables
        [SerializeField] public string rootTaskName = "LM_Timeline";
        [SerializeField] public string applicationName = "Landmarks";
        [SerializeField] public bool resumeLastSave = true;

        // Saves-related variables
        [NotEditable] public string currentSaveFile;
        [NotEditable] public string lastSaveFile;
        [NotEditable] public List<string> lastSaveStack;
        [NotEditable] public string savingFolderPath;

        // Task-related variables
        [NotEditable] public string currentTaskName = "";
        [NotEditable] public int currentIndex = -1;

        [NotEditable] private List<string> tasksToSkip;
        
        // Debug variables
        [SerializeField] private bool deleteCurrentSaveFileOnEditorQuit = false;
        [SerializeField] private int depth = 0;


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
            tasksToSkip = new List<string>();
            
            savingFolderPath = GetSystemConfigFolder();
            LoadLastSave();
            PrepareNewSave();
        }


        //**************************************************************
        // Task-related methods
        //**************************************************************
    
        /// <summary>
        /// Record the start of a task
        /// This method will be inside the ExperimentTask.startTask() method
        /// 1. It will write the task info to the save file
        /// 2. It will check if the task is skippable and skip it if it is
        /// 3. It will update the current resume index
        /// One should always call RecordTaskEnd() after calling this method
        /// </summary>
        /// <param name="task"> The task that needs to be recorded </param>
        public void RecordTaskStart(ExperimentTask task)
        {
            WriteToCurrentSaveFileSync(new XmlTag(task.name).ToString());
            LM_Debug.Instance.Log($"Recording start: {task.name}", 1);

            // Next Index here can be either the current index or the next index of child
            var nextIndex = TrySkip(task);
            ShiftCurrentIndex(nextIndex);
        }


        /// <summary>
        /// Record the end of a task
        /// This method will be inside the ExperimentTask.endTask() method
        /// </summary>
        /// <param name="task"></param>
        public void RecordTaskEnd(ExperimentTask task)
        {
            LM_Debug.Instance.Log($"Recording stop: {task.name}", 1);
            
            depth--;
            WriteToCurrentSaveFileSync(XmlTag.BuildClosingString(task.name));
            ShiftCurrentIndex(currentIndex + 1);
        }

        private int TrySkip(ExperimentTask task)
        {
            var nextIndex = currentIndex + 1;

            // Check if the task is set to skippable when resuming
            if (!task.skipIfResume) return nextIndex;

            // Check if we are doing a resume
            if (!resumeLastSave) return nextIndex;

            if (currentIndex == -1) return nextIndex;

            // Check if the task stack is empty 
            if (lastSaveStack.Count == 0) return nextIndex;

            // Check if index is going out of bound
            if (currentIndex >= lastSaveStack.Count) return nextIndex;
        
            // check if the task is a an task list, if not just move to next task
            if (!(task is TaskList taskList)) return nextIndex;
        
        
            // TaskList means we have to deal with child tasks skipping

            // Get the last taskName
            var lastTask = lastSaveStack[currentIndex];
            // if the the last task is not the current task, then we don't need to skip, we just move to next task
            if (!lastTask.StartsWith($"({task.name}:")) return nextIndex;
        
            // Handle non-trial type TaskList 
            // - Here we want to skip all the children tasks of the current task
            // - Meaning skip straight to end of the current task, denoted by "taskName)"
            if (taskList.taskListType != Role.trial)
            {
                var taskRange = GetTaskIndexRange(currentIndex, task.name);

                LM_Debug.Instance.Log($"Task: {task.name}, current task {lastSaveStack[currentIndex]}", 1);
                LM_Debug.Instance.Log($"Child range: {taskRange.Item1}, {taskRange.Item2}", 1);
            
            
                // Just redo the current task if we cannot find the end of the task
                if (taskRange.Item2 == -1)
                {

                    LM_Debug.Instance.Log($"Found the interrupted task: {task.name}", 2);
                    
                    if (task.stopResumeIfNotCompleted)
                    {
                        LM_Debug.Instance.Log($"Stop resuming", 2);
                        resumeLastSave = false;
                        return nextIndex;
                    }
                    
                    return nextIndex;
                }
            
                nextIndex = taskRange.Item2;
            
                task.skip = true;
            
            }
            // Now deal with trial type TaskList
            // We need to get all the completed trials and skip them
            else
            {
                var taskRange = GetTaskIndexRange(currentIndex, task.name);
                if (taskRange.Item2 == -1)
                {
                    UpdateSkippableTrial(taskRange);
                }
            }

            // If the task is not a trial
            return nextIndex;
        }

        public bool CheckIfCurrentTrialIsCompleted()
        {
            var tempIndex = currentIndex;
            var tempName = currentTaskName;


            return true;
        }

        //**************************************************************
        // Save-related methods
        //**************************************************************
        private void LoadLastSave()
        {
            if (!resumeLastSave)
            {
                lastSaveStack = new List<string>();
                return;
            }

            lastSaveFile = GetLastSaveFile(savingFolderPath);
            if (lastSaveFile == "")
            {
                lastSaveStack = new List<string>();
                return;
            }

            lastSaveStack = File.ReadAllText(lastSaveFile).Split('\n').ToList();
            RemoveEmptyLines(lastSaveStack);
        }

        private void PrepareNewSave()
        {
            currentSaveFile = CreateSaveFile(savingFolderPath);
        }

        private void ShiftCurrentIndex(int shift)
        {
            if (shift >= lastSaveStack.Count) return;

            currentIndex = shift;
            currentTaskName = lastSaveStack[shift];
        }
        
        private Tuple<int, int> GetTaskIndexRange(int startIndex, string taskName)
        {
            //find the end of the current task in stack
            //return the list of task between
            var i = startIndex + 1;
            var duplicateCount = 0;
            for (; i < lastSaveStack.Count; i++)
            {
                var task = lastSaveStack[i];

                if (task.StartsWith($"({taskName}:"))
                {
                    duplicateCount++;
                }
                else if (task.StartsWith($"{taskName})"))
                {
                    duplicateCount--;
                }

                if (duplicateCount == -1)
                {
                    return new Tuple<int, int>(startIndex, i);
                }
            }
            return new Tuple<int, int>(startIndex, -1);
        }
        

        /// <summary>
        /// The methods will return a list of child tasks that can be skipped
        /// One should only call this method when trying to skip the parental task
        /// </summary>
        /// <param name="range">The range should try to include the start and end index for the parent task</param>
        private void UpdateSkippableTrial(Tuple<int, int> range)
        {
            if (range.Item1 == -1 || range.Item2 == -1) return;
            // define a stack to store the tokens
            var tokens = new Stack<string>();
            
            for (var i = range.Item1 + 1; i < range.Item2; i++)
            {
                var taskToken = lastSaveStack[i].TrimEnd();
                LM_Debug.Instance.Log("token: " + taskToken, 2);
                // push token
                if (taskToken.StartsWith("("))
                {
                    var taskName = taskToken.Substring(1, taskToken.IndexOf(":", StringComparison.Ordinal) - 1);
                    LM_Debug.Instance.Log("pushing: " + taskName, 2);
                    tokens.Push(taskName);
                }
                // check and pop
                else if (taskToken.EndsWith(")"))
                {

                    var taskName = taskToken.Substring(0, taskToken.Length - 1);
                    var top = tokens.Peek();
                    
                    LM_Debug.Instance.Log($"matching: {top} with {taskName}", 2);
                    if (top == taskName)
                    {
                        tokens.Pop();
                        tasksToSkip.Add(taskName);
                    }
                }
            }

            LM_Debug.Instance.Log("tasks to skip" +string.Join(", ", tasksToSkip), 2);
        }




        //**************************************************************
        // IO methods
        //**************************************************************
        private void WriteToCurrentSaveFileSync(string text)
        {
            if (string.IsNullOrEmpty(currentSaveFile))
            {
                LM_Debug.Instance.LogError("Save file has not been created: " + currentSaveFile + " writing:" + text);
            }

            using (var writer = new StreamWriter(currentSaveFile, true))
            {
                LM_Debug.Instance.Log("Writing to save file: " + currentSaveFile + " writing:" + text, 0);
                writer.WriteLine(text);
                writer.Close();
            }
        }


        private void DeleteSaveFile(string saveFile)
        {
            var path = Path.Combine(savingFolderPath, saveFile);
            File.Delete(path);
            LM_Debug.Instance.Log("Save file deleted: " + path);
        }

        public void DeleteAllSaveFiles()
        {
            var files = Directory.GetFiles(savingFolderPath);
            foreach (var file in files)
            {
                File.Delete(file);
                LM_Debug.Instance.Log("Save file deleted: " + file);
            }
        }

        public static string GetLastSaveFile(string filepath)
        {
            var files = Directory.GetFiles(filepath);
            if (files.Length == 0)
            {
                LM_Debug.Instance.LogWarning("No save file found");
                return "";
            }
        

            var latest = DateTime.MinValue;
            var latestFile = "";
            foreach (var file in files)
            {
                if (!file.EndsWith(".txt")) continue;
            
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

            LM_Debug.Instance.Log("Config folder: " + path);
            return path;
        }

        //method to create a save file with current timestamp
        public static string CreateSaveFile(string folderPath)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var saveFile = Path.Combine(folderPath, "save_" + timestamp + ".txt");
            File.Create(saveFile).Dispose();
            if (!File.Exists(saveFile))
            {
                LM_Debug.Instance.LogError("Save file not created: " + saveFile);
                throw new Exception("Save file not created: " + saveFile);
            }

            LM_Debug.Instance.Log("Save file created: " + saveFile);
            return saveFile;
        }

        //**************************************************************
        // Helper methods
        //**************************************************************
        private static void RemoveEmptyLines(List<string> lines)
        {
            lines.RemoveAll(string.IsNullOrWhiteSpace);
        }
        //**************************************************************
        // Debug methods
        //**************************************************************

        public static void PrintAllTaskInOrder()
        {
            var root = GameObject.Find("LM_Timeline");
            var rootTask = root.GetComponent<TaskList>();
            rootTask.Traverse((obj) => LM_Debug.Instance.Log(obj.name), (_) => false);
        }

        public static void PrintAllNonTrialTask()
        {
            var root = GameObject.Find("LM_Timeline");
            var rootTask = root.GetComponent<TaskList>();
            rootTask.Traverse((obj) =>
            {
                // LM_Debug.Instance.Log(obj.name);
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
}