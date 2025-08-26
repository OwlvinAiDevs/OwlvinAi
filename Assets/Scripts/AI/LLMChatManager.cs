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
}