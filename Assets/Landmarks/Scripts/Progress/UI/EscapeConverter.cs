#if UNITY_EDITOR
using System.Security;
using Landmarks.Scripts.Debugging;
using UnityEditor;
using UnityEngine.UIElements;

namespace Landmarks.Scripts.Progress
{
    public class EscapeConverter : LM_EditorWindow
    {

        [MenuItem("LM_ProgressController/Escape Character Converter")]
        public static void ShowWindow()
        {
            LM_EditorWindow.ShowWindow<EscapeConverter>("Escape Converter");
        }

        public new void CreateGUI()
        {
            // Create a large text field where the user can enter a string
            var root = rootVisualElement;
            var textField = new TextField("Enter a string to convert")
            {
                multiline = true,
                style =
                {
                    height = 200,
                    width = 400
                }
            };


            root.Add(textField);

            // Create a button to escape the string
            var escapeButton = new Button(EscapeString)
            {
                text = "Escape"
            };
            root.Add(escapeButton);

            // Create a button to unescape the string
            var unescapeButton = new Button(UnescapeString)
            {
                text = "Unescape"
            };
            root.Add(unescapeButton);

            // Create a button to clear the text field
            var clearButton = new Button(ClearTextField)
            {
                text = "Clear"
            };
            root.Add(clearButton);
        }

        private void EscapeString()
        {
            var textField = rootVisualElement.Q<TextField>();

            // use security element to escape the string
            textField.value = SecurityElement.Escape(textField.value);
        }

        private void UnescapeString()
        {
            var textField = rootVisualElement.Q<TextField>();

            // use security element to unescape the string
            textField.value = new SecurityElement("", textField.value).Text;
        }

        private void ClearTextField()
        {
            var textField = rootVisualElement.Q<TextField>();
            textField.value = "";
        }
    }
}
#endif
