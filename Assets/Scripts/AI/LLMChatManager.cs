using System;
using UnityEngine;
using TMPro;

[Serializable]
public class ChatPrompt
{
    public int user_id;
    public string message;
    public string context;
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

    // Save a chat log entry to the database
    private void SaveChatLog(int userId, string role, string message)
    {
        var chatLog = new AIChatLog
        {
            user_id = userId,
            timestamp = DateTime.UtcNow,
            role = role,
            message = message
        };
        DatabaseManager.db.Insert(chatLog);
    }

    // Save an AI response to the database
    private void SaveAIResponse(int userId, string responseJson)
    {
        var aiResponse = new AIResponse
        {
            user_id = userId,
            timestamp = DateTime.UtcNow,
            response_json = responseJson
        };
        DatabaseManager.db.Insert(aiResponse);
    }

    public void OnGenerateClicked()
    {
        string message = inputField.text.Trim();
        if (!string.IsNullOrEmpty(message))
        {
            // Gather cached availability for the user for the next 7 days
            DateTime now = DateTime.UtcNow;
            DateTime future = now.AddDays(7);
            var slots = DatabaseManager.db.Table<CachedAvailability>()
                .Where(a => a.user_id == userId && a.start_time > now && a.end_time <= future)
                .OrderBy(a => a.start_time)
                .ToList();

            // Format as context string
            System.Text.StringBuilder contextBuilder = new System.Text.StringBuilder();
            contextBuilder.AppendLine("Available time slots (next 7 days):");
            foreach (var slot in slots)
            {
                contextBuilder.AppendLine($"- {slot.start_time:yyyy-MM-dd HH:mm} to {slot.end_time:yyyy-MM-dd HH:mm} (source: {slot.source})");
            }

            // Build the specific request payload.
            ChatPrompt prompt = new ChatPrompt
            {
                user_id = userId,
                message = message,
                context = contextBuilder.ToString()
            };
            string jsonPayload = JsonUtility.ToJson(prompt);
            string url = ApiConfig.GetFullUrl(ApiConfig.Endpoints.Chat);

            // Tell the central manager to send the request.
            apiRequestManager.SendRequest(url, jsonPayload, OnChatSuccess);

            // Save user message to ai_chat_logs
            SaveChatLog(userId, "user", message);
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

                // Save assistant response to ai_chat_logs
                SaveChatLog(userId, "assistant", sb.ToString());

                // Save full AI response JSON to ai_responses
                SaveAIResponse(userId, jsonResponse);
            }
            else if (apiRequestManager.outputText != null)
            {
                // Handle cases where the AI responds but infers no tasks
                apiRequestManager.outputText.text = "I can help with that! What would you like to schedule?";

                // Save assistant response to ai_chat_logs
                SaveChatLog(userId, "assistant", apiRequestManager.outputText.text);

                // Save full AI response JSON to ai_responses
                SaveAIResponse(userId, jsonResponse);
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