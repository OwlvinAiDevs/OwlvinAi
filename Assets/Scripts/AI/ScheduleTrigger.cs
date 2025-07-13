using UnityEngine;

public class ScheduleTrigger : MonoBehaviour
{
    public ScheduleApiClient scheduleClient;
    public int userId = 1; // this can be overridden in the inspector

    public void TriggerSchedule()
    {
        if (scheduleClient != null)
        {
            scheduleClient.RequestScheduleFromBackend(userId);
        }
        else
        {
            Debug.LogWarning("ScheduleApiClient reference is not set.");
        }
    }
}
