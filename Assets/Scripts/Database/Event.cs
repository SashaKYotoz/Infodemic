using SQLite4Unity3d;

public class Event
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Title { get; set; }
    public string Date { get; set; }
    public string Location { get; set; }
    public string GeneratedContent { get; set; }
    public int EventTypeId { get; set; } // Foreign key to EventType


}
