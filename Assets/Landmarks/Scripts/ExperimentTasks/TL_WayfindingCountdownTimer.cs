using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TL_WayfindingCountdownTimer : MonoBehaviour
{
    public NavigationTask navigationTaskReference;
    private GUIStyle guiStyle = new GUIStyle();
    public string wayfindingRunningCountdownTimer = string.Empty;

    private void Start()
    {
        //guiStyle.fontSize = 70;
        guiStyle.normal.textColor = Color.red;
    }
    void Update()
    {
        guiStyle.fontSize = (int)(Screen.height * 0.06f);
        if (navigationTaskReference.timeRemaining > 0)
        {
            navigationTaskReference.timeRemaining -= Time.deltaTime;
            wayfindingRunningCountdownTimer = navigationTaskReference.timeRemaining.ToString();
        }
    }
    public void OnGUI()
    {
        if (navigationTaskReference.timeRemaining > 0)
        {
            int minutes = Mathf.FloorToInt(navigationTaskReference.timeRemaining / 60);
            int seconds = Mathf.FloorToInt(navigationTaskReference.timeRemaining % 60);
            int milliseconds = Mathf.FloorToInt((navigationTaskReference.timeRemaining * 1000) % 1000);
            string timeText;
            timeText = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
            float x = Screen.width * 0.75f; // 70% from the left side of the screen
            float y = Screen.height * 0.025f; // 10% from the top of the screen
            float width = Screen.width * 0.9f; // 90% of screen width
            float height = Screen.height * 0.1f; // 10% of screen height
            GUI.Label(new Rect(x, y, width, height), "Wayfinding C.D.", guiStyle);
            GUI.Label(new Rect(x, y + height, width, height), timeText, guiStyle);
        }
        //else
        //{
        //    GUI.Label(new Rect(1200, 145, 1000, 200), "Ended Trial", guiStyle);
        //}
    }
    public void DisableTimer()
    {
        this.enabled = false;
    }
}
