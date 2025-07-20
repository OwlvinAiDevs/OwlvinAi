using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScheduleLocalStorage : MonoBehaviour
{
    void Awake()
    {
        if (DatabaseManager.db == null)
        {
            DatabaseManager.Init();  // Ensure the DB is initialized
        }
    }

    public void SaveScheduleLocally(ScheduleResponse response)
    {
        if (response == null || response.sessions == null) return;

        int userId = int.Parse(response.user_id);

        // Clear previous sessions for this user
        DatabaseManager.db.Execute("DELETE FROM scheduled_sessions WHERE user_id = ?", userId);

        foreach (var session in response.sessions)
        {
            var task = session.task;
            var entry = new ScheduledSession
            {
                user_id = userId,
                task_id = session.task_id > 0 ? session.task_id : 0,
                start_time = DateTime.Parse(session.start_time),
                end_time = DateTime.Parse(session.end_time),
                break_after = session.break_after
            };

            DatabaseManager.db.Insert(entry);
        }

        Debug.Log($"Stored {response.sessions.Count} sessions for user {response.user_id}.");
    }

    public List<ScheduledSession> GetScheduleForUser(int userId)
    {
        return DatabaseManager.db.Table<ScheduledSession>().Where(s => s.user_id == userId).ToList();
    }
}
