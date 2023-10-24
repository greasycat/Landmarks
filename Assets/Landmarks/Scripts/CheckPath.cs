using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landmarks.Scripts
{
    public class CheckPath : EditorWindow
    {
        private TextField _input;

        [MenuItem("LM_UnitTests/CheckPath")]
        public static void ShowWindow()
        {
            var window = GetWindow<CheckPath>("Check Path Test");
            window.titleContent = new GUIContent("Check if path is accessible");
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            _input = new TextField("Path") { value = "/" };
            root.Add(_input);

            var checkButton = new Button(Check) { text = "Check" };
            root.Add(checkButton);

            var button = new Button(CloseWindow) { text = "Close" };
            root.Add(button);
        }


        private void Check()
        {
            Debug.Log("Checking accessibility of " + _input.value);
            IsDirectoryAccessible(_input.value);
        }

        private static void IsDirectoryAccessible(string path)
        {
            try
            {
                // Attempt to get a list of subdirectories
                var subdir = Directory.GetDirectories(path);
                Debug.Log("Directory " + path + " is accessible");
                foreach (var dir in subdir)
                {
                    Debug.Log(dir);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Lack of permissions
                Debug.LogError("Not enough permissions to access " + path);
            }
            catch (DirectoryNotFoundException)
            {
                // Directory does not exist
                Debug.LogError("Directory " + path + " does not exist");
            }
        }

        private void CloseWindow()
        {
            Close();
        }
    }
}