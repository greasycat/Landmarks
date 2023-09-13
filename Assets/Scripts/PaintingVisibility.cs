using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Serialization;

public class PaintingVisibility : MonoBehaviour
{
    private Experiment _experiment;

    private GameObject _player;

    private TaskList _taskList;

    private List<string> _exceptTasks;

    private List<string> _targetOnlyTask;

    private NavigationTask _navigationTask;

    private Transform _playerControllerTransform;

    private Renderer _objectRenderer;
    private Color _originalColor;

    private bool _isSetupComplete = false;

    private void Start()
    {
        _experiment = TargetController.instance.experiment;
        _player = TargetController.instance.player;
        _taskList = TargetController.instance.taskList;
        _exceptTasks = TargetController.instance.exceptTasks;
        _targetOnlyTask = TargetController.instance.targetOnlyTask;
        _navigationTask = TargetController.instance.navigationTask;

        _playerControllerTransform = _player.transform.Find(_experiment.userInterface == UserInterface.KeyboardMouse ? "KeyboardMouseController" : "ViveRoomspaceController");

        _objectRenderer = GetComponent<Renderer>();
        if (_objectRenderer != null && _playerControllerTransform != null)
        {
            _originalColor = _objectRenderer.material.color;
            _isSetupComplete = true;
        }
        else
        {
            Debug.LogWarning("TransparencyController is missing a Renderer or playerEntity.");
        }
    }

    private static float Decay(float x)
    {
        if (x <= 1.5)
        {
            return 1;
        }

        return Mathf.Exp(-2.0f * (x - 1.5f));
    }

    private bool CheckIfLearning()
    {
        if (_taskList == null || _exceptTasks == null || _taskList.currentTask == null)
        {
            return false;
        }

        return _exceptTasks.Any(exp => exp == _taskList.currentTask.name);
    }

    private bool CheckIfTargetMode()
    {
        if (_taskList == null || _taskList.currentTask == null || _targetOnlyTask == null || _navigationTask == null)
        {
            return false;
        }

        return _targetOnlyTask.Any(exp => exp == _taskList.currentTask.name) && transform.name != _navigationTask.currentTarget.name;
    }

    private void Update()
    {
        if (!_isSetupComplete)
        {
            return;
        }

        if (CheckIfLearning())
        {
            return;
        }

        if (CheckIfTargetMode())
        {
            _objectRenderer.material.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0);
            return;
        }

        // Calculate the distance between the playerEntity and this GameObject (self)
        var distance = Vector3.Distance(_playerControllerTransform.position, transform.position);

        // Calculate a factor between 0 and 1 based on how close the object is to becoming fully transparent
        var fadeFactor = Decay(distance);

        // Debug.Log($"Distance: {distance}");
        // Debug.Log($"Factor: {fadeFactor}");

        // Adjust transparency accordingly
        var newColor = new Color(
            _originalColor.r,
            _originalColor.g,
            _originalColor.b,
            fadeFactor * _originalColor.a
        );

        _objectRenderer.material.color = newColor;
    }
}