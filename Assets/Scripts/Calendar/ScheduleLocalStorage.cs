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
        Debug.Log("Local database initialized at: " + DbPath);
    }
}