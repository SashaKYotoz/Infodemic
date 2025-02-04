using SQLite4Unity3d;

public class Events
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Title { get; set; }
    public string Date { get; set; }
    public string CoreTruth { get; set; }
    public string Description { get; set; }
    public string GeneratedContent { get; set; }
    public int EventTypeId { get; set; } // Foreign key to EventType


}
