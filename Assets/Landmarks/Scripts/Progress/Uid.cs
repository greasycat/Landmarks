using UnityEngine;
using UnityEngine.Serialization;

namespace Landmarks.Scripts.Progress
{
    public class Uid: MonoBehaviour
    {
        [NotEditable, SerializeField] private uint id;
        public uint ID
        {
            get => id;
            set => id = value;
        }

        public Uid(uint id)
        {
            ID = id;
        }
        
        public static bool TryGetUid(GameObject gameObject, out uint uid)
        {
            uid = 0;
            if (gameObject == null) return false;
            
            var uidComponent = gameObject.GetComponent<Uid>();
            if (uidComponent == null) return false;
            uid = uidComponent.ID;
            return true;

        }
    }
}