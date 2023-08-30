using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Serialization;

public class PaintingVisibility : MonoBehaviour
{

    [SerializeField] private Experiment experiment;
    [SerializeField] private GameObject player;
    [SerializeField] private TaskList taskList;
    [SerializeField] private List<string> exceptTasks;
    private Transform _playerControllerTransform;

    private Renderer _objectRenderer;
    private Color _originalColor;

    private bool _isSetupComplete = false;

    private void Start()
    {
        if (experiment.userInterface == UserInterface.KeyboardMouse)
        {
            _playerControllerTransform = player.transform.Find("KeyboardMouseController");
        }
        else
        {
            _playerControllerTransform = player.transform.Find("ViveRoomspaceController");
        }
        
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

        return Mathf.Exp(-2.0f * (x-1.5f));
    }

    private bool CheckIfLearning()
    {
        if (taskList == null || exceptTasks == null || taskList.currentTask == null)
        {
            return false;
        }

        return exceptTasks.Any(exp => exp == taskList.currentTask.name);
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
