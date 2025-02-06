using SQLite4Unity3d;

public class Media
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Readers { get; set; }
    public float Credibility { get; set; }
}
