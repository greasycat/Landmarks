using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Landmarks.Scripts.Progress.UI
{
    public class ListSelect : MonoBehaviour
    {
        [SerializeField] private Button itemPrefab;
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_InputField selectedTextField;
        [SerializeField] private float itemHeight = 48f;

        [Serializable]
        public class ConfirmEvent : UnityEvent<string>
        {
            public string text;
        }

        [SerializeField]
        private ConfirmEvent onConfirm = new ConfirmEvent();
        [SerializeField]
        private ConfirmEvent onItemClick = new ConfirmEvent();

        [FormerlySerializedAs("_canvas")] [SerializeField] private Canvas canvas;
        private List<Button> _items;
        private int SelectionIndex { get; set; }



        private void Start()
        {
            _items = new List<Button>();
        }

        public void Show()
        {
            canvas.enabled = true;
        }

        public void Hide()
        {
            canvas.enabled = false;
            selectedTextField.text = "";
        }

        public void Cancel()
        {
            Hide();
            ClearList();
        }

        public void Confirm()
        {
            var text = GetSelectionText();
            if (text == null) return;
            onConfirm?.Invoke(text);
            ClearList();
            Hide();
        }
        
        private string GetSelectionText()
        {
            var text = selectedTextField.text;
            return text == null ? null : Unescape(text);
        }

        private static string GetText(Component button) => Unescape(button.GetComponentInChildren<TMP_Text>().text);
        
        private void AddOnClick(Button button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                SelectionIndex = _items.IndexOf(button);
                selectedTextField.text = GetText(button);
                onItemClick?.Invoke(GetText(button));
            });
        }

        public void SetList(IReadOnlyList<string> targetList)
        {
            if (targetList.Count == 0)
            {
                return; // The provided list is empty, so just return
            }
            
            ClearList();

            // If there are not enough buttons in the list, instantiate new ones
            for (var i = 0; i < targetList.Count; i++)
            {
                Debug.Log("Instantiating new item");
                var item = Instantiate(itemPrefab, content);
                var itemTransform = item.transform;
                var pos = itemTransform.localPosition;
                itemTransform.localPosition = new Vector3(pos.x, pos.y - itemHeight * i, pos.z);
                
                item.GetComponentInChildren<TMP_Text>().text = Escape(targetList[i]); // Assuming the Button prefab has a Text child
                AddOnClick(item);
                _items.Add(item);
            }



            var size = content.sizeDelta;
            // Adjust the scrollable content size to fit the list
            content.sizeDelta = new Vector2(size.x, itemHeight * targetList.Count);
        }
        
        private static string Escape(string path)
        {
            return path.Replace(@"\", @"\\");
        }
        
        private static string Unescape(string path)
        {
            return path.Replace(@"\\", @"\");
        }

        public delegate List<string> ItemModifier(IReadOnlyList<string> targetList);
        public void SetList(IReadOnlyList<string> targetList, ItemModifier modifier)
        {
            var newList = modifier(targetList);
            SetList(newList);
        }

        private void ClearList()
        {
            foreach (var item in _items)
            {
                Destroy(item.gameObject);
            }

            _items.Clear();
        }
    }
}
