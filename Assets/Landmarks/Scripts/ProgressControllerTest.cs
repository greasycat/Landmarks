using System;
using Landmarks.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ProgressControllerTest : LM_UnitTest
{
    [MenuItem("LM_UnitTests/ProgressController")]
    public static void ShowWindow()
    {
        var window = GetWindow<ProgressControllerTest>("ProgressController Unit Test");
        window.titleContent = new GUIContent("Progress Controller Unit Test");
    }

    public new void CreateGUI()
    {
        var root = rootVisualElement;
        root.Add(new Button(TestProgressFileCreation) { text = "Create a save file" });
        root.Add(new Button(TestProgressFileDeletions) { text = "Delete all save file" });
        root.Add(new Button(TestGetLastSaveFile) { text = "Get Last Save" });
        root.Add(new Button(OpenCurrentSaveFile) { text = "Open current save" });
        root.Add(new Button(OpenProgressSavingLocation) { text = "Open save folder in explorer" });
        root.Add(new Button(PrintAllTasks) { text = "Print All TaskLists" });
        root.Add(new Button(PrintAllNonTrialTasks) { text = "Print All Non-Trial Tasks" });
    }

    private static void OpenFolder(string path)
    {
        Application.OpenURL($"file://{path}");
    }

    private static void TestProgressFileCreation()
    {
        var controller = LM_ProgressController.Instance;
        LM_ProgressController.CreateSaveFile(controller.savePath);
    }

    private static void TestProgressFileDeletions()
    {
        var controller = LM_ProgressController.Instance;
        controller.DeleteAllSaveFiles();
    }
    
    private static void TestGetLastSaveFile()
    {
        var controller = LM_ProgressController.Instance;
        var file = LM_ProgressController.GetLastSaveFile(controller.savePath);
        Debug.Log(file);
    }

    private void OpenCurrentSaveFile()
    {
        var controller = LM_ProgressController.Instance;
        var file = LM_ProgressController.GetLastSaveFile(controller.savePath);
        OpenFolder(file);
    }
    
    private static void OpenProgressSavingLocation()
    {
        var controller = LM_ProgressController.Instance;
        var dir = controller.GetSystemConfigFolder();
        OpenFolder(dir);
    }

    private static void PrintAllTasks()
    {
        LM_ProgressController.PrintAllTaskInOrder();
    }
    private static void PrintAllNonTrialTasks()
    {
        LM_ProgressController.PrintAllNonTrialTask();
    }
}