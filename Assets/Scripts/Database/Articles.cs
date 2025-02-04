using SQLite4Unity3d;

public class Articles
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int MediaId { get; set; }
    public string EventId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public float AccuracyScore { get; set; }
    public string CretedAt { get; set; }
}
