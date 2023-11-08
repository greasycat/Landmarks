using System;
using System.Linq;
using System.Text;

namespace Landmarks.Scripts.Progress
{
using System.Collections.Generic;
using UnityEngine;

public static class SerializationHelper
{
    public static Dictionary<string, GameObject> ConvertToDictionary(IEnumerable<GameObject> gameObjects)
    {
        return gameObjects.ToDictionary(gameObject => gameObject.name);
    }

    public static string SerializeGameObjectList(List<List<GameObject>> listOfLists)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('[');
        for (int i = 0; i < listOfLists.Count; i++)
        {
            sb.Append('[');
            for (int j = 0; j < listOfLists[i].Count; j++)
            {
                sb.AppendFormat("\"{0}\"", listOfLists[i][j].name);
                if (j < listOfLists[i].Count - 1)
                    sb.Append(',');
            }
            sb.Append(']');
            if (i < listOfLists.Count - 1)
                sb.Append(',');
        }
        sb.Append(']');

        return sb.ToString();
    }
    
    
    public static List<List<GameObject>> DeserializeGameObjectList(string serializedData, IDictionary<string, GameObject> gameObjectLookup)
    {
        var listOfLists = new List<List<GameObject>>();
        var outerLists = serializedData.Trim('[', ']').Split(new string[] { "],[" }, StringSplitOptions.None);

        foreach (var outerList in outerLists)
        {
            var innerList = new List<GameObject>();
            var gameObjectNames = outerList.Trim('[', ']').Split(',');

            foreach (var name in gameObjectNames)
            {
                var trimmedName = name.Trim('"');
                if (gameObjectLookup.TryGetValue(trimmedName, out var gameObject))
                {
                    innerList.Add(gameObject);
                }
                else
                {
                    // Handle the case where the GameObject is not found
                    Debug.LogWarning($"GameObject with name {trimmedName} not found.");
                }
            }

            listOfLists.Add(innerList);
        }

        return listOfLists;
    }

}

}