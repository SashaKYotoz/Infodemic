using SQLite4Unity3d;

public class Character
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; } 
    public string Name { get; set; }
    public string Profession { get; set; }
    public int Affiliation { get; set; } // Reference to Organizations.Id
    public float Credibility { get; set; } // Credibility scale: 1.0 to 10.0
    public int Tier { get; set; }          // Tier: 1 (low), 2 (mid), 3 (high)
    public int LastUsedEventId { get; set; } // Foreign key to Event
}
