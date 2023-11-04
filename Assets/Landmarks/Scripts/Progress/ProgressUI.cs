using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Landmarks.Scripts.Progress
{
    public class ProgressUI : MonoBehaviour
    {
        [SerializeField] private Button placeholderItem;
        private Transform _root;
        private List<Button> _textList;
        private int SelectionIndex { get; set; }   

        private void Start()
        {
            _textList = new List<Button> {placeholderItem};
            placeholderItem.onClick.AddListener(() =>
            {
                SelectionIndex = _textList.IndexOf(placeholderItem);
                Debug.Log(SelectionIndex);
            });
            _root = placeholderItem.transform.parent;
            var list = new List<string>()
            {
                "1", "2", "3"
            };
            SetList(list);
        }

        private void InstantiateItem(string content)
        {
            if (_textList.Count == 0) return;
            var item = Instantiate(placeholderItem, _root);
            var newPosition = item.transform.position;
            item.transform.position = new Vector3(newPosition.x, newPosition.y - 45f*_textList.Count, newPosition.z);
            item.onClick.AddListener(() =>
            {
                SelectionIndex = _textList.IndexOf(item);
                Debug.Log(SelectionIndex);
            });
            SetText(item, content);
            _textList.Add(item);
        }

        private void SetText(Button button, string text)
        {
            // Get the child TMP_Text component of the button
            var buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }
        
        private string GetText(Button button) => button.GetComponentInChildren<TMP_Text>().text;

        private void SetList(IReadOnlyList<string> list)
        {
            if (list == null || list.Count == 0)
            {
                _textList.ForEach(button => SetText(button, ""));
                return;
            }
            if (_textList.Count < list.Count)
            {
                for (var i = 0; i < _textList.Count; i++)
                {
                    SetText(_textList[i], list[i]);
                }
                for (var i = _textList.Count; i < list.Count; i++)
                {
                    InstantiateItem(list[i]);
                }
            }
            else if (_textList.Count > list.Count)
            {
                for (var i = list.Count; i < _textList.Count; i++)
                {
                    Destroy(_textList[i].gameObject);
                }
                
                for (var i = 0; i < list.Count; i++)
                {
                    SetText(_textList[i], list[i]);
                }
            }

        }
    }
}