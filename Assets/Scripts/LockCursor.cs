using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockCursor : MonoBehaviour
{
    private bool _cursorLocked = true;

    // Update is called once per frame
    private void Update()
    {
        // Lock or unlock the cursor based on the player's input (e.g., pressing the "Escape" key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _cursorLocked = !_cursorLocked;
        }

        // Apply the cursor lock state
        UpdateCursorLockState();
    }

    // Updates the cursor lock state based on the value of cursorLocked
    private void UpdateCursorLockState()
    {
        if (_cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
