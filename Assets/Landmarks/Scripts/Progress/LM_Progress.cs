using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Landmarks.Scripts.Debugging;
using UnityEngine;
using UnityEngine.Serialization;

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
        private XmlNode _currentSaveNode;
        private Queue<KeyValuePair<string, string>> _attributeStack;

        public Queue<KeyValuePair<string, string>> AttributeStack => _attributeStack;


        // Debug variables
        [SerializeField] private bool deleteCurrentSaveFileOnEditorQuit = false;
        private int _depth = 0;
        private int _line = 1;


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
            _attributeStack = new Queue<KeyValuePair<string, string>>();
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
            var attributes = new Dictionary<string, string>
            {
                { "name", task.name },
                { "line", _line.ToString() }
            };
            
            // RecordAttributesByType(task, attributes);

            WriteToCurrentSaveFileSync(XmlNode.BuildOpeningString("Task", attributes,  _depth));
            _line++;
            
            LM_Debug.Instance.Log($"Recording start: {task.name}", 1);
            _depth++;

            // LM_Debug.Instance.Log("Start-Before:" + _currentSaveNode.Name, 1);
            TrySkip(task);
            // LM_Debug.Instance.Log("Start-After:" + _currentSaveNode.Name, 1);

            if (!task.skip)
            {
                LM_Debug.Instance.Log($"Trigger MoveToNextNode for {task.name}", 1);
                XmlNode.MoveToNextNode(ref _currentSaveNode);
            }
            
        }


        /// <summary>
        /// Record the end of a task
        /// This method will be inside the ExperimentTask.endTask() method
        /// </summary>
        /// <param name="task">The task you want to record</param>
        public void RecordTaskEnd(ExperimentTask task)
        {
            LM_Debug.Instance.Log($"Recording stop: {task.name}", 1);
            _depth--;
            WriteToCurrentSaveFileSync(XmlNode.BuildClosingString("Task", _depth));
            _line++;
        }

        private void RecordAttributesByType(ExperimentTask task, IDictionary<string, string> attributes)
        {
            while (_attributeStack.Count > 0)
            {
                attributes.Add(_attributeStack.Dequeue());
                Debug.Log("Dequeuing");
            }
            
        }
        
        
        private void TrySkip(ExperimentTask task)
        {
            if (task.skip && lastSaveStack.Count != 0)
            {
                LM_Debug.Instance.Log($"Manual Skipping task {task.name} at index {_currentSaveNode.GetAttribute("line")}", 1);
                XmlNode.SkipToNextNode(ref _currentSaveNode);
                return;
            }
            
            if (!resumeLastSave || lastSaveStack.Count == 0 || !task.skipIfResume)
            {
                LM_Debug.Instance.Log($"Skip not enabled for {task.name}", 1);
                return;
            }

            if (task.name != _currentSaveNode.Name)
            {
                LM_Debug.Instance.Log($"Task name not match: {task.name} {_currentSaveNode.Name}", 1);
                return;
            }


            if (!_currentSaveNode.HasAttributeEqualTo("completed", "true"))
            {
                if (task is TaskList taskList && taskList.taskListType == Role.trial)
                {
                    var trialSubTaskNames = task.gameObject.transform.Cast<Transform>().Select(tr => tr.name);
                    var numSubTasks = trialSubTaskNames.Count();
                    
                    // Get the number of completed subtasks
                    var child = _currentSaveNode.GetAllChildren();
                    var completedSubTasks = child.Where(node => node.HasAttributeEqualTo("completed", "true"));
                    var numCompletedSubTasks = completedSubTasks.Count();
                    
                    // floor division completed subtasks number by number of subtask in each trial
                    // to get the number of completed trials
                    var numCompletedTrials = numCompletedSubTasks / numSubTasks;
                    taskList.repeatCount += numCompletedTrials;
                    
                    resumeLastSave = false; // Stop resuming
                    return;
                }
                LM_Debug.Instance.Log("Task not completed", 1);
                return;
            }
            
            
            LM_Debug.Instance.Log($"Skipping task {task.name} at index {_currentSaveNode.GetAttribute("line")}", 1);
            task.skip = true;
            XmlNode.SkipToNextNode(ref _currentSaveNode);

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

            _currentSaveNode = XmlNode.ParseFromLines(lastSaveStack);
            XmlNode.MoveToNextNode(ref _currentSaveNode);

            LM_Debug.Instance.Log(_currentSaveNode.HierarchyToString(0), 2);
        }


        private void PrepareNewSave()
        {
            currentSaveFile = CreateSaveFile(savingFolderPath);
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
                if (!file.EndsWith(".xml")) continue;

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
            var saveFile = Path.Combine(folderPath, "save_" + timestamp + ".xml");
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