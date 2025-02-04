using SQLite4Unity3d;

public class SelectedWords {
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int EventId { get; set; }
    public int PostId { get; set; }
    public string Word { get; set; }
}
