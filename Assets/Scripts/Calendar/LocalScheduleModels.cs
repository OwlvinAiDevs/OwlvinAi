using SQLite;
using System;

[Table("scheduled_sessions")]
public class ScheduledSession
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }

    [Indexed]
    public int task_id { get; set; }

    [Indexed]
    public DateTime start_time { get; set; }

    [Indexed]
    public DateTime end_time { get; set; }

    public int break_after { get; set; }
}