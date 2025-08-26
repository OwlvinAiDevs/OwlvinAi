using System;
using UnityEngine;
using TMPro;

[Serializable]
public class ChatPrompt
{
    public int user_id;
    public string message;
    public bool include_context;
}

[Serializable]
public class ChatResponse
{
    public InferredTask[] response;
}

public class LLMChatManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField inputField;
    public ApiRequestManager apiRequestManager;
    public int userId = 1;

    public void OnGenerateClicked()
    {
        string message = inputField.text.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            // Build the specific request payload.
            ChatPrompt prompt = new ChatPrompt
            {
                user_id = userId,
                message = message,
                context = "" // Build from local DB if needed
            };
            string jsonPayload = JsonUtility.ToJson(prompt);
            string url = ApiConfig.GetFullUrl(ApiConfig.Endpoints.Chat);

            // Tell the central manager to send the request.
            apiRequestManager.SendRequest(url, jsonPayload, OnChatSuccess);
        }
        else
        {
            // ... (handle empty input field) ...
        }
    }

    // This method ONLY handles a successful chat response.
    private void OnChatSuccess(string jsonResponse)
    {
        try
        {
            ChatResponse response = JsonUtility.FromJson<ChatResponse>(jsonResponse);
            if (apiRequestManager.outputText != null && response.response != null && response.response.Length > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"Total Sessions: {schedule.sessions.Count}");
                sb.AppendLine();

                foreach (var session in schedule.sessions)
                {
                    if (session.task.title.ToLower().Contains("break") || session.task.category.ToLower().Contains("rest"))
                        continue;

                    sb.AppendLine($"📝 Task: {session.task.title}");
                    sb.AppendLine($"📂 Category: {session.task.category}");
                    sb.AppendLine($"⏰ Start: {session.start_time}");
                    sb.AppendLine($"⏱ End: {session.end_time}");
                    sb.AppendLine($"☕ Break After: {session.break_after} minutes");
                    sb.AppendLine(); // extra line for spacing
                }
                
                apiRequestManager.outputText.text = "📅 AI-Generated Schedule:\n\n" + sb.ToString();
            }
            // ... (handle other cases like no tasks returned) ...
        }
        catch (Exception e)
        {
            // ... (handle JSON parsing errors) ...
        }
    }
}