using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TL_ExplorationCountdownTimer : MonoBehaviour
{
    public ExplorationTask ExplorationTaskReference;
    private GUIStyle guiStyle = new GUIStyle();

    private void Start()
    {
        guiStyle.fontSize = 70;
        guiStyle.normal.textColor = Color.yellow;
    }
    void Update()
    {
        if (ExplorationTaskReference.timeRemaining > 0)
        {
            ExplorationTaskReference.timeRemaining -= Time.deltaTime;

        }
        else
        {
            ExplorationTaskReference.timeRemaining += Time.deltaTime;
        }
        //if (tL_ExplorationTaskReference.timeRemaining < 0.01f)
        //{
        //    tL_ExplorationTaskReference.timeRemaining += Time.deltaTime;
        //}
    }
    public void OnGUI()
    {
        if (ExplorationTaskReference.timeRemaining > 0)
            Debug.Log("Running countdown timer OnGui(): called");
        {
            Debug.Log("Running countdown timer OnGui(): timeRemaining > 0");
            int minutes = Mathf.FloorToInt(ExplorationTaskReference.timeRemaining / 60);
            int seconds = Mathf.FloorToInt(ExplorationTaskReference.timeRemaining % 60);
            int milliseconds = Mathf.FloorToInt((ExplorationTaskReference.timeRemaining * 1000) % 1000);
            string timeText = string.Format("{0:00}:{1:00}{2:000}", minutes, seconds, milliseconds);
            GUI.Label(new Rect(100, 50, 1000, 200), "Exploration C.D.", guiStyle);
            GUI.Label(new Rect(100, 145, 1000, 200), timeText, guiStyle);
        }
    }
    public void DisableTimer()
    {
        this.enabled = false;
    }
}
