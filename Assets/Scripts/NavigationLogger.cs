using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NavigationLogger : LM_TaskLog
{
    [SerializeField] private float sampleRate;
    private Experiment _experiment;
    private Coroutine _coroutine;
    private string _currentPath;
    private int _count;

    private Transform _body;
    private Transform _head;

    private LM_PlayerController _controller;
    private float _timer;
    

    // Start is called before the first frame update
    private void Start()
    {
        _experiment = FindObjectOfType<Experiment>();
        _controller = _experiment.player.GetComponent<LM_PlayerController>();
        _body = _controller.collisionObject.transform;
        _head = _controller.cam.transform;
        UpdatePath(-1, "None");
    }

    private void UpdatePath(int count, string target)
    {
        if (_experiment != null)
        {
            _currentPath = $"{_experiment.dataPath}/sub-{_experiment.config.subject}_task-{name}_trial-{count}_target-{target}_controller-{_controller.name}.csv";
            return;
        }

        Debug.LogError($"Cannot Find Experiment Object (target: {target})!");
    }

    public void StartLogging(int count, string target)
    {
        UpdatePath(count, target);
        if (_body == null || _head == null)
        {
            return;
        }
        _coroutine = StartCoroutine(Logging());
    }

    public void EndLogging()
    {
        StopCoroutine(_coroutine);
        _timer = 0;
    }

    public override void LogTrial()
    {
        output = new StreamWriter(_currentPath, append: true);

        var header = string.Empty;
        var data = string.Empty;

        // convert the list of label values into a formatted string for printing to the log
        foreach (var item in trialData)
        {
            header += item.Key + ","; // append and add a tab
            data += item.Value + ","; // append and add a tab
        }

        // Log the header (if empty file) and data
        if (output.BaseStream.Length == 0) output.WriteLine(header);
        output.WriteLine(data);

        // Clean up from this trial
        trialData.Clear();
        hopper.Clear();
        output.Close();
    }

    private IEnumerator Logging()
    {
        while (true)
        {
            if (_body == null || _head == null)
            {
                yield return null;
            }
            
            var bodyPosition = _body.position;
            var headPosition = _head.position;
            AddData("Time", $"{_timer}");
            AddData("Body Position (x)", $"{bodyPosition.x}");
            AddData("Body Position (y)", $"{bodyPosition.y}");
            AddData("Body Position (z)", $"{bodyPosition.z}");
            AddData("Head Position (x)", $"{headPosition.x}");
            AddData("Head Position (y)", $"{headPosition.y}");
            AddData("Head Position (z)", $"{headPosition.z}");
            AddData("Region (Color)", $"{MazeController.instance.region}");
            LogTrial();
            _timer += 1/sampleRate;
            yield return new WaitForSeconds(1f/sampleRate);
        }
    }

    public void Pause()
    {
        StopCoroutine(_coroutine);
    }

}