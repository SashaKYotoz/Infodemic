using SQLite4Unity3d;

public class Posts
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int EventId { get; set; }
    public int? CharacterId { get; set; }
    public int? OrganizationId { get; set; }
    public string Content { get; set; }

}
