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
    }
    
    
}