using SQLite;
using System;

[Table("scheduled_sessions")]
public class ScheduledSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int UserId { get; set; }
    public string TaskTitle { get; set; }
    public string TaskCategory { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int BreakAfter { get; set; }
}