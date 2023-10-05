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
        FindSingleton(out LM_ProgressController controller);
        controller.Instance.CreateSaveFile();
    }

    private static void TestProgressFileDeletions()
    {
        FindSingleton(out LM_ProgressController controller);
        controller.Instance.DeleteAllSaveFiles();
    }
    
    private static void TestGetLastSaveFile()
    {
        FindSingleton(out LM_ProgressController controller);
        var file = controller.Instance.GetLastSaveFile();
        Debug.Log(file);
    }

    private void OpenCurrentSaveFile()
    {
        FindSingleton(out LM_ProgressController controller);
        var file = controller.Instance.GetLastSaveFile();
        OpenFolder(file);
    }
    
    private static void OpenProgressSavingLocation()
    {
        FindSingleton(out LM_ProgressController controller);
        var dir = controller.Instance.GetSystemConfigFolder();
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