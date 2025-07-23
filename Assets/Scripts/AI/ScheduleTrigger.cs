using TMPro;
using UnityEngine;

public class ScheduleTrigger : MonoBehaviour
{
    public ScheduleApiClient scheduleClient;
    public LLMChatManager chatManager;
    public TMP_InputField chatInputField;
    public int userId = 1; // this can be overridden in the inspector

    public void TriggerSchedule()
    {
        if (chatInputField != null && !string.IsNullOrWhiteSpace(chatInputField.text))
        {
            // If there's text, use chat route
            Debug.Log("Chat input detected, triggering chat generation.");
            if (chatManager != null)
            {
                chatManager.OnGenerateClicked();
            }
            else
            {
                Debug.LogWarning("ChatManager not assigned.");
            }
        }
        else
        {
            // If chat input is empty, use schedule route
            if (scheduleClient != null)
            {
                Debug.Log("No chat input detected, triggering schedule generation.");
                scheduleClient.RequestScheduleFromBackend(userId);
            }
            else
            {
                Debug.LogWarning("ScheduleApiClient reference is not set.");
            }
        }
    }
}
