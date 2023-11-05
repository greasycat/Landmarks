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

        [SerializeField] private Canvas _canvas;
        private List<Button> items;
        private int SelectionIndex { get; set; }



        private void Start()
        {
            items = new List<Button>();
            itemPrefab.onClick.AddListener(() =>
            {
                SelectionIndex = items.IndexOf(itemPrefab);
                selectedTextField.text = GetText(itemPrefab);
            });
        }

        public void Show()
        {
            _canvas.enabled = true;
        }

        public void Hide()
        {
            _canvas.enabled = false;
            selectedTextField.text = "";
        }

        public void Cancel()
        {
            Hide();
            ClearList();
        }

        public void Confirm()
        {
            if (selectedTextField.text == null) return;
            onConfirm.Invoke(selectedTextField.text);
            ClearList();
            Hide();
        }
        private static string GetText(Component button) => button.GetComponentInChildren<TMP_Text>().text;

        public void SetList(IReadOnlyList<string> targetList)
        {
            if (targetList.Count == 0)
            {
                return; // The provided list is empty, so just return
            }

            // If there are not enough buttons in the list, instantiate new ones
            for (var i = items.Count; i < targetList.Count; i++)
            {
                Debug.Log("Instantiating new item");
                var item = Instantiate(itemPrefab, content);
                items.Add(item);
            }

            // Update text and destroy any extra buttons if necessary

            for (var i = 0; i < items.Count && i < targetList.Count; ++i)
            {
                var item = items[i];
                var newPosition = item.transform.position;
                item.transform.position =
                    new Vector3(newPosition.x, newPosition.y - itemHeight * i, newPosition.z);
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() =>
                {
                    SelectionIndex = items.IndexOf(item);
                    selectedTextField.text = GetText(item);
                });
                items[i].GetComponentInChildren<TMP_Text>().text = targetList[i]; // Assuming the Button prefab has a Text child
            }

            for (int i = targetList.Count; i < items.Count; i++)
            {
                Destroy(items[i].gameObject); // Destroy the extra button
            }

            // Remove the destroyed buttons from the list
            if (targetList.Count < items.Count)
                items.RemoveRange(targetList.Count, targetList.Count - items.Count);

            var size = content.sizeDelta;

            // Adjust the scrollable content size to fit the list
            content.sizeDelta = new Vector2(size.x, itemHeight * targetList.Count);
        }

        public delegate List<string> ItemModifier(IReadOnlyList<string> targetList);
        public void SetList(IReadOnlyList<string> targetList, ItemModifier modifier)
        {
            var newList = modifier(targetList);
            SetList(newList);
        }

        public void ClearList()
        {
            foreach (var item in items)
            {
                Destroy(item.gameObject);
            }

            items.Clear();
        }
    }
}
