using System;
using SQLite;

[Table("session_logs")]
public class SessionLog
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }
    public int? task_id { get; set; }

    public DateTime start_time { get; set; }
    public DateTime end_time { get; set; }
    public bool was_productive { get; set; } = true;
}
