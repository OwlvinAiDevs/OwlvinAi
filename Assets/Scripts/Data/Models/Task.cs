using System;
using SQLite;

[Table("tasks")]
public class Task
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    public string title { get; set; }
    public string description { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime due_date { get; set; }
    public int duration_minutes { get; set; }
    public bool completed { get; set; } = false;
    public string category { get; set; }

    [Indexed]
    public int user_id { get; set; }
}
