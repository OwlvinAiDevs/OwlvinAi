using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class ApiRequestManager : MonoBehaviour
{
    public TextMeshProUGUI outputText;

    // Public method to send a request
    public void SendRequest(string url, string jsonPayload, Action<string> onSuccess)
    {
        StartCoroutine(WebRequestCoroutine(url, jsonPayload, onSuccess));
    }

    private IEnumerator WebRequestCoroutine(string url, string jsonPayload, Action<string> onSuccess)
    {
        // Start the loading animation
        Coroutine loadingAnimation = StartCoroutine(AnimateLoadingText("Contacting AI"));

        // Set up and send the web request
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 60;
        
        yield return request.SendWebRequest();

        // Stop the loading animation
        StopCoroutine(loadingAnimation);

        // Handle the response
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"API request failed: {request.error}");
            if (outputText != null)
            {
                outputText.text = "❌ Error: " + request.error;
            }
        }
        else
        {
            // If successful, call the specific success function that was passed in.
            onSuccess?.Invoke(request.downloadHandler.text);
        }
    }

    private IEnumerator AnimateLoadingText(string message)
    {
        string baseMessage = message;
        int dotCount = 0;
        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            string dots = new string('.', dotCount);
            if (outputText != null) outputText.text = baseMessage + dots;
            yield return new WaitForSeconds(0.5f);
        }
    }
}