using System;
using System.Collections;
using UnityEngine;

namespace Landmarks.Scripts.Debugging
{
    public class LM_Debug : MonoBehaviour
    {
        public static LM_Debug Instance { get; private set; }
        public int minimalPriorityLevel = 0;
        public bool _printing = false;

        public bool Printing
        {
            get => _printing;
            set => _printing = value;
        }

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
            // var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            // Log message
            UnityEngine.Debug.Log($" {WrapWithBold(WrapWithColor("[INFO]", "green"))} {message}");
        }


        //private function to log error
        private void LogError(string message)
        {
            // Get timestamp
            // var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            // Log message
            UnityEngine.Debug.Log($" {WrapWithBold(WrapWithColor("[ERROR]", "red"))} {message}");
        }


        // public function to log an generic argument
        // if the argument is a collection (array, list, etc), it will be logged as a comma-separated list
        public void Log(object message, int priorityLevel = 0)
        {
            if (priorityLevel < minimalPriorityLevel) return;
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
        
        private void LogWarning(string message)
        {
            // Get timestamp
            // var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            // Log message
            UnityEngine.Debug.Log($" {WrapWithBold(WrapWithColor("[WARNING]", "orange"))} {message}");
        }
        
        public void LogWarning(object message)
        {
            if (message is ICollection collection)
            {
                LogWarning(string.Join(", ", collection));
            }
            else
            {
                LogWarning(message.ToString());
            }
        }

        private static string WrapWithTag(string content, string xmlTag)
        {
            try
            {
                return $"<{xmlTag}>{content}</{xmlTag.Split('=')[0]}>";
            }
            catch (Exception e)
            {
                return content;
            }
        }

        private static string WrapWithColor(string content, string color)
        {
            return WrapWithTag(content, $"color={color}");
        }

        private static string WrapWithSize(string content, string size)
        {
            return WrapWithTag(content, $"size={size}");
        }

        private static string WrapWithBold(string content)
        {
            return WrapWithTag(content, "b");
        }

        private static string WrapWithItalic(string content)
        {
            return WrapWithTag(content, "i");
        }
    }
}