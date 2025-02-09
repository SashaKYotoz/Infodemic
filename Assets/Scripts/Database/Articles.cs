using SQLite4Unity3d;

public class Articles
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int MediaId { get; set; }
    public int EventId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public float VeracityScore { get; set; }
}
