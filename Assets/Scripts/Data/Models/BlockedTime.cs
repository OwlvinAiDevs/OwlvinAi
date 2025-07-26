using System;
using SQLite;

[Table("blocked_times")]
public class BlockedTime
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }

    public DateTime start_time { get; set; }
    public DateTime end_time { get; set; }

    public string reason { get; set; }
}
