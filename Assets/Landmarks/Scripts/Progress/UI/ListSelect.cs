using System;
using System.Collections.Generic;
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
        [SerializeField] private TMP_InputField inputField;
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
                inputField.text = GetText(itemPrefab);
            });
            _canvas = GetComponent<Canvas>();
        }

        public void Show()
        {
            _canvas.sortingOrder = 100;
        }
        
        public void Hide()
        {
            _canvas.sortingOrder = 0;
        }

        public void Cancel()
        {
            Hide();
            SetList(new[]{""});
        }

        public void Confirm()
        {
            if (inputField.text == null) return;
            onConfirm.Invoke(inputField.text);
            SetList(new[]{""});
            Hide();
        }
        private void InstantiateItem(string text)
        {
            if (items.Count == 0) return;
            var item = Instantiate(itemPrefab, content);
            var newPosition = item.transform.position;
            item.transform.position =
                new Vector3(newPosition.x, newPosition.y - itemHeight * items.Count, newPosition.z);
            item.onClick.AddListener(() =>
            {
                SelectionIndex = items.IndexOf(item);
                inputField.text = GetText(item);
            });
            items.Add(item);
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
                InstantiateItem(targetList[i]);
            }

            // Update text and destroy any extra buttons if necessary
            for (int i = 0; i < targetList.Count; i++)
            {
                if (i < items.Count)
                {
                    items[i].GetComponentInChildren<TMP_Text>().text = targetList[i]; // Assuming the Button prefab has a Text child
                }
                else
                {
                    Destroy(items[i].gameObject); // Destroy the extra button
                }
            }

            // Remove the destroyed buttons from the list
            if (targetList.Count < items.Count)
                items.RemoveRange(targetList.Count, items.Count - targetList.Count); 
            
            var size = content.sizeDelta;
            
            // Adjust the scrollable content size to fit the list 
            content.sizeDelta = new Vector2(size.x, itemHeight * targetList.Count);
        }
    }
}