using SQLite4Unity3d;
public class EventType
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }  
    public string Description { get; set; }  
    public int LastUsedEventId { get; set; } // Foreign key to Event(Id)
}
