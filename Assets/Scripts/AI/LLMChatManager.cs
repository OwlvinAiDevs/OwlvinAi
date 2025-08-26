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
            StartCoroutine(SendChatPrompt(message));
        }
        else
        {
            Debug.LogWarning("Input field is empty.");
            if (outputText != null)
            {
                outputText.text = "⚠ Please enter a message before submitting.";
            }
        }
    }

    private IEnumerator SendChatPrompt(string userInput)
    {
        ChatPrompt prompt = new ChatPrompt
        {
            user_id = userId,
            message = userInput,
            include_context = includeContext
        };

        string json = JsonUtility.ToJson(prompt);

        UnityWebRequest request = new UnityWebRequest(chatApiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 60;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("GPT request failed: " + request.error);
            if (outputText != null)
            {
                outputText.text = "❌ Error: " + request.error;
            }
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("GPT response JSON: " + jsonResponse);

            try
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(jsonResponse);
                if (outputText != null && response.response != null && response.response.Length > 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();

                    foreach (var inferred in response.response)
                    {
                        if (inferred.task.ToLower().Contains("break") || inferred.category.ToLower().Contains("rest"))
                            continue;

                        sb.AppendLine($"📝 Task: {inferred.task}");
                        sb.AppendLine($"📂 Category: {inferred.category}");
                        sb.AppendLine($"⏰ Start: {inferred.start}");
                        sb.AppendLine($"⏱ End: {inferred.end}");
                        sb.AppendLine($"☕ Break After: 5 minutes\n");
                    }

                    outputText.text = "📅 AI-Generated Schedule:\n\n" + sb.ToString();
                }
                else
                {
                    outputText.text = "⚠ GPT returned no tasks.";
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse GPT response: " + e.Message);
                if (outputText != null)
                {
                    outputText.text = "⚠ Failed to read GPT response.";
                }
            }
        }
    }
}