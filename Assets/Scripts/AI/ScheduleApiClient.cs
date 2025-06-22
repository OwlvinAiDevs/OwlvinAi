using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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

public class ScheduleApiClient : MonoBehaviour
{
    private readonly string API_URL = ApiConfig.GetFullUrl(ApiConfig.Endpoints.GenerateSchedule);

    void Start()
    {
        StartCoroutine(SendMockScheduleRequest());
    }

    IEnumerator SendMockScheduleRequest()
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
        www.timeout = 15; // Set a timeout for the request
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + www.error);
        }
        else
        {
            string jsonResponse = www.downloadHandler.text;
            ScheduleResponse schedule = JsonUtility.FromJson<ScheduleResponse>(jsonResponse);
            Debug.Log("Parsed Schedule for: " + schedule.user_id);
            Debug.Log("Total Sessions: " + schedule.sessions.Count);

            foreach (var session in schedule.sessions)
            {
                Debug.Log($"Task: {session.task.title}, Start: {session.start_time}, End: {session.end_time}, Break After: {session.break_after} mins");
            }

            if (schedule.warnings != null && schedule.warnings.Count > 0)
            {
                foreach (var warning in schedule.warnings)
                {
                    Debug.LogWarning("⚠ Warning: " + warning);
                }
            }
        }
    }
}