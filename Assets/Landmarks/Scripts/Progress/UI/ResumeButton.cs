﻿using System.IO;
using System.Linq;
using Landmarks.Scripts.Debugging;
using UnityEngine;

namespace Landmarks.Scripts.Progress.UI
{
    public class ResumeButton : MonoBehaviour
    {
        private ListSelect _listSelect;
        private LM_ExpStartup _expStartup;
        private LM_Progress _progress;

        private void Start()
        {
            _listSelect = FindObjectOfType<ListSelect>();
            _expStartup = FindObjectOfType<LM_ExpStartup>();
            _progress = LM_Progress.Instance;
        }


        public void OnClick()
        {
            HandleFolder(LM_Progress.GetSaveFolder());
            _listSelect.Show();
        }


        public void OnConfirm(string text)
        {
            if (File.GetAttributes(text).HasFlag(FileAttributes.Directory))
                HandleFolder(text);
            else
                HandleFile(text);
        }

        public void OnItemClick(string text)
        {
            LM_Debug.Instance.Log("Item clicked: " + text, 10);
            if (File.GetAttributes(text).HasFlag(FileAttributes.Directory))
                HandleFolder(text);
        }

        private void HandleFile(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (folder == "")
            {
                _listSelect.Hide();
                return;
            }

            _expStartup.ExtraInitCallback = () =>
            {
                _progress.SetSavingFolderPath(folder);
                _progress.EnableResuming();
                _progress.InitializeSave(path);
            };
            _expStartup.OnStartButtonClicked();
        }

        private void HandleFolder(string path)
        {
            // Set the list to a new folder
            LM_Debug.Instance.Log("Setting list to " + path, 10);
            var folders = Directory.GetDirectories(path).Prepend(Path.GetDirectoryName(path));
            var files = Directory.GetFiles(path);
            
            // combine these two lists
            folders = folders.Concat(files);
            _listSelect.SetList(folders.ToList());
        }
    }
}