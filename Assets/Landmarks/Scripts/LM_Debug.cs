using System.Collections;
using UnityEngine;

namespace Landmarks.Scripts
{
    public class LM_Debug: MonoBehaviour
    {
        public static LM_Debug Instance { get; private set; }
        public bool Printing { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                DontDestroyOnLoad(this);
            }
            else
            {
                Instance = this;
            }
        }
        
        //private function to log
        private void Log(string message)
        {
            if (!Printing) return;
            // Get timestamp
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            // Log message
            Debug.Log(timestamp + "\t" + message);
        }
        
        //private function to log error
        private void LogError(string message)
        {
            // Get timestamp
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            // Log message
            Debug.LogError(timestamp + "\t" + message);
        }
        
        
        // public function to log an generic argument
        // if the argument is a collection (array, list, etc), it will be logged as a comma-separated list
        public void Log(object message)
        {
            if (message is ICollection collection)
            {
                Log(string.Join(", ", collection));
            }
            else
            {
                Log(message.ToString());
            }
        }
        
        // public function to log an error with a generic argument
        
        public void LogError(object message)
        {
            if (message is ICollection collection)
            {
                LogError(string.Join(", ", collection));
            }
            else
            {
                LogError(message.ToString());
            }
        }
        
        
        
        
    }
}