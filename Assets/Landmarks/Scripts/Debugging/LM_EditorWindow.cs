#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landmarks.Scripts.Debugging
{
    public class LM_EditorWindow: EditorWindow
    {
        public void CreateGUI()
        {
            var root = rootVisualElement;
            var label = new Label() { text = "Please implement ShowWindow() \n and CreateGUI() in your unit test" };
            root.Add(label);
        }


        protected static void ShowWindow<T>(string windowTitle) where T : EditorWindow
        {
            var window = GetWindow<T>(windowTitle);
            window.titleContent = new GUIContent(windowTitle);
        }

        protected static void FindSingleton<T>(out T obj) where T : Object
        {

            // Find the ProgressController singleton
            obj = FindObjectOfType<T>();
            if (obj == null)
            {
                Debug.LogError($"{typeof(T)} not found");
            }
            else
            {
                Debug.Log($"{typeof(T)} found");
            }
        }
    }
}
#endif
