using Landmarks.Scripts.Progress;
using UnityEngine;

namespace Landmarks.Scripts.ExperimentTasks
{
    public interface INavigationTask
    {
        void SetTimeAllotted(float time);
        Vector3 GetPlayerPosition();
        Quaternion GetPlayerRotation();
        
        TaskList GetParentTask();
        
        float GetTimeRemaining();
        
        float GetTimeAllotted();
        
        void SetStartTime(float time);
    }
}