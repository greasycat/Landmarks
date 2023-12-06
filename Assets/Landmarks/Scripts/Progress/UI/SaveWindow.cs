﻿#if UNITY_EDITOR
using Landmarks.Scripts.Debugging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Landmarks.Scripts.Progress
{
    public class SaveWindow : LM_EditorWindow
    {
        [MenuItem("LM_ProgressController/Manage Saves")]
        public static void ShowWindow()
        {
            LM_EditorWindow.ShowWindow<SaveWindow>("Manage Saves");
        }

        public new void CreateGUI()
        {
            var root = rootVisualElement;
            root.Add(new Button(TestProgressFileCreation) { text = "Create a save file" });
            root.Add(new Button(TestProgressFileDeletions) { text = "Delete all save file" });
            root.Add(new Button(TestGetLastSaveFile) { text = "Get Last Save" });
            root.Add(new Button(OpenCurrentSaveFile) { text = "Open current save" });
            root.Add(new Button(OpenProgressSavingLocation) { text = "Open save folder in explorer" });
        }

        private static void OpenFolder(string path)
        {
            Application.OpenURL($"file://{path}");
        }

        private static void TestProgressFileCreation()
        {
            var controller = LM_Progress.Instance;
            LM_Progress.CreateSaveFile(controller.savingFolderPath);
        }

        private static void TestProgressFileDeletions()
        {
            var controller = LM_Progress.Instance;
            controller.DeleteAllSaveFiles();
        }

        private static void TestGetLastSaveFile()
        {
            var controller = LM_Progress.Instance;
            var file = LM_Progress.GetLastSaveFile(controller.savingFolderPath);
            Debug.Log(file);
        }

        private static void OpenCurrentSaveFile()
        {
            var controller = LM_Progress.Instance;
            var file = LM_Progress.GetLastSaveFile(controller.savingFolderPath);
            OpenFolder(file);
        }

        private static void OpenProgressSavingLocation()
        {
            var controller = LM_Progress.Instance;
            var dir = LM_Progress.GetSystemConfigFolder();
            OpenFolder(dir);
        }

    }
}

#endif
