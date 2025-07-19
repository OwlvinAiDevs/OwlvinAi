using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SQLite;

public class ScheduleLocalStorage : MonoBehaviour
{
    private static string DbPath => Path.Combine(Application.persistentDataPath, "UserSchedule.db");
    private SQLiteConnection db;

    void Awake()
    {
        db = new SQLiteConnection(DbPath);
        db.CreateTable<ScheduledSession>();
        Debug.Log("Local DB initialized at: " + DbPath);
    }

    public void SaveScheduleLocally(ScheduleResponse response)
    {
        // Clear existing sessions for this user
        db.Execute("DELETE FROM scheduled_sessions WHERE UserId = ?", response.user_id);

        foreach (var session in response.sessions)
        {
            var entry = new ScheduledSession
            {
                UserId = int.Parse(response.user_id),
                TaskTitle = session.task.title,
                TaskCategory = session.task.category,
                StartTime = DateTime.Parse(session.start_time),
                EndTime = DateTime.Parse(session.end_time),
                BreakAfter = session.break_after
            };

            db.Insert(entry);
        }

        Debug.Log($"Stored {response.sessions.Count} scheduled sessions locally for user {response.user_id}.");
    }

    public List<ScheduledSession> GetScheduleForUser(int userId)
    {
        return db.Table<ScheduledSession>().Where(s => s.UserId == userId).ToList();
    }
}