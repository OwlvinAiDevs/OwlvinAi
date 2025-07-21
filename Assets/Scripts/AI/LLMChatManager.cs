using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
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
    public string response;
}

public class LLMChatManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField inputField;
    public TextMeshProUGUI outputText;
    public int userId = 1;
    public bool includeContext = false;

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

    private string CleanGPTResponse(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        // Split response if there is a JSON block
        int jsonStart = raw.IndexOf("[{");
        string naturalText = (jsonStart >= 0) ? raw.Substring(0, jsonStart).Trim() : raw.Trim();
        string jsonBlock = (jsonStart >= 0) ? raw.Substring(jsonStart).Trim() : "";

        // Replace any problematic characters (fallback: emoji and fancy quotes)
        naturalText = naturalText
            .Replace("“", "\"")
            .Replace("”", "\"")
            .Replace("‘", "'")
            .Replace("’", "'");

        // Collapse JSON to a single line or highlight it
        if (!string.IsNullOrEmpty(jsonBlock))
        {
            naturalText += "\n\n JSON Task Data:\n" + jsonBlock;
        }
        return naturalText;
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

            ChatResponse response = JsonUtility.FromJson<ChatResponse>(jsonResponse);
            if (outputText != null)
            {
                string cleaned = CleanGPTResponse(response.response);
                outputText.text = "🤖 GPT says:\n\n" + cleaned;
            }
        }
    }
}