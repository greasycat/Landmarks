using System.Collections.Generic;
using Landmarks.Scripts.Progress;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landmarks.Scripts.Debugging
{
    public class EditorUtility : EditorWindow
    {
        [MenuItem("EditorUtility/Expand Selected Tasks &q")]
        //https://stackoverflow.com/a/66366775
        private static void ExpandTasks()
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var window = GetWindow(type);
            var exprec = type.GetMethod("SetExpandedRecursive");
            if (exprec != null)
                exprec.Invoke(window, new object[] {Selection.activeGameObject.GetInstanceID(), true});
        }

        [MenuItem("EditorUtility/Assign Uid to Unassigned Game Objects")]
        private static void AssignUid()
        {
            var usedIds = new HashSet<uint>();

            // Get all the game objects in the scene
            if (!(FindObjectsOfType(typeof(Transform)) is Transform[] transforms)) return;
            foreach (var transform in transforms)
            {
                var renew = true;
                // Try get the Uid component and if it exists add the ID member of Uid to the usedIds hashset
                if (transform.gameObject.TryGetComponent<Uid>(out var uid))
                {
                    if (uid.ID != 0)
                    {
                        usedIds.Add(uid.ID);
                        renew = false;
                    }
                }
                else
                {
                    uid = transform.gameObject.AddComponent<Uid>();
                }
                // If the game object does not have a Uid component, add one
                if (renew)
                {
                    uid.ID = GetUnusedID(usedIds);
                    usedIds.Add(uid.ID);
                }
                
                UnityEditor.EditorUtility.SetDirty(uid);
            }
        }

        [MenuItem("EditorUtility/Remove All Uid")]
        public static void RemoveAllUid()
        {
            
            if (!(FindObjectsOfType(typeof(Transform)) is Transform[] transforms)) return;
            foreach (var transform in transforms)
            {
                UnityEditor.EditorUtility.SetDirty(transform.gameObject);
                if (transform.gameObject.TryGetComponent<Uid>(out var uid))
                {
                    DestroyImmediate(uid);
                }
            }
            
        }

        private static readonly System.Random Random = new System.Random();

        // Get a random uint
        private static uint RandomUInt() => (uint) Random.Next(1 << 30);

        private static uint GetUnusedID(ICollection<uint> usedIDs)
        {
            uint id = 0;

            do
            {
                id = RandomUInt();
            } while (id == 0 || usedIDs.Contains(id));

            return id;
        }
    }
}