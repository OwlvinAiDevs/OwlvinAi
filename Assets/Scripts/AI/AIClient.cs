using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AIClient : MonoBehaviour
{
    public IEnumerator GetSchedule(string jsonPayload)
    {
        string url = ApiConfig.GetFullUrl(ApiConfig.Endpoints.GenerateAiSchedule);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.timeout = 45; // Set a timeout for the request
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("Response: " + request.downloadHandler.text);
        else
            Debug.LogError("Error: " + request.error);
    }
}
