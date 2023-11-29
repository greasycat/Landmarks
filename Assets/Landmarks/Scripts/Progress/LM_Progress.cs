using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Landmarks.Scripts.Debugging;
using Landmarks.Scripts.ExperimentTasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Landmarks.Scripts.Progress
{
    public class LM_Progress : MonoBehaviour
    {
        public static LM_Progress Instance { get; private set; }

        // Config variables
        public static readonly string ApplicationName = "Landmarks";
        public static readonly string SaveFolderName = "saves";

        // Singleton variables
        private Experiment _experiment;

        [SerializeField] public bool resumeLastSave = true;

        // Saves-related variables
        [NotEditable] public string currentSaveFile;
        [NotEditable] public string lastSaveFile;
        [NotEditable] public List<string> lastSaveStack;
        [NotEditable] public string savingFolderPath;

        // Task-related variables
        private XmlNode _currentSaveNode;
        private Dictionary<string, XmlNode> _taskNodeLookup;
        private Queue<KeyValuePair<string, string>> _attributeQueue;


        // Debug variables
        [SerializeField] private bool deleteCurrentSaveFileOnEditorQuit = false;
        private int _depth = 0;
        private int _line = 1;

        private Coroutine ReportingCoroutine { get; set; }

        private ResumeOptions _resumeOption;
        
        public enum ResumeOptions
        {
            LastTrialFromStart = 0, 
            LastTrialFromProgress = 1,
            SkipTrial = 2,
        }

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
            _attributeQueue = new Queue<KeyValuePair<string, string>>();
            _experiment = FindObjectOfType<Experiment>();
            _taskNodeLookup = new Dictionary<string, XmlNode>();
        }

        public void InitializeSave(string savePath = "")
        {
            LoadLastSave(savePath);
            PrepareNewSave();
        }

        public void SetSavingFolderPath(string path)
        {
            savingFolderPath = path;
        }

        public void EnableResuming()
        {
            resumeLastSave = true;
        }

        public void DisableResuming()
        {
            resumeLastSave = false;
        }
        
        public void SetResumeOption(ResumeOptions option)
        {
            _resumeOption = option;
        }

        //**************************************************************
        // Attribute-related methods
        //**************************************************************

        public void AddAttribute(string key, string value)
        {
            _attributeQueue.Enqueue(new KeyValuePair<string, string>(key, value));
        }


        public string GetCurrentNodeAttribute(string key)
        {
            if (_currentSaveNode != null) return _currentSaveNode.GetAttribute(key);
            LM_Debug.Instance.Log("Current save node is null", 1);
            return null;
        }

        public void UpdateAttributeAfterWritten(XmlNode node, string key, string value)
        {
            XmlNode.UpdateAttribute(currentSaveFile, node, key, value);
        }

        public void UpdateAttributesAfterWritten(XmlNode node, Dictionary<string, string> dictionary)
        {
            XmlNode.UpdateAttributes(currentSaveFile, node, dictionary);
        }

        // Task-specific methods
        public void ResumeLastNavigationTimer(INavigationTask task)
        {
            if (_resumeOption != ResumeOptions.LastTrialFromProgress)
                return;
            
            var nodeSearch = _taskNodeLookup.Where(p => p.Value.HasAttribute("timeRemaining")).ToList();
            if (nodeSearch.Count == 1)
            {
                var node = nodeSearch[0].Value;
                if (float.TryParse(node.GetAttribute("timeRemaining"), out var timeRemaining))
                {
                    task.SetTimeAllotted(timeRemaining);
                }

                // remove attributes
                node.RemoveAttribute("timeRemaining");
            }
            else
            {
                LM_Debug.Instance.Log("Found " + nodeSearch.Count + " nodes with timeRemaining attribute", 3);
            }
        }
        
        public bool CheckIfResumeNavigation()
        {
            if (_resumeOption != ResumeOptions.LastTrialFromProgress)
                return false;
            var nodeSearch = _taskNodeLookup.Where(p => p.Value.HasAttribute("timeRemaining")).ToList();
            return nodeSearch.Count != 0;
        }

        public void ResumeLastPlayerPositionToNavStart(Transform startTransform)
        {
            if (_resumeOption != ResumeOptions.LastTrialFromProgress)
                return;
            
            var nodeSearch = _taskNodeLookup.Where(p => p.Value.HasAttribute("x")).ToList();
            if (nodeSearch.Count == 1)
            {
                var node = nodeSearch[0].Value;
                if (float.TryParse(node.GetAttribute("x"), out var x) &&
                    float.TryParse(node.GetAttribute("z"), out var z))
                {
                    var y = startTransform.position.y; // do not change the height
                    startTransform.position = new Vector3(x, y, z);
                }

                if (float.TryParse(node.GetAttribute("rx"), out var rx) &&
                    float.TryParse(node.GetAttribute("ry"), out var ry) &&
                    float.TryParse(node.GetAttribute("rz"), out var rz) &&
                    float.TryParse(node.GetAttribute("rw"), out var rw))
                {
                    var rotation = new Quaternion(rx, ry, rz, rw);
                    // rotate on y axis by -90 degrees to match the local rotation of the footprint
                    rotation *= Quaternion.Euler(0, -90, 0);
                    startTransform.rotation = rotation;
                }
                
                // remove attributes
                node.RemoveAttribute("x");
            }
            else
            {
                LM_Debug.Instance.Log("Found " + nodeSearch.Count + " nodes with position attribute", 3);
            }
        }

        public void StartNavigationReportingCoroutine(INavigationTask task)
        {
            ReportingCoroutine = StartCoroutine(NavigationUpdatingCoroutine(task));
        }

        public void StopNavigationReportingCoroutine(INavigationTask task)
        {
            if (ReportingCoroutine == null) return;

            StopCoroutine(ReportingCoroutine);
            UpdateAttributeAfterWritten(task.GetParentTask().taskNodeToBeWritten, "timeRemaining", "");
        }

        private IEnumerator NavigationUpdatingCoroutine(INavigationTask task)
        {
            var parentNode = task.GetParentTask().taskNodeToBeWritten;
            while (true)
            {
                var avatarPosition = task.GetPlayerPosition();
                var avatarRotation = task.GetPlayerRotation();

                var attributes = new Dictionary<string, string>
                {
                    {"timeRemaining", task.GetTimeRemaining().ToString(CultureInfo.InvariantCulture)},
                    {"x", avatarPosition.x.ToString(CultureInfo.InvariantCulture)},
                    {"y", avatarPosition.y.ToString(CultureInfo.InvariantCulture)},
                    {"z", avatarPosition.z.ToString(CultureInfo.InvariantCulture)},
                    {"rx", avatarRotation.x.ToString(CultureInfo.InvariantCulture)},
                    {"ry", avatarRotation.y.ToString(CultureInfo.InvariantCulture)},
                    {"rz", avatarRotation.z.ToString(CultureInfo.InvariantCulture)},
                    {"rw", avatarRotation.w.ToString(CultureInfo.InvariantCulture)},
                };

                UpdateAttributesAfterWritten(parentNode, attributes);
                yield return new WaitForSecondsRealtime(1f);
            }
        }


        //**************************************************************
        // Task-related methods
        //**************************************************************

        private static uint GetUid(Component task)
        {
            // Get UID component from transform
            return task.TryGetComponent(out Uid uid) ? uid.ID : 0;
        }

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
            LM_Debug.Instance.Log($"Recording start: {task.name}", 2);
            var attributes = new Dictionary<string, string>
            {
                {"name", task.name},
                {"line", _line.ToString()},
                {"depth", _depth.ToString()},
                {"uid", GetUid(task).ToString()}
            };

            var attributePairs = attributes.Select(pair => $"{pair.Key}=\"{pair.Value}\"").ToList();
            LM_Debug.Instance.Log($"Attributes: {string.Join(", ", attributePairs)}", 2);

            if (_currentSaveNode != null && _currentSaveNode.HasAttributeEqualTo("uid", attributes["uid"]))
            {
                task.taskNodeLoaded = _currentSaveNode;
                LM_Debug.Instance.Log($"Found matching uid for {attributes["name"]}: {attributes["uid"]}", 2);
            }

            TrySkip(task); // This may add some attributes to the attributes dictionary

            MoveAllAttributesFromQueue(attributes); // Push all the attributes from the queue to the dictionary

            var newNode = new XmlNode("Task", attributes);
            task.taskNodeToBeWritten = newNode;

            var text = XmlNode.BuildOpeningString(newNode);
            WriteToCurrentSaveFileSync(text);

            _line++;
            _depth++;


            if (!task.skip && resumeLastSave)
            {
                LM_Debug.Instance.Log($"Trigger MoveToNextNode for {task.name}", 1);
                XmlNode.MoveToNextNode(ref _currentSaveNode);
            }
        }

        public bool CheckIfResumeCurrentNode(ExperimentTask task) =>
            resumeLastSave && _currentSaveNode.HasAttributeEqualTo("uid", GetUid(task).ToString());


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

        private void MoveAllAttributesFromQueue(IDictionary<string, string> attributes)
        {
            while (_attributeQueue.Count > 0)
            {
                attributes.Add(_attributeQueue.Dequeue());
                Debug.Log("Dequeuing");
            }
        }


        private void TrySkip(ExperimentTask task)
        {
            if (task.skip && lastSaveStack.Count != 0 && resumeLastSave)
            {
                LM_Debug.Instance.Log(
                    $"Manual Skipping task {task.name} at index {_currentSaveNode.GetAttribute("line")}", 1);
                XmlNode.SkipToNextNode(ref _currentSaveNode);
                return;
            }

            if (!resumeLastSave || lastSaveStack.Count == 0 || !task.skipIfResume)
            {
                LM_Debug.Instance.Log($"Skip not enabled for {task.name}", 1);
                return;
            }

            if (!_currentSaveNode.HasAttributeEqualTo("uid", GetUid(task).ToString()))
            {
                LM_Debug.Instance.Log($"UID not match: {task.name} {_currentSaveNode.Name}", 1);
                LM_Debug.Instance.Log($"UID: {GetUid(task)} {_currentSaveNode.GetAttribute("uid")}", 1);
                return;
            }

            if (!_currentSaveNode.HasAttributeEqualTo("completed", "true"))
            {
                if (task is TaskList taskList && taskList.taskListType == Role.trial)
                {
                    var trialSubTaskNames = task.gameObject.transform.Cast<Transform>().Select(tr => tr.name);
                    var numSubTasks = trialSubTaskNames.Count();

                    LM_Debug.Instance.Log($"Number of subtasks in trial: {numSubTasks}", 10);

                    // Get the number of completed subtasks
                    var child = _currentSaveNode.GetAllChildren();
                    var completedSubTasks = child.Where(node => node.HasAttributeEqualTo("completed", "true"));
                    var numCompletedSubTasks = completedSubTasks.Count();

                    LM_Debug.Instance.Log($"Number of completed subtasks: {numCompletedSubTasks}", 10);

                    // floor division completed subtasks number by number of subtask in each trial
                    // to get the number of completed trials

                    // For Taylor's experiment, there are 8 subtasks in each trial
                    // If the last disorientation task is incomplete, the number of completed subtasks will still be 8
                    if (numCompletedSubTasks % 8 == 7)
                    {
                        numCompletedSubTasks += 1;
                    }

                    var numCompletedTrials = numCompletedSubTasks / numSubTasks;

                    if (int.TryParse(_currentSaveNode.GetAttribute("numCompletedTrials"), out var lastResumedNumber))
                    {
                        numCompletedTrials += lastResumedNumber;
                    }
                    
                    if (ResumeOptions.SkipTrial == _resumeOption)
                    {
                        numCompletedTrials += 1;
                    }

                    LM_Debug.Instance.Log($"Number of completed trials: {numCompletedTrials}", 10);

                    // The repeatCount is 1-indexed but its initial value 0
                    // This value is used to check if subject finish the repeating set
                    taskList.repeatCount += numCompletedTrials;

                    // The overideRepeat is 0-indexed and its initial value is 0
                    // This determines which of the object is going to be displayed in the next trial
                    if (taskList.overideRepeat != null)
                        taskList.overideRepeat.current = taskList.repeatCount - 1;

                    foreach (Transform childTransform in taskList.transform)
                    {
                        if (childTransform.TryGetComponent(typeof(LM_IncrementLists), out var component))
                        {
                            var incrementLists = (LM_IncrementLists) component;

                            incrementLists.Increment(numCompletedTrials);

                            LM_Debug.Instance.Log($"Increment trial number by triggering {childTransform.name}", 2);
                        }
                        else
                        {
                            LM_Debug.Instance.Log($"Cannot find LM_IncrementLists component in {childTransform.name}",
                                1);
                        }
                    }

                    AddAttribute("numCompletedTrials", numCompletedTrials.ToString());


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
        private void LoadLastSave(string savePath = "")
        {
            if (!resumeLastSave)
            {
                lastSaveStack = new List<string>();
                return;
            }

            lastSaveFile = savePath != "" ? savePath : GetLastSaveFile(savingFolderPath);

            if (lastSaveFile == "")
            {
                lastSaveStack = new List<string>();
                return;
            }

            lastSaveStack = File.ReadAllText(lastSaveFile).Split('\n').ToList();
            RemoveEmptyLines(lastSaveStack);

            _currentSaveNode = XmlNode.ParseFromLines(lastSaveStack, _taskNodeLookup, "uid");
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
            if (File.Exists(path))
            {
                File.Delete(path);
                LM_Debug.Instance.Log("Save file deleted: " + path);
            }
            else
            {
                LM_Debug.Instance.LogWarning("Save file not found: " + path);
            }
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

        public static IEnumerable<string> GetSaveFiles(string filepath)
        {
            var files = Directory.GetFiles(filepath);
            return files.Where(file => file.EndsWith(".xml"));
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

        public static string GetSystemConfigFolder()
        {
            var configFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // create a path at the config folder
            var path = Path.Combine(configFolder, ApplicationName);

            // create the folder if it doesn't exist
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            LM_Debug.Instance.Log("Config folder: " + path);
            return path;
        }

        public static string GetSaveFolderWithId(string id)
        {
            var saveFolder = Path.Combine(GetSystemConfigFolder(), SaveFolderName, id);
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            return saveFolder;
        }

        public static string GetSaveFolder()
        {
            return GetSaveFolderWithId("");
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