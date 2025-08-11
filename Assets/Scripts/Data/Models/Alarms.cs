using SQLite;

[Table("alarms")]
public class Alarms
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }

    public int hour { get; set; }
    public int minute { get; set; }
    public bool is_enabled { get; set; } = true;
}