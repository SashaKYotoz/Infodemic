using UnityEngine;
using SQLite4Unity3d;

public class DatabaseManager : MonoBehaviour
{
    private SQLiteConnection _connection;

    private void Start()
    {
        string dbPath = $"{Application.persistentDataPath}/Infodemic.db";

        if (!System.IO.File.Exists(dbPath))
        {
            Debug.Log("Database not found. Creating a new one...");
            _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            CreateTables();
        }
        else
        {
            Debug.Log("Database found. Connecting...");
            _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite);
        }

        _connection.Execute("PRAGMA foreign_keys = ON;");
    }

    private void CreateTables()
    {
        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Article (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Title VARCHAR,
                Content VARCHAR
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Biases (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name VARCHAR
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS CharacterBiases (
                CharacterId INTEGER,
                BiasId INTEGER,
                PRIMARY KEY (CharacterId, BiasId),
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id),
                FOREIGN KEY (BiasId) REFERENCES Biases(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Characters (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT,
                Profession TEXT,
                Affilation INTEGER,
                Credibility REAL CHECK(Credibility BETWEEN 1 AND 10),
                Tier INTEGER,
                LastUsedEventId INTEGER,
                FOREIGN KEY (Affilation) REFERENCES Organizations(Id),
                FOREIGN KEY (LastUsedEventId) REFERENCES Event(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Event (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Title TEXT,
                Description TEXT,
                Date TEXT,
                Location TEXT,
                EventTypeId INTEGER,
                FOREIGN KEY (EventTypeId) REFERENCES EventType(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS EventType (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name VARCHAR,
                Description VARCHAR,
                ExpectedBiases VARCHAR
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS OrganizationTypes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT NOT NULL
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Organizations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT NOT NULL,
                TypeId INTEGER,
                Description TEXT,
                Credibility REAL CHECK (Credibility BETWEEN 1 AND 10),
                FOREIGN KEY (TypeId) REFERENCES OrganizationTypes(Id)
            );");

        Debug.Log("All tables created successfully!");
    }
}
