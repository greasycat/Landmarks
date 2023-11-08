using System.Linq;
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
            var saves = LM_Progress.GetSaveFiles(GetSavingFolderPathFromScreen());
            _listSelect.Show();
            _listSelect.SetList(saves.ToList());
        }

        private string GetSavingFolderPathFromScreen()
        {

            var id = _expStartup.GetSubjectID();
            return id == "" ? "" : LM_Progress.GetSaveFolderWithId($"{id}");
        }

        public void OnConfirm(string text)
        {
            var folder = GetSavingFolderPathFromScreen();
            if (folder == "")
            {
                _listSelect.Hide();
                return;
            }

            _expStartup.ExtraInitCallback = () =>
            {
                _progress.SetSavingFolderPath(folder);
                _progress.EnableResuming();
                _progress.InitializeSave(text);
            };

            _expStartup.OnStartButtonClicked();
        }




    }
}
