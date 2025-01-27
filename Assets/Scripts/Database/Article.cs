using SQLite4Unity3d;

public class Article
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
}
