using System;
using SQLite;

[Table("ai_responses")]
public class AIResponse
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }

    public DateTime timestamp { get; set; } = DateTime.UtcNow;
    public string response_json { get; set; }
}
