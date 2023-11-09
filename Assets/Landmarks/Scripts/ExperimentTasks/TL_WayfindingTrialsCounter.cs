using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TL_WayfindingTrialsCounter : MonoBehaviour
{
    public NavigationTask navigationTaskReference;
    //public LM_IncrementLists lmIncrementListsReference;
    public LM_PermutedList lmPermutedListReference;
    public ObjectList objectListReference;
    public LM_IncrementLists lmIncrementLists;
    public TaskList taskListReference;
    
    public int finishedTrialCount = 0;
    public int totalTrials;

    private GUIStyle guiStyle = new GUIStyle();

    private void Start()
    {
        //guiStyle.fontSize = 70;
        guiStyle.normal.textColor = Color.red;
        totalTrials = taskListReference.repeat;
    }
    void OnGUI()
    {
        if (objectListReference.objects.Count > 0)
        {
            string trialCountText = "Finished Trial: " + finishedTrialCount + "/" + totalTrials;

            float x = Screen.width * 0.60f; // __ from the left side of the screen
            float y = Screen.height * 0.925f; // __ from the top of the screen
            float width = Screen.width * 0.9f; // 90% of screen width
            float height = Screen.height * 0.1f; // 10% of screen height

            GUI.Label(new Rect(x, y, width, height), trialCountText, guiStyle);
        }
    }

    
    void Update()
    {
         guiStyle.fontSize = (int)(Screen.height * 0.06f);
        if (lmIncrementLists != null)
        {
            int currentTrialCount = 0;
            // Replace "listToTrack" with the specific ObjectList object you want to track
            currentTrialCount += objectListReference.current;
            finishedTrialCount = currentTrialCount;
        }
    }
}
