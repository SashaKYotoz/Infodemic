using SQLite4Unity3d;

public class WordFolders
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int EventId { get; set; }
    public string FolderName { get; set; }
    public string FolderDescription { get; set; }
}