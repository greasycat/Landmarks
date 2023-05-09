using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BulkRename : MonoBehaviour
{
    public string baseName = "Hallway ";
    public int startIndex = 1;

    [ContextMenu("Rename Children")]
    void RenameChildren()
    {
        int index = startIndex;
        foreach (Transform child in transform)
        {
            Undo.RecordObject(child.gameObject, "Renamed object");
            child.gameObject.name = baseName + index.ToString();
            index++;
        }
    }
}