using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landmarks.Scripts.Debugging
{
    public class LM_UnitTest: EditorWindow
    {
        public void CreateGUI()
        {
            var root = rootVisualElement;
            var label = new Label() { text = "Please implement ShowWindow() \n and CreateGUI() in your unit test" };
            root.Add(label);
            var button = new Button(CloseWindow) {text = "Close"};
            root.Add(button);
        }
        
        protected void CloseWindow()
        {
            Close();
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