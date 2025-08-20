using System;
using SQLite;

[Table("user_notes")]
public class UserNote
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }

    public string date_key { get; set; } // "yyyy-MM-dd"
    public string note_text { get; set; }
    public DateTime last_modified { get; set; } = DateTime.UtcNow;
}