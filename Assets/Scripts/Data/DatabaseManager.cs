using System.IO;
using UnityEngine;
using SQLite;

public static class DatabaseManager
{
    public static SQLiteConnection db;

    public static void Init(string dbName = "UserSchedule.db")
    {
        string dbPath = Path.Combine(Application.persistentDataPath, dbName);
        db = new SQLiteConnection(dbPath);

        db.CreateTable<User>();
        db.CreateTable<Task>();
        db.CreateTable<ScheduledSession>();
        db.CreateTable<SessionLog>();
        db.CreateTable<EnergyLevel>();
        db.CreateTable<CachedAvailability>();
        db.CreateTable<BlockedTime>();
        db.CreateTable<AIResponse>();
        db.CreateTable<AIChatLog>();

        Debug.Log($"[SQLite] Database initialized at: {dbPath}");
    }

    public static void Close()
    {
        db?.Close();
        Debug.Log("[SQLite] Database connection closed.");
    }
}