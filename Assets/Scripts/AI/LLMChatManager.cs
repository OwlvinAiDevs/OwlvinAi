using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using static ResponseFormatter;
using System.Collections.Generic;

[Serializable]
public class ChatPrompt
{
    public int user_id;
    public string message;
    public bool include_context;
}

[Serializable]
public class InferredTask
{
    public string task;
    public string start;
    public string end;
    public string category;
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
    public TextMeshProUGUI outputText;
    public int userId = 1;
    public bool includeContext = true;

    private readonly string chatApiUrl = ApiConfig.GetFullUrl(ApiConfig.Endpoints.Chat);

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
                if (outputText != null)
                {
                    List<InferredTask> inferred;
                    string cleaned = CleanGPTResponse(response.response, out inferred);

                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("🤖 GPT says:\n");
                    sb.AppendLine(cleaned);

                    if (inferred != null && inferred.Count > 0)
                    {
                        sb.AppendLine("\n📦 Inferred Tasks:");
                        foreach (var task in inferred)
                        {
                            sb.AppendLine($"📝 Task: {task.task}");
                            sb.AppendLine($"📂 Category: {task.category}");
                            sb.AppendLine($"⏰ Start: {task.start}");
                            sb.AppendLine($"⏱ End: {task.end}");
                            sb.AppendLine($"☕ Break After: {task.break_after} minutes");
                            sb.AppendLine();
                        }
                    }
                    outputText.text = sb.ToString();
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