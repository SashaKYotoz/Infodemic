using SQLite4Unity3d;

public class WordPanels
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public int EventId { get; set; }

}