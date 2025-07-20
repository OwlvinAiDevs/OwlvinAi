using System;
using SQLite;

[Table("ai_chat_logs")]
public class AIChatLog
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }

    public DateTime timestamp { get; set; } = DateTime.UtcNow;
    public string role { get; set; }  // "user" or "assistant"
    public string message { get; set; }
}
