using SQLite4Unity3d;

public class OrganizationTypes
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
}
