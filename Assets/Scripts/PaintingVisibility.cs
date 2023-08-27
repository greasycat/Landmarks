using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Serialization;

public class PaintingVisibility : MonoBehaviour
{

    [SerializeField] private Experiment experiment;
    [SerializeField] private GameObject player;
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

    private float decay(float x)
    {
        if (x <= 1.5)
        {
            return 1;
        }

        return Mathf.Exp(-2.0f * (x-1.5f));
    }

    private void Update()
    {
        if (!_isSetupComplete)
        {
            return;
        }

        // Calculate the distance between the playerEntity and this GameObject (self)
        var distance = Vector3.Distance(_playerControllerTransform.position, transform.position);

        // Calculate a factor between 0 and 1 based on how close the object is to becoming fully transparent
        var fadeFactor = decay(distance);
        
        Debug.Log($"Distance: {distance}");
        Debug.Log($"Factor: {fadeFactor}");

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
