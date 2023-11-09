using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TL_ExplorationCountdownTimer : MonoBehaviour
{
    public ExplorationTask ExplorationTaskReference;
    private GUIStyle guiStyle = new GUIStyle();
    public string runningCountdownTimer = string.Empty;

    private void Start()
    {
        //guiStyle.fontSize = 70;
        guiStyle.normal.textColor = Color.yellow;
    }
    void Update()
    {
        guiStyle.fontSize = (int)(Screen.height * 0.06f);
        if (ExplorationTaskReference.timeRemaining > 0)
        {
            ExplorationTaskReference.timeRemaining -= Time.deltaTime;
            runningCountdownTimer = ExplorationTaskReference.timeRemaining.ToString();
        }
        else
        {
            //ExplorationTaskReference.timeRemaining += Time.deltaTime;
        }
        //if (tL_ExplorationTaskReference.timeRemaining < 0.01f)
        //{
        //    tL_ExplorationTaskReference.timeRemaining += Time.deltaTime;
        //}
    }
    public void OnGUI()
    {
        int minutes = Mathf.FloorToInt(ExplorationTaskReference.timeRemaining / 60);
        int seconds = Mathf.FloorToInt(ExplorationTaskReference.timeRemaining % 60);
        int milliseconds = Mathf.FloorToInt((ExplorationTaskReference.timeRemaining * 1000) % 1000);

        string timeText;
        if (ExplorationTaskReference.timeRemaining == Mathf.Infinity)
        {
            timeText = "Infinite";
        }
        else
        {
            timeText = string.Format("{0:00}:{1:00}{2:000}", minutes, seconds, milliseconds);
        }

        //GUI.Label(new Rect(100, 50, 1000, 200), "Exploration C.D.", GUIStyle);
        //GUI.Label(new Rect(100, 145, 1000, 200), timeText, GUIStyle);
        float x = Screen.width * 0.025f; // 10% from the left side of the screen
        float y = Screen.height * 0.025f; // 10% from the top of the screen
        float width = Screen.width * 0.9f; // 90% of screen width
        float height = Screen.height * 0.1f; // 10% of screen height

        GUI.Label(new Rect(x, y, width, height), "Exploration C.D.", guiStyle);
        GUI.Label(new Rect(x, y + height, width, height), timeText, guiStyle);
        // //if (ExplorationTaskReference.timeRemaining > 0)
        //     Debug.Log("Running countdown timer OnGui(): called");
        // {
        //     Debug.Log("Running countdown timer OnGui(): timeRemaining > 0");
        //     int minutes = Mathf.FloorToInt(ExplorationTaskReference.timeRemaining / 60);
        //     int seconds = Mathf.FloorToInt(ExplorationTaskReference.timeRemaining % 60);
        //     int milliseconds = Mathf.FloorToInt((ExplorationTaskReference.timeRemaining * 1000) % 1000);
        //     string timeText = string.Format("{0:00}:{1:00}{2:000}", minutes, seconds, milliseconds);
        //     GUI.Label(new Rect(100, 50, 1000, 200), "Exploration C.D.", guiStyle);
        //     GUI.Label(new Rect(100, 145, 1000, 200), timeText, guiStyle);
        // }
    }
    public void DisableTimer()
    {
        this.enabled = false;
    }
}
