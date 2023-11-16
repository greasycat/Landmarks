#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UIElements.Button;

namespace Landmarks.Scripts.Debugging
{
    public class UIMaterialSetter : LM_EditorWindow
    {

        private ObjectField _materialField;
        public void CreateGUI()
        {
            var root = rootVisualElement;

            // add a material select
            _materialField = new ObjectField("Material")
            {
                objectType = typeof(Material)
            };
            root.Add(_materialField);

            root.Add(new Button(SetMaterial) { text = "Set Material" });
        }

        [MenuItem("EditorUtility/Set Material")]
        public static void ShowWindow()
        {
            LM_EditorWindow.ShowWindow<UIMaterialSetter>("UIMaterialSetter");
        }

        private void SetMaterial()
        {
            var material = _materialField.value as Material;
            if (material == null)
            {
                Debug.LogError("Material is null");
                return;
            }

            // get all the UI elements
            var uiElements = FindObjectsOfType<MaskableGraphic>();
            foreach (var uiElement in uiElements)
            {
                // set the material of the UI element
                if (uiElement.material != null)
                    uiElement.material = material;

                UnityEditor.EditorUtility.SetDirty(uiElement);
            }


        }
    }
}
#endif
