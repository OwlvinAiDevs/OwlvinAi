using System;
using SQLite;

[Table("users")]
public class User
{
    [PrimaryKey]
    public int id { get; set; }

    public string username { get; set; }
    [Unique]
    public string email { get; set; }
    public string auth_provider { get; set; }
    public DateTime date_created { get; set; } = DateTime.UtcNow;
}
