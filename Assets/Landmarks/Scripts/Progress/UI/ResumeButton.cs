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
            var id = _expStartup.GetSubjectID();
            if (id == -1) return;
            var path = _progress.GetSaveFolderWithId($"{id}");
            var saves = LM_Progress.GetSaveFiles(path);
            _listSelect.Show();
            _listSelect.SetList(saves.ToList());
        }
        
        
        
        
    }
}