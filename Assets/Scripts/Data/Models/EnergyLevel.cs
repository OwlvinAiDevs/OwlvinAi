using System;
using SQLite;

[Table("energy_levels")]
public class EnergyLevel
{
    [PrimaryKey, AutoIncrement]
    public int id { get; set; }

    [Indexed]
    public int user_id { get; set; }

    public DateTime timestamp { get; set; } = DateTime.UtcNow;
    public string level { get; set; } // "low", "medium", "high"
}
