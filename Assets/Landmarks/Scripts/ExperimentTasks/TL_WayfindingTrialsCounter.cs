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
        guiStyle.fontSize = 70;
        guiStyle.normal.textColor = Color.red;
        totalTrials = taskListReference.repeat;
    }
    void OnGUI()
    {
        if (objectListReference.objects.Count > 0)
        {
            string trialCountText = "Finished Trial: " + finishedTrialCount + "/" + totalTrials; Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(trialCountText));
            GUI.Label(new Rect(1300, 1000, 1000, 200), trialCountText, guiStyle);
        }
    }

    
    void Update()
    {
        if (lmIncrementLists != null)
        {
            int currentTrialCount = 0;
            // Replace "listToTrack" with the specific ObjectList object you want to track
            currentTrialCount += objectListReference.current;
            finishedTrialCount = currentTrialCount;
        }
    }
}
