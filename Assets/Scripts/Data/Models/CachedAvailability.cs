using System;
using SQLite;

[Table("cached_availability")]
public class CachedAvailability
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }

    public DateTime start_time { get; set; }
    public DateTime end_time { get; set; }

    public string source { get; set; } = "inferred";
}
