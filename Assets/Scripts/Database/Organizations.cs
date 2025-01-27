using SQLite4Unity3d;
public class Organizations
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public int TypeId { get; set; }  // Foreign key to OrganizationTypes.Id
    public string Description { get; set; }
    public float Credibility { get; set; } // Credibility scale: 1.0 to 10.0
}
