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
                sb.AppendLine($"Inferred Tasks: {response.response.Length}");
                sb.AppendLine();

                foreach (var inferredTask in response.response)
                {
                    sb.AppendLine($"📝 Task: {inferredTask.task}");
                    sb.AppendLine($"📂 Category: {inferredTask.category}");
                    sb.AppendLine($"⏰ Start: {inferredTask.start}");
                    sb.AppendLine($"⏱ End: {inferredTask.end}");
                    sb.AppendLine(); 
                }
                
                apiRequestManager.outputText.text = "📅 From your chat, I inferred the following schedule:\n\n" + sb.ToString();
            }
            else if (apiRequestManager.outputText != null)
            {
                // Handle cases where the AI responds but infers no tasks
                apiRequestManager.outputText.text = "I can help with that! What would you like to schedule?";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing chat response: {e.Message}");
            if (apiRequestManager.outputText != null)
            {
                apiRequestManager.outputText.text = "❌ Sorry, I received a response I couldn't understand.";
            }
        }
    }
}