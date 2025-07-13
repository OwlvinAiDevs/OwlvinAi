using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

[Serializable]
public class TaskData
{
    public string title;
    public string due_date;
    public int duration_minutes;
    public string category;
}

[Serializable]
public class TimeSlotData
{
    public string start_time;
    public string end_time;
}

[Serializable]
public class StudyRequest
{
    public string user_id;
    public int[] energy_level;
    public int pomodoro_length;
    public TimeSlotData[] available_slots;
    public TaskData[] tasks;
}

[Serializable]
public class ScheduledTask
{
    public string title;
    public string due_date;
    public int duration_minutes;
    public string category;
}

[Serializable]
public class SessionData
{
    public ScheduledTask task;
    public string start_time;
    public string end_time;
    public int break_after;
}

[Serializable]
public class ScheduleResponse
{
    public string user_id;
    public List<SessionData> sessions;
    public int total_study_time;
    public int total_break_time;
    public bool success;
    public string message;
    public List<string> warnings;
}

[Serializable]
public class StudyRequestWrapper
{
    public string user_id;
    public int[] energy_level;
    public int pomodoro_length;
    public TimeSlotData[] available_slots;
    public TaskData[] tasks;
}

public class ScheduleApiClient : MonoBehaviour
{
    public TextMeshProUGUI outputText;
    private readonly string API_URL = ApiConfig.GetFullUrl(ApiConfig.Endpoints.GenerateSchedule);

    public void SendMockScheduleRequest()
    {
        StartCoroutine(RunScheduleRequest());
    }

    public void RequestScheduleFromBackend(int userId)
    {
        StartCoroutine(GetAndForwardUserState(userId));
    }

    private IEnumerator GetAndForwardUserState(int userId)
    {
        string userStateUrl = ApiConfig.GetFullUrl(ApiConfig.Endpoints.GetUserState) + $"?user_id={userId}";
        UnityWebRequest getRequest = UnityWebRequest.Get(userStateUrl);
        getRequest.timeout = 45;
        getRequest.SetRequestHeader("Content-Type", "application/json");

        yield return getRequest.SendWebRequest();

        if (getRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to get user state: " + getRequest.error);
            yield break;
        }

        string json = getRequest.downloadHandler.text;
        Debug.Log("User State JSON: " + json);

        UnityWebRequest postRequest = new UnityWebRequest(ApiConfig.GetFullUrl(ApiConfig.Endpoints.GenerateSchedule), "POST");
        postRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        postRequest.downloadHandler = new DownloadHandlerBuffer();
        postRequest.SetRequestHeader("Content-Type", "application/json");
        postRequest.timeout = 60;

        yield return postRequest.SendWebRequest();

        if (postRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to get schedule: " + postRequest.error);
            Debug.LogError("Response text: " + postRequest.downloadHandler.text);
            yield break;
        }
        else
        {
            ScheduleResponse schedule = JsonUtility.FromJson<ScheduleResponse>(postRequest.downloadHandler.text);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Parsed Schedule for: {schedule.user_id}");
            sb.AppendLine($"Total Sessions: {schedule.sessions.Count}");

            foreach (var session in schedule.sessions)
            {
                sb.AppendLine($"Task: {session.task.title}, Start: {session.start_time}, End: {session.end_time}, Break After: {session.break_after} minutes");
            }

            if (schedule.warnings != null && schedule.warnings.Count > 0)
            {
                sb.AppendLine("\n⚠ Warnings:");
                foreach (var warning in schedule.warnings)
                {
                    sb.AppendLine($"• {warning}");
                }
            }

            if (outputText != null)
            {
                outputText.text = sb.ToString(); // Display result in ScrollView
            }
            else
            {
                Debug.LogWarning("⚠ outputText is null. UI not updated.");
            }
        }
    }

    private IEnumerator RunScheduleRequest()
    {
        StudyRequest request = new StudyRequest
        {
            user_id = "unity_test_user",
            energy_level = new int[] { 3, 2 },
            pomodoro_length = 25,
            available_slots = new TimeSlotData[]
            {
                new TimeSlotData { start_time = DateTime.UtcNow.AddHours(1).ToString("o"), end_time = DateTime.UtcNow.AddHours(3).ToString("o") },
                new TimeSlotData { start_time = DateTime.UtcNow.AddHours(4).ToString("o"), end_time = DateTime.UtcNow.AddHours(6).ToString("o") }
            },
            tasks = new TaskData[]
            {
                new TaskData { title = "Unity essay task", due_date = DateTime.UtcNow.AddDays(1).ToString("o"), duration_minutes = 60, category = "Unity" },
                new TaskData { title = "Unity review notes", due_date = DateTime.UtcNow.AddDays(2).ToString("o"), duration_minutes = 45, category = "Math" }
            }
        };

        string json = JsonUtility.ToJson(request, true);
        UnityWebRequest www = new UnityWebRequest(API_URL, "POST");
        www.timeout = 45; // Set a timeout for the request
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("Sending POST request to: " + API_URL);
        Debug.Log("Request JSON: " + json);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
            Debug.LogError("Response Text: " + www.downloadHandler.text);
        }
        else
        {
            ScheduleResponse schedule = JsonUtility.FromJson<ScheduleResponse>(www.downloadHandler.text);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Parsed Schedule for: {schedule.user_id}");
            sb.AppendLine($"Total Sessions: {schedule.sessions.Count}");

            foreach (var session in schedule.sessions)
            {
                sb.AppendLine($"Task: {session.task.title}, Start: {session.start_time}, End: {session.end_time}, Break After: {session.break_after} minutes");
            }

            if (schedule.warnings != null && schedule.warnings.Count > 0)
            {
                sb.AppendLine("\n⚠ Warnings:");
                foreach (var warning in schedule.warnings)
                {
                    sb.AppendLine($"• {warning}");
                }
            }

            if (outputText != null)
            {
                outputText.text = sb.ToString(); // Display result in ScrollView
            }
            else
            {
                Debug.LogWarning("⚠ outputText is null. UI not updated.");
            }
        }
    }
}