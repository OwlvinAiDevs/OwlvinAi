using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    public int task_id;
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
    public ApiRequestManager apiRequestManager;

    public void RequestScheduleFromBackend(int userId)
    {
        // Build the specific request payload.
        StudyRequest requestPayload = BuildStudyRequestFromLocalDB(userId);
        string jsonPayload = JsonUtility.ToJson(requestPayload);
        string url = ApiConfig.GetFullUrl(ApiConfig.Endpoints.GenerateSchedule);

        // Tell the central manager to send the request, and tell it which method to call on success.
        apiRequestManager.SendRequest(url, jsonPayload, OnScheduleSuccess);
    }

    // This method ONLY handles a successful response.
    private void OnScheduleSuccess(string jsonResponse)
    {
        ScheduleResponse schedule = JsonUtility.FromJson<ScheduleResponse>(jsonResponse);

        var localSaver = FindObjectOfType<ScheduleLocalStorage>();
        if (localSaver != null)
        {
            localSaver.SaveScheduleLocally(schedule);
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Total Sessions: {schedule.sessions.Count}");
        sb.AppendLine();

        foreach (var session in schedule.sessions)
        {
            if (session.task.title.ToLower().Contains("break") || session.task.category.ToLower().Contains("rest"))
                continue;

            sb.AppendLine($"📝 Task: {session.task.title}");
            sb.AppendLine($"📂 Category: {session.task.category}");
            sb.AppendLine($"⏰ Start: {session.start_time}");
            sb.AppendLine($"⏱ End: {session.end_time}");
            sb.AppendLine($"☕ Break After: {session.break_after} minutes");
            sb.AppendLine(); // extra line for spacing
        }

        if (apiRequestManager.outputText != null)
        {
            apiRequestManager.outputText.text = "📅 AI-Generated Schedule:\n\n" + sb.ToString();
        }
    }

    private StudyRequest BuildStudyRequestFromLocalDB(int userId)
    {
        // Query UserSchedule.db for all non-completed tasks for the user
        var userTasks = DatabaseManager.db.Table<Task>()
            .Where(t => t.user_id == userId && !t.completed)
            .ToList();

        // Convert db task model to TaskData model required by the API
        List<TaskData> taskDataList = new List<TaskData>();
        foreach (var task in userTasks)
        {
            taskDataList.Add(new TaskData
            {
                title = task.title,
                due_date = task.due_date.ToString("o"),
                duration_minutes = task.duration_minutes,
                category = task.category
            });
        }

        // Query for availability, energy, etc., (using placeholder data for now)
        // Replace this will real queries later
        var availableSlots = new TimeSlotData[]
        {
            new TimeSlotData { start_time = DateTime.UtcNow.AddHours(1).ToString("o"),
                end_time = DateTime.UtcNow.AddHours(3).ToString("o") },
        };
        var energyLevels = new int[] { 3 };

        // Assemble and return the final request object
        return new StudyRequest
        {
            user_id = userId.ToString(),
            energy_level = energyLevels,
            pomodoro_length = 25, // Default, or replace with user settings
            available_slots = availableSlots,
            tasks = taskDataList.ToArray()
        };
    }
}