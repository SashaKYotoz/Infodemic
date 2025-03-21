using UnityEngine;
using SQLite4Unity3d;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager _instance;
    private SQLiteConnection _connection;

    public static DatabaseManager Instance => _instance;
    public SQLiteConnection Connection => _connection;
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        string dbPath = $"{Application.persistentDataPath}/infodemic.db";

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

    private void OnDestroy()
    {
        _connection?.Close();
        _connection?.Dispose();
        Debug.Log("Database connection closed.");
    }

    public void InsertSelectedWord(int eventId, int postId, string word)
    {
        try
        {
            _connection.Insert(new SelectedWords
            {
                EventId = eventId,
                PostId = postId,
                Word = word
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to add word: {e.Message}");
        }
    }

    public void SaveArticle(Articles newArticle)
    {
        _connection.Insert(newArticle);
        Debug.Log("Player's article generated successfully");
    }


    public void CreateWordFolder(WordFolders folder) => _connection.Insert(folder);

    public WordFolders GetFolderForEvent(int eventId) => _connection.Find<WordFolders>(eventId);

    public Characters GetCharacter(int charId)
    {
        return _connection.Find<Characters>(charId);
    }
    public Organizations GetOrganizations(int orgId)
    {
        return _connection.Find<Organizations>(orgId);
    }

    public List<string> GetFormattedCoreTruth(int eventId)
    {
        var currentEvent = GetEvent(eventId);
        string coreTruth = currentEvent.CoreTruth;
        return JsonKeyFormatter.GetFormattedKeysFromJson(coreTruth);
    }
    public Events GetEvent(int eventId)
    {
        return _connection.Find<Events>(eventId);
    }

    public Media GetMedia(int mediaId)
    {
        return _connection.Find<Media>(mediaId);
    }

    public float GetMediaReputation(int mediaId)
    {
        var media = _connection.Find<Media>(mediaId);
        if (media != null)
            return media.Credibility;
        else
            return 0f;
    }


    public void RemoveSelectedWord(int eventId, int postId, string word)
    {
        try
        {
            _connection.Execute(
                "DELETE FROM SelectedWords WHERE EventId = ? AND PostId = ? AND Word = ?",
                eventId, postId, word
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to remove word: {e.Message}");
        }
    }

    public List<SelectedWords> GetSelectedWordsForPost(int postId)
    {
        return _connection.Query<SelectedWords>(
                    "SELECT * FROM SelectedWords WHERE PostId = ?", postId
                ).ToList();
    }
    public List<SelectedWords> GetSelectedWordsForEvent(int eventId)
    {
        return _connection.Query<SelectedWords>(
            "SELECT * FROM SelectedWords WHERE EventId = ?", eventId
        ).ToList();
    }

    public List<Posts> GetPostsForCharacter(int characterId)
    {
        return _connection.Query<Posts>(
            "SELECT * FROM Posts WHERE CharacterId = ? AND CharacterId IS NOT NULL",
            characterId
        ).ToList();
    }
    public List<Posts> GetPostsForOrganization(int organizationId)
    {
        return _connection.Query<Posts>(
            "SELECT * FROM Posts WHERE OrganizationId = ? AND OrganizationId IS NOT NULL",
            organizationId
        ).ToList();
    }

    public List<WordFolders> GetFolders() => _connection.Table<WordFolders>().ToList();

    public List<Posts> GetPostsForEvent(int eventId)
    {
        return _connection.Query<Posts>(
            "SELECT * FROM Posts WHERE EventId = ?", eventId
        );
    }

    public List<int> GetParticipatingCharacters(int eventId)
    {
        return _connection.Table<Posts>()
                          .Where(p => p.EventId == eventId && p.CharacterId != null)
                          .Select(p => p.CharacterId.Value)
                          .Distinct()
                          .ToList();
    }

    public List<int> GetParticipatingOrganizations(int eventId)
    {
        return _connection.Table<Posts>()
                          .Where(p => p.EventId == eventId && p.OrganizationId != null)
                          .Select(p => p.OrganizationId.Value)
                          .Distinct()
                          .ToList();
    }


    public List<Characters> GetCharacterDetails(List<int> characterIds)
    {
        if (characterIds.Count == 0)
            return new List<Characters>();

        string idList = string.Join(",", characterIds);
        string query = $"SELECT * FROM Characters WHERE Id IN ({idList})";
        return _connection.Query<Characters>(query);
    }

    public List<Organizations> GetOrganizationDetails(List<int> organizationIds)
    {
        if (organizationIds.Count == 0)
            return new List<Organizations>();

        string idList = string.Join(",", organizationIds);
        string query = $"SELECT * FROM Organizations WHERE Id IN ({idList})";
        return _connection.Query<Organizations>(query);
    }


    public string GetOrganizationType(int typeId)
    {
        return _connection.Find<OrganizationTypes>(typeId)?.Name ?? "Unknown";
    }

    public string GetOrganizationName(int? orgId)
    {
        return orgId.HasValue ? _connection.Find<Organizations>(orgId.Value)?.Name : "Unknown Source";
    }
    public string GetCharacterName(int? charId)
    {
        return charId.HasValue ? _connection.Find<Characters>(charId.Value)?.Name : "Unknown Source";
    }

    public string GetCharacterBiases(int characterId)
    {
        return string.Join(", ", _connection.Query<Bias>(
            "SELECT b.Name FROM Biases b " +
            "JOIN CharacterBiases cb ON b.Id = cb.BiasId " +
            "WHERE cb.CharacterId = ?", characterId)
            .Select(b => b.Name));
    }
    public List<Organizations> GetRelevantOrganizations(int eventTypeId)
    {
        return _connection.Query<Organizations>(@"
        SELECT DISTINCT o.* FROM Organizations o
        JOIN OrganizationTags ot ON o.Id = ot.OrganizationId
        JOIN EventTypeTags ett ON ot.TagId = ett.TagId
        WHERE ett.EventTypeId = ?
        AND (o.LastUsedEventId IS NULL OR o.LastUsedEventId < datetime('now','-7 day'))
        LIMIT 3
    ", eventTypeId);
    }


    public List<Characters> GetRelevantCharacters(int eventTypeId)
    {
        return _connection.Query<Characters>(@"
        SELECT DISTINCT c.* FROM Characters c
        JOIN CharacterBiases cb ON c.Id = cb.CharacterId
        JOIN BiasTags bt ON cb.BiasId = bt.BiasId
        JOIN EventTypeTags ett ON bt.TagId = ett.TagId
        WHERE ett.EventTypeId = ?
        AND (c.LastUsedEventId IS NULL OR c.LastUsedEventId < datetime('now','-3 day'))
        LIMIT 6
    ", eventTypeId);
    }
    public string GetBiasContext(List<Characters> characters)
    {
        var context = new StringBuilder();
        foreach (var character in characters)
        {
            var biases = _connection.Query<Bias>(@"
            SELECT b.Name FROM Biases b
            JOIN CharacterBiases cb ON b.Id = cb.BiasId
            WHERE cb.CharacterId = ?
        ", character.Id);

            context.AppendLine($"{character.Name} tends to: {string.Join(", ", biases.Select(b => b.Name))}");
        }
        return context.ToString();
    }

    private void CreateTables()
    {


        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Tags (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT UNIQUE
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Media (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT,
                Description TEXT,
                Readers INTEGER,
                Credibility REAL CHECK(Credibility BETWEEN 1 AND 10)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Articles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                MediaId INTEGER NOT NULL,
                EventId INTEGER NOT NULL,
                Title TEXT,
                Content TEXT,
                VeracityScore REAL CHECK(VeracityScore BETWEEN 0 AND 10),
                Verdict TEXT,
                FOREIGN KEY (MediaId) REFERENCES Media(Id),
                FOREIGN KEY (EventId) REFERENCES Events(Id)
            );");
        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS WordFolders(
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                EventId INTEGER NOT NULL,
                FolderName TEXT,
                FolderDescription TEXT,
                FOREIGN KEY (EventId) REFERENCES Events(Id)
            )
        ");
        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS SelectedWords(
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                EventId INTEGER NOT NULL,
                PostId INTEGER NOT NULL,
                Word TEXT,
                PanelIndex INT DEFAULT -1,
                IsApproved BOOLEAN DEFAULT 0,
                FOREIGN KEY (EventId) REFERENCES Events(Id),
                FOREIGN KEY (PostId) REFERENCES Posts(Id)
            )
        ");
        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS WordPanels(
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT,
                EventId INT,
                FOREIGN KEY (EventId) REFERENCES Events(Id)
            )
        ");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Biases (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name VARCHAR
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS BiasTags (
                BiasId INTEGER,
                TagId INTEGER,
                FOREIGN KEY (BiasId) REFERENCES Biases(Id),
                FOREIGN KEY (TagId) REFERENCES Tags(Id)
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
                SocialMediaTag TEXT,
                SocialMediaDescription TEXT,
                Credibility REAL CHECK(Credibility BETWEEN 1 AND 10),
                Tier INTEGER,
                LastUsedEventId INTEGER,
                FOREIGN KEY (Affilation) REFERENCES Organizations(Id),
                FOREIGN KEY (LastUsedEventId) REFERENCES Events(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Events (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Title TEXT,
                Date TEXT,
                Description TEXT,
                GeneratedContent TEXT,
                CoreTruth TEXT,
                EventTypeId INTEGER,
                FOREIGN KEY (EventTypeId) REFERENCES EventType(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS EventType (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT,
                Description TEXT,
                LastUsedEventId INTEGER
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS EventTypeTags (
                EventTypeId INTEGER,
                TagId INTEGER,
                FOREIGN KEY (EventTypeId) REFERENCES EventType(Id),
                FOREIGN KEY (TagId) REFERENCES Tags(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Posts  (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                EventId INTEGER,
                CharacterId INTEGER,
                OrganizationId INTEGER,              
                Content TEXT,
                IsTruthful BOOLEAN,
                FOREIGN KEY (EventId) REFERENCES Events(Id)
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id),
                FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Organizations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT NOT NULL,
                TypeId INTEGER,
                Description TEXT,
                SocialMediaTag TEXT,
                SocialMediaDescription TEXT,
                Credibility REAL CHECK (Credibility BETWEEN 1 AND 10),
                LastUsedEventId INTEGER,
                FOREIGN KEY (TypeId) REFERENCES OrganizationTypes(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS OrganizationTags (
                OrganizationId INTEGER,
                TagId INTEGER,
                FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id),
                FOREIGN KEY (TagId) REFERENCES Tags(Id)
            );");

        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS OrganizationTypes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT NOT NULL
            );");

        Debug.Log("All tables created successfully!");

        InsertDefaultValues();
    }



    public List<Articles> GetArticlesByEventId(int eventId) => _connection.Query<Articles>("SELECT * From Articles WHERE EventId = ?", eventId).ToList();
    public void UpdateEntity<T>(T entity) => _connection.Update(entity);

    public void InsertSelectedWord(SelectedWords selectedWord)
    {
        _connection.Insert(selectedWord);
    }

    public void InsertEntity<T>(T entity) => _connection.Insert(entity);

    public List<WordPanels> GetWordPanelsForEvent(int eventId)
    {
        return _connection.Query<WordPanels>("SELECT * FROM WordPanels WHERE EventId = ?", eventId).ToList();
    }

    private void InsertDefaultValues()
    {

        // Insert default Biases
        var defaultBiases = new[] { "Pro-Environment", "Anti-Environment", "Pro-Corporate", "Anti-Corporate", "Pro-Government", "Anti-Government", "Pro-Science", "Anti-Vax", "Nationalist", "Globalist", "Conspiracy-Minded", "Populist", "Elitist", "Techno-Optimist", "Techno-Pessimist", "Libertarian", "Socialist", "Religious Fundamentalist" };

        foreach (var bias in defaultBiases)
        {
            if (_connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Biases WHERE Name = ?", bias) == 0)
            {
                _connection.Execute("INSERT INTO Biases (Name) VALUES (?)", bias);
            }
        }

        // Insert default Organization Types
        var defaultOrganizationTypes = new[] { "Corporation", "NGO", "Political Party", "Media", "Activist Group", "Religious Organization", "University/Research Institution", "Labor Union", "Tech Startup", "Charity/Foundation" };
        foreach (var type in defaultOrganizationTypes)
        {
            if (_connection.ExecuteScalar<int>("SELECT COUNT(*) FROM OrganizationTypes WHERE Name = ?", type) == 0)
            {
                _connection.Execute("INSERT INTO OrganizationTypes (Name) VALUES (?)", type);
            }
        }

        // Insert default organizations
        var defaultOrganizations = new[]
   {
        // Corporations
        new { Name = "Sentinel Dynamics", TypeId = 1, Description = "A defense contractor specializing in advanced weaponry.", Credibility = 6.8f, SocialMediaTag = "@SentinelDynamics", SocialMediaDescription = "Safeguarding nations through innovation in defense technology. Stay secure, stay advanced." },
        new { Name = "Solaris Innovations", TypeId = 1, Description = "A renewable energy corporation pushing clean energy tech.", Credibility = 8.4f, SocialMediaTag = "@SolarisInnovations", SocialMediaDescription = "Leading the charge for a cleaner planet with cutting-edge renewable energy solutions." },
        new { Name = "CoreX Minerals", TypeId = 1, Description = "A global leader in rare earth mineral extraction.", Credibility = 5.9f, SocialMediaTag = "@CoreXGlobal", SocialMediaDescription = "Providing essential minerals for a sustainable future. Essential resources for modern tech." },
        new { Name = "Summit Capital Group", TypeId = 1, Description = "A controversial private equity firm.", Credibility = 4.5f, SocialMediaTag = "@SummitCapital", SocialMediaDescription = "Shaping industries and transforming economies through strategic investments - your wealth, our expertise." },
    
        // NGOs 
        new { Name = "GreenPulse Movement", TypeId = 2, Description = "Focuses on reducing plastic waste and cleaning oceans.", Credibility = 7.5f, SocialMediaTag = "@GreenPulse", SocialMediaDescription = "Turning the tide against plastic pollution. Join us in saving our oceans." },
        new { Name = "Humanity's Voice Coalition", TypeId = 2, Description = "Defends human rights in the world.", Credibility = 7.8f, SocialMediaTag = "@HumanitysVoice", SocialMediaDescription = "Speaking up for human rights worldwide. Together, we amplify justice." },
        new { Name = "Unity Beyond Borders", TypeId = 2, Description = "Specializes in mediating peace treaties in conflict zones.", Credibility = 7.4f, SocialMediaTag = "@UnityBorders", SocialMediaDescription = "Fostering peace and understanding across nations. Building bridges, not walls." },


        // Political Parties
        new { Name = "Progressive Alliance", TypeId = 3, Description = "A forward-thinking party focused on climate reform.", Credibility = 7.2f, SocialMediaTag = "@ProgressiveAlliance", SocialMediaDescription = "Leading the way to a sustainable and inclusive future. Change begins now." },
        new { Name = "Liberty Front", TypeId = 3, Description = "A libertarian party promoting deregulation and free markets.", Credibility = 6.5f, SocialMediaTag = "@LibertyFront", SocialMediaDescription = "Advocating for freedom, minimal government, and maximum opportunity." },
        new { Name = "Social Unity", TypeId = 3, Description = "Populist party advocating for economic equality.", Credibility = 5.8f, SocialMediaTag = "@SocialUnity", SocialMediaDescription = "Fighting for equality and economic justice for all citizens." },

        // Media
        new { Name = "Daily Spectrum", TypeId = 4, Description = "A reputable media outlet known for investigative journalism.", Credibility = 8.7f, SocialMediaTag = "@DailySpectrum", SocialMediaDescription = "Uncovering the truth behind the headlines. Your source for investigative journalism." },
        new { Name = "Breaking Lens", TypeId = 4, Description = "A popular tabloid known for sensational headlines.", Credibility = 5.2f, SocialMediaTag = "@BreakingLens", SocialMediaDescription = "Your go-to source for sensational stories and breaking news." },
        new { Name = "Chronicle Insight", TypeId = 4, Description = "A digital-first platform for in-depth reporting.", Credibility = 7.6f, SocialMediaTag = "@ChronicleInsight", SocialMediaDescription = "Exploring the world through data-driven journalism. Stay informed, stay ahead." },
    
        // Activists Group
        new { Name = "Green Vanguard", TypeId = 5, Description = "A radical environmentalist group.", Credibility = 6.1f, SocialMediaTag = "@GreenVanguard", SocialMediaDescription = "Fighting for the planet's future. Radical action for a sustainable world." },
        new { Name = "Parity Now!", TypeId = 5, Description = "A movement for gender equality", Credibility = 7.9f, SocialMediaTag = "@ParityNow", SocialMediaDescription = " Radical change for a sustainable planet. Activism with impact."},
        new { Name = "Justice Impact", TypeId = 5, Description = "Campaigning against multinational corporate abuses.", Credibility = 7.2f, SocialMediaTag = "@JusticeImpact", SocialMediaDescription = "Fighting for justice against corporate greed. Your voice, our mission." },

        // Religious Organizations
        new { Name = "Order of the Silver Cross", TypeId = 6, Description = "A Catholic organization focused on humanitarian aid.", Credibility = 8.3f, SocialMediaTag = "@SilverCross",  SocialMediaDescription = "Faith in action—providing aid and hope where it's needed most." },
        new { Name = "The Crescent Fellowship", TypeId = 6, Description = "A Muslim organization promoting interfaith dialogue.", Credibility = 8.5f , SocialMediaTag = "@CrescentFellowship", SocialMediaDescription = "Promoting peace and interfaith dialogue across communities." },

        // Universities / Research Institutions 
        new { Name = "Vanguard Institute of Innovation", TypeId = 7, Description = "Pioneers advanced technologies in AI and quantum computing.", Credibility = 9.2f, SocialMediaTag = "@VanguardInnovate", SocialMediaDescription = "Pushing the boundaries of AI and quantum tech for a better tomorrow." },
        new { Name = "Arclight Research Center", TypeId = 7, Description = "Focuses on renewable energy breakthroughs and sustainable technologies.", Credibility = 8.8f, SocialMediaTag = "@ArclightResearch", SocialMediaDescription = " Illuminating paths to sustainable energy and ecological balance." },
        new { Name = "Helios Institute of Space Studies", TypeId = 7, Description = "A world leader in aerospace research and planetary science.", Credibility = 9.1f, SocialMediaTag = "@HeliosSpace", SocialMediaDescription = "Exploring the cosmos and unlocking the mysteries of the universe." },

        // Labor Unions
        new { Name = "United Workers", TypeId = 8, Description = "A labor union advocating for worker rights in manufacturing.", Credibility = 7.5f, SocialMediaTag = "@UnitedWorkers", SocialMediaDescription = "Defending the dignity and rights of workers everywhere." },
        new { Name = "Global Labor Federation", TypeId = 8, Description = "An international union focused on promoting fair labor practices globally.", Credibility = 8.5f, SocialMediaTag = "@GlobalLabor", SocialMediaDescription = "Uniting workers worldwide for fair wages and safe working conditions." },
        new { Name = "The Solidarity Movement", TypeId = 8, Description = "An activist labor union focused on workers' rights across multiple industries.", Credibility = 7.7f, SocialMediaTag = "@SolidarityMove", SocialMediaDescription = "Standing together for workers' rights and social justice." },

        // Tech Startups
        new { Name = "NeuraCore AI", TypeId = 9, Description = "A startup focused on advanced AI for medical diagnostics.", Credibility = 8.6f, SocialMediaTag = "@NeuraCore", SocialMediaDescription = "Revolutionizing healthcare with cutting-edge AI diagnostics." },
        new { Name = "SkyLink Systems", TypeId = 9, Description = "Building next-gen satellite internet solutions.", Credibility = 7.9f, SocialMediaTag = "@SkyLinkSystems", SocialMediaDescription = "Connecting the world with next-gen satellite internet solutions." },
        new { Name = "NimbusTech Innovations", TypeId = 9, Description = "A startup focused on creating AI-powered solutions for the healthcare industry.", Credibility = 8.3f , SocialMediaTag = "@NimbusTech", SocialMediaDescription = "Innovating healthcare with AI-powered solutions for better patient outcomes." },
        new { Name = "ByteForge Labs", TypeId = 9, Description = "Developing cutting-edge virtual reality experiences for education and entertainment.", Credibility = 8.7f, SocialMediaTag = "@ByteForgeLabs", SocialMediaDescription = "Immersive VR experiences that transform education and entertainment."},
        new { Name = "GreenSpire Technologies", TypeId = 9, Description = "A clean-tech startup focused on sustainable energy solutions for urban environments.", Credibility = 8.5f, SocialMediaTag = "@GreenSpireTech", SocialMediaDescription = "Empowering cities with sustainable energy solutions for a greener future." },

        // Charity/Foundation
        new { Name = "Lifeline Initiative", TypeId = 10, Description = "A charity focused on disaster relief and rebuilding efforts around the world.", Credibility = 9.0f, SocialMediaTag = "@LifelineInit", SocialMediaDescription = "Providing hope and support in times of crisis. Your lifeline to those in need." },
        new { Name = "Hands Together", TypeId = 10, Description = "Supports global initiatives for poverty alleviation and social equality.", Credibility = 8.3f, SocialMediaTag = "@HandsTogether", SocialMediaDescription = "Combating poverty and fostering equality across the globe.", },
        new { Name = "World Healing Foundation", TypeId = 10, Description = "Focuses on providing clean water and sanitation to communities in need.", Credibility = 8.7f, SocialMediaTag = "@WorldHealing", SocialMediaDescription = "Delivering clean water and sanitation to those in greatest need.", },
    };

        foreach (var org in defaultOrganizations)
        {
            if (_connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Organizations WHERE Name = ?", org.Name) == 0)
            {
                _connection.Execute(
                    "INSERT INTO Organizations (Name, TypeId, Description, Credibility, SocialMediaTag, SocialMediaDescription) VALUES (?, ?, ?, ?, ?, ?)",
                    org.Name,
                    org.TypeId,
                    org.Description,
                    Math.Round(org.Credibility, 1),
                    org.SocialMediaTag,
                    org.SocialMediaDescription
                );
            }
        }


        var defaultCharacters = new[]
{
    // Sentinel Dynamics (Pro-Corporate, Pro-Government, Nationalist)
    new {
        Name = "James Calloway",
        Profession = "Defense Consultant",
        SocialMediaTag = "@JamesCalloway",
        SocialMediaDescription = "Defense Consultant at @SentinelDynamics. Sharing insights on security.",
        Affilation = 1,
        Credibility = 7.2f,
        Tier = 2
    },
    new {
        Name = "Rebecca Monroe",
        Profession = "Military Strategist",
        SocialMediaTag = "@RebeccaMonroe",
        SocialMediaDescription = "Military Strategist at @SentinelDynamics. Analyzing tactics and strategy.",
        Affilation = 1,
        Credibility = 6.5f,
        Tier = 1
    },

    // Solaris Innovations (Pro-Environment, Techno-Optimist, Pro-Science)
    new {
        Name = "Ethan Caldwell",
        Profession = "Renewable Energy Scientist",
        SocialMediaTag = "@EthanCaldwell",
        SocialMediaDescription = "Renewable Energy Scientist at @SolarisInnovations. Passionate about green tech.",
        Affilation = 2,
        Credibility = 8.9f,
        Tier = 3
    },
    new {
        Name = "Nadia Foster",
        Profession = "Sustainability Consultant",
        SocialMediaTag = "@NadiaFoster",
        SocialMediaDescription = "Sustainability Consultant at @SolarisInnovations. Advocating for a greener future.",
        Affilation = 2,
        Credibility = 7.8f,
        Tier = 2
    },

    // CoreX Minerals (Pro-Corporate, Anti-Environment)
    new {
        Name = "Douglas Hayes",
        Profession = "Mining Executive",
        SocialMediaTag = "@DouglasHayes",
        SocialMediaDescription = "Mining Executive at @CoreXGlobal. Discussing industry trends.",
        Affilation = 3,
        Credibility = 6.3f,
        Tier = 2
    },
    new {
        Name = "Megan Russell",
        Profession = "Resource Extraction Analyst",
        SocialMediaTag = "@MeganRussell",
        SocialMediaDescription = "Resource Extraction Analyst at @CoreXGlobal. Examining market dynamics.",
        Affilation = 3,
        Credibility = 5.7f,
        Tier = 1
    },

    // Summit Capital Group (Elitist, Pro-Corporate, Anti-Government)
    new {
        Name = "Victor Langley",
        Profession = "Investment Tycoon",
        SocialMediaTag = "@VictorLangley",
        SocialMediaDescription = "Investment Tycoon at @SummitCapital. Sharing high finance insights.",
        Affilation = 4,
        Credibility = 4.1f,
        Tier = 3
    },
    new {
        Name = "Charlotte Brennan",
        Profession = "Economic Strategist",
        SocialMediaTag = "@CharlotteBrennan",
        SocialMediaDescription = "Economic Strategist at @SummitCapital. Breaking down market forces.",
        Affilation = 4,
        Credibility = 4.8f,
        Tier = 2
    },

    // GreenPulse Movement (Pro-Environment, Socialist)
    new {
        Name = "Cassandra Yu",
        Profession = "Climate Activist",
        SocialMediaTag = "@CassandraYu",
        SocialMediaDescription = "Climate Activist at @GreenPulse. Rallying for our planet.",
        Affilation = 5,
        Credibility = 8.3f,
        Tier = 2
    },
    new {
        Name = "Liam Hutchinson",
        Profession = "Wildlife Conservationist",
        SocialMediaTag = "@LiamHutchinson",
        SocialMediaDescription = "Wildlife Conservationist at @GreenPulse. Protecting nature daily.",
        Affilation = 5,
        Credibility = 7.6f,
        Tier = 2
    },

    // Humanity’s Voice Coalition (Pro-Government, Globalist)
    new {
        Name = "Amir Rahman",
        Profession = "Human Rights Lawyer",
        SocialMediaTag = "@AmirRahman",
        SocialMediaDescription = "Human Rights Lawyer at @HumanitysVoice. Advocating for justice.",
        Affilation = 6,
        Credibility = 8.2f,
        Tier = 3
    },
    new {
        Name = "Sophia Martinez",
        Profession = "NGO Coordinator",
        SocialMediaTag = "@SophiaMartinez",
        SocialMediaDescription = "NGO Coordinator at @HumanitysVoice. Pushing for global reform.",
        Affilation = 6,
        Credibility = 7.9f,
        Tier = 2
    },

    // Unity Beyond Borders (Mediating Peace Treaties)
    new {
        Name = "Jonathan Wells",
        Profession = "Conflict Resolution Expert",
        SocialMediaTag = "@JonathanWells",
        SocialMediaDescription = "Conflict Resolution Expert at @UnityBorders. Working for global peace.",
        Affilation = 7,
        Credibility = 8.2f,
        Tier = 2
    },
    new {
        Name = "Amina Rahman",
        Profession = "International Relations Specialist",
        SocialMediaTag = "@AminaRahman",
        SocialMediaDescription = "International Relations Specialist at @UnityBorders. Promoting global cooperation.",
        Affilation = 7,
        Credibility = 7.5f,
        Tier = 2
    },

    // Progressive Alliance (Climate Reform Party)
    new {
        Name = "Marissa Cohen",
        Profession = "Environmental Policy Advisor",
        SocialMediaTag = "@MarissaCohen",
        SocialMediaDescription = "Environmental Policy Advisor at @ProgressiveAlliance. Shaping climate policy.",
        Affilation = 8,
        Credibility = 7.8f,
        Tier = 2
    },
    new {
        Name = "Felix Grant",
        Profession = "Senator",
        SocialMediaTag = "@FelixGrant",
        SocialMediaDescription = "Senator at @ProgressiveAlliance. Balancing politics with green initiatives.",
        Affilation = 8,
        Credibility = 6.9f,
        Tier = 2
    },

    // Liberty Front (Libertarian, Anti-Government)
    new {
        Name = "Travis Beaumont",
        Profession = "Political Commentator",
        SocialMediaTag = "@TravisBeaumont",
        SocialMediaDescription = "Political Commentator at @LibertyFront. Offering a libertarian perspective.",
        Affilation = 9,
        Credibility = 6.4f,
        Tier = 1
    },
    new {
        Name = "Katie Hendricks",
        Profession = "Policy Analyst",
        SocialMediaTag = "@KatieHendricks",
        SocialMediaDescription = "Policy Analyst at @LibertyFront. Dissecting government policies daily.",
        Affilation = 9,
        Credibility = 6.8f,
        Tier = 2
    },

    // Social Unity (Populist Economic Party)
    new {
        Name = "Carlos Mendes",
        Profession = "Grassroots Organizer",
        SocialMediaTag = "@CarlosMendes",
        SocialMediaDescription = "Grassroots Organizer at @SocialUnity. Empowering the community one tweet at a time.",
        Affilation = 10,
        Credibility = 5.6f,
        Tier = 1
    },
    new {
        Name = "Eleanor Hayes",
        Profession = "Labor Economist",
        SocialMediaTag = "@EleanorHayes",
        SocialMediaDescription = "Labor Economist at @SocialUnity. Breaking down economic policies for workers.",
        Affilation = 10,
        Credibility = 6.4f,
        Tier = 2
    },

    // Daily Spectrum (Investigative Media)
    new {
        Name = "David Blackwood",
        Profession = "Investigative Journalist",
        SocialMediaTag = "@DavidBlackwood",
        SocialMediaDescription = "Investigative Journalist at @DailySpectrum. Uncovering hidden truths.",
        Affilation = 11,
        Credibility = 8.9f,
        Tier = 3
    },
    new {
        Name = "Samantha Ortega",
        Profession = "Political Correspondent",
        SocialMediaTag = "@SamanthaOrtega",
        SocialMediaDescription = "Political Correspondent at @DailySpectrum. Reporting the pulse of politics.",
        Affilation = 11,
        Credibility = 8.1f,
        Tier = 2
    },

    // Breaking Lens (Populist, Conspiracy-Minded)
    new {
        Name = "Derek Malone",
        Profession = "Investigative Blogger",
        SocialMediaTag = "@DerekMalone",
        SocialMediaDescription = "Investigative Blogger at @BreakingLens. Questioning the mainstream narrative.",
        Affilation = 12,
        Credibility = 5.1f,
        Tier = 1
    },
    new {
        Name = "Jessica Vaughn",
        Profession = "Freelance Journalist",
        SocialMediaTag = "@JessicaVaughn",
        SocialMediaDescription = "Freelance Journalist at @BreakingLens. Exposing controversies with a keen eye.",
        Affilation = 12,
        Credibility = 5.5f,
        Tier = 2
    },

    // Chronicle Insight (Digital-First Journalism)
    new {
        Name = "Raj Patel",
        Profession = "Digital Editor",
        SocialMediaTag = "@RajPatel",
        SocialMediaDescription = "Digital Editor at @ChronicleInsight. Curating stories from the digital realm.",
        Affilation = 13,
        Credibility = 7.4f,
        Tier = 2
    },
    new {
        Name = "Melissa Cho",
        Profession = "Data Journalist",
        SocialMediaTag = "@MelissaCho",
        SocialMediaDescription = "Data Journalist at @ChronicleInsight. Translating numbers into narratives.",
        Affilation = 13,
        Credibility = 7.9f,
        Tier = 2
    },

    // Green Vanguard (Pro-Environment, Techno-Pessimist)
    new {
        Name = "Elena Radcliffe",
        Profession = "Eco-Warrior",
        SocialMediaTag = "@ElenaRadcliffe",
        SocialMediaDescription = "Eco-Warrior at @GreenVanguard. Speaking out on environmental issues.",
        Affilation = 14,
        Credibility = 6.2f,
        Tier = 1
    },
    new {
        Name = "Benjamin Holt",
        Profession = "Climate Researcher",
        SocialMediaTag = "@BenjaminHolt",
        SocialMediaDescription = "Climate Researcher at @GreenVanguard. Investigating climate change impacts.",
        Affilation = 14,
        Credibility = 6.5f,
        Tier = 2
    },

    // Parity Now! (Gender Equality Movement)
    new {
        Name = "Zoe Sanders",
        Profession = "Women's Rights Advocate",
        SocialMediaTag = "@ZoeSanders",
        SocialMediaDescription = "Women's Rights Advocate at @ParityNow! Championing gender equality.",
        Affilation = 15,
        Credibility = 8.2f,
        Tier = 2
    },
    new {
        Name = "Maya Delgado",
        Profession = "Activist",
        SocialMediaTag = "@MayaDelgado",
        SocialMediaDescription = "Activist at @ParityNow! Speaking up for social justice.",
        Affilation = 15,
        Credibility = 7.6f,
        Tier = 2
    },

    // Justice Impact (Anti-Corporate Activism)
    new {
        Name = "Liam Novak",
        Profession = "Corporate Watchdog",
        SocialMediaTag = "@LiamNovak",
        SocialMediaDescription = "Corporate Watchdog at @JusticeImpact. Holding big business accountable.",
        Affilation = 16,
        Credibility = 7.0f,
        Tier = 2
    },
    new {
        Name = "Olivia Tran",
        Profession = "Legal Analyst",
        SocialMediaTag = "@OliviaTran",
        SocialMediaDescription = "Legal Analyst at @JusticeImpact. Breaking down corporate legal battles.",
        Affilation = 16,
        Credibility = 7.3f,
        Tier = 2
    },

    // Order of the Silver Cross (Religious Fundamentalist, Pro-Government)
    new {
        Name = "Father Michael Graves",
        Profession = "Catholic Bishop",
        SocialMediaTag = "@MichaelGraves",
        SocialMediaDescription = "Catholic Bishop at @SilverCross. Sharing faith & insights.",
        Affilation = 17,
        Credibility = 8.4f,
        Tier = 3
    },
    new {
        Name = "Sister Angela Turner",
        Profession = "Missionary",
        SocialMediaTag = "@AngelaTurner",
        SocialMediaDescription = "Missionary at @SilverCross. Spreading compassion and hope.",
        Affilation = 17,
        Credibility = 7.9f,
        Tier = 2
    },

    // The Crescent Fellowship (Interfaith Muslim Organization)
    new {
        Name = "Yusuf Al-Farid",
        Profession = "Interfaith Diplomat",
        SocialMediaTag = "@YusufAlFarid",
        SocialMediaDescription = "Interfaith Diplomat at @CrescentFellowship. Promoting unity through dialogue.",
        Affilation = 18,
        Credibility = 8.7f,
        Tier = 3
    },
    new {
        Name = "Fatima Hassan",
        Profession = "Community Organizer",
        SocialMediaTag = "@FatimaHassan",
        SocialMediaDescription = "Community Organizer at @CrescentFellowship. Amplifying voices for change.",
        Affilation = 18,
        Credibility = 8.3f,
        Tier = 2
    },

    // Vanguard Institute of Innovation (AI and Quantum Research)
    new {
        Name = "Elliot West",
        Profession = "AI Ethics Researcher",
        SocialMediaTag = "@ElliotWest",
        SocialMediaDescription = "AI Ethics Researcher at @VanguardInnovate. Examining the future of tech.",
        Affilation = 19,
        Credibility = 9.1f,
        Tier = 3
    },
    new {
        Name = "Dr. Susan Caldwell",
        Profession = "Quantum Computing Specialist",
        SocialMediaTag = "@SusanCaldwell",
        SocialMediaDescription = "Quantum Computing Specialist at @VanguardInnovate. Decoding quantum innovations.",
        Affilation = 19,
        Credibility = 9.3f,
        Tier = 3
    },

    // Arclight Research Center (Renewable Energy)
    new {
        Name = "Martin Lowe",
        Profession = "Renewable Energy Scientist",
        SocialMediaTag = "@MartinLowe",
        SocialMediaDescription = "Renewable Energy Scientist at @ArclightResearch. Pioneering sustainable energy solutions.",
        Affilation = 20,
        Credibility = 8.5f,
        Tier = 3
    },
    new {
        Name = "Elena Fischer",
        Profession = "Sustainable Tech Engineer",
        SocialMediaTag = "@ElenaFischer",
        SocialMediaDescription = "Sustainable Tech Engineer at @ArclightResearch. Innovating for a greener world.",
        Affilation = 20,
        Credibility = 8.7f,
        Tier = 3
    },

    // Helios Institute of Space Studies (Space Exploration)
    new {
        Name = "Dr. Alan Reyes",
        Profession = "Astrophysicist",
        SocialMediaTag = "@AlanReyes",
        SocialMediaDescription = "Astrophysicist at @HeliosSpace. Exploring space one discovery at a time.",
        Affilation = 21,
        Credibility = 9.0f,
        Tier = 3
    },
    new {
        Name = "Sophia Kim",
        Profession = "Aerospace Engineer",
        SocialMediaTag = "@SophiaKim",
        SocialMediaDescription = "Aerospace Engineer at @HeliosSpace. Designing the future of space travel.",
        Affilation = 21,
        Credibility = 8.9f,
        Tier = 3
    },

    // United Workers (Manufacturing Labor Union)
    new {
        Name = "Jack Mitchell",
        Profession = "Union Organizer",
        SocialMediaTag = "@JackMitchell",
        SocialMediaDescription = "Union Organizer at @UnitedWorkers. Fighting for worker rights.",
        Affilation = 22,
        Credibility = 7.6f,
        Tier = 2
    },
    new {
        Name = "Rebecca Lin",
        Profession = "Factory Worker Representative",
        SocialMediaTag = "@RebeccaLin",
        SocialMediaDescription = "Factory Worker Rep at @UnitedWorkers. Amplifying voices from the shop floor.",
        Affilation = 22,
        Credibility = 7.3f,
        Tier = 2
    },

    // Global Labor Federation (International Union)
    new {
        Name = "Hector Morales",
        Profession = "Union Representative",
        SocialMediaTag = "@HectorMorales",
        SocialMediaDescription = "Union Representative at @GlobalLabor. Advocating for fair labor practices.",
        Affilation = 23,
        Credibility = 8.1f,
        Tier = 3
    },
    new {
        Name = "Maria Vasquez",
        Profession = "Human Rights Advocate",
        SocialMediaTag = "@MariaVasquez",
        SocialMediaDescription = "Human Rights Advocate at @GlobalLabor. Speaking out for workers' rights.",
        Affilation = 23,
        Credibility = 8.4f,
        Tier = 3
    },

    // The Solidarity Movement (Activist Union)
    new {
        Name = "Jamal Edwards",
        Profession = "Strike Coordinator",
        SocialMediaTag = "@JamalEdwards",
        SocialMediaDescription = "Strike Coordinator at @SolidarityMove. Organizing for labor justice.",
        Affilation = 24,
        Credibility = 7.2f,
        Tier = 2
    },
    new {
        Name = "Hannah Weiss",
        Profession = "Labor Rights Journalist",
        SocialMediaTag = "@HannahWeiss",
        SocialMediaDescription = "Labor Rights Journalist at @SolidarityMove. Reporting on worker struggles.",
        Affilation = 24,
        Credibility = 7.8f,
        Tier = 2
    },

    // NeuraCore AI (Techno-Optimist, Pro-Science)
    new {
        Name = "Dr. Alan Voss",
        Profession = "AI Researcher",
        SocialMediaTag = "@AlanVoss",
        SocialMediaDescription = "AI Researcher at @NeuraCore. Exploring breakthroughs in artificial intelligence.",
        Affilation = 25,
        Credibility = 9.1f,
        Tier = 3
    },
    new {
        Name = "Samantha Lin",
        Profession = "Machine Learning Engineer",
        SocialMediaTag = "@SamanthaLin",
        SocialMediaDescription = "Machine Learning Engineer at @NeuraCore. Innovating with data and algorithms.",
        Affilation = 25,
        Credibility = 8.5f,
        Tier = 2
    },

    // SkyLink Systems (Satellite Internet Startup)
    new {
        Name = "Ethan Caldwell",
        Profession = "Satellite Engineer",
        SocialMediaTag = "@EthanCaldwell",
        SocialMediaDescription = "Satellite Engineer at @SkyLinkSystems. Connecting the world from above.",
        Affilation = 26,
        Credibility = 7.7f,
        Tier = 2
    },
    new {
        Name = "Nadia Blake",
        Profession = "Telecommunications Specialist",
        SocialMediaTag = "@NadiaBlake",
        SocialMediaDescription = "Telecommunications Specialist at @SkyLinkSystems. Ensuring global connectivity.",
        Affilation = 26,
        Credibility = 8.0f,
        Tier = 2
    },

    // NimbusTech Innovations (AI Healthcare Startup)
    new {
        Name = "Dr. Adrian Wells",
        Profession = "AI Medical Researcher",
        SocialMediaTag = "@AdrianWells",
        SocialMediaDescription = "AI Medical Researcher at @NimbusTech. Merging tech with healthcare.",
        Affilation = 27,
        Credibility = 8.4f,
        Tier = 3
    },
    new {
        Name = "Lisa Cooper",
        Profession = "Health Tech Developer",
        SocialMediaTag = "@LisaCooper",
        SocialMediaDescription = "Health Tech Developer at @NimbusTech. Building the future of medicine.",
        Affilation = 27,
        Credibility = 8.2f,
        Tier = 3
    },

    // ByteForge Labs (VR Development)
    new {
        Name = "Cody Ramirez",
        Profession = "VR Experience Designer",
        SocialMediaTag = "@CodyRamirez",
        SocialMediaDescription = "VR Experience Designer at @ByteForgeLabs. Crafting immersive digital adventures.",
        Affilation = 28,
        Credibility = 8.6f,
        Tier = 3
    },
    new {
        Name = "Tina Song",
        Profession = "Game Developer",
        SocialMediaTag = "@TinaSong",
        SocialMediaDescription = "Game Developer at @ByteForgeLabs. Blending creativity and technology.",
        Affilation = 28,
        Credibility = 8.3f,
        Tier = 3
    },
    
    // GreenSpire Technologies 
    new {
        Name = "Elena Vargas",
        Profession = "Sustainability Engineer",
        SocialMediaTag = "@ElenaVargas",
        SocialMediaDescription = "Sustainability Engineer at @GreenSpireTech. Designing clean energy solutions for a sustainable future.",
        Affilation = 29,
        Credibility = 8.2f,
        Tier = 3
    },

    new {
        Name = "Jared Mitchell",
        Profession = "Urban Planner",
        SocialMediaTag = "@JaredMitchell",
        SocialMediaDescription = "Urban Planner at @GreenSpireTech. Transforming cities with green technologies for a brighter tomorrow.",
        Affilation = 29,
        Credibility = 7.6f,
        Tier = 2
    },

    
    // Lifeline Initiative (Disaster Relief Charity)
    new {
        Name = "Daniel Harper",
        Profession = "Humanitarian Coordinator",
        SocialMediaTag = "@DanielHarper",
        SocialMediaDescription = "Humanitarian Coordinator at @LifelineInit. Organizing disaster relief with heart.",
        Affilation = 30,
        Credibility = 9.0f,
        Tier = 3
    },
    new {
        Name = "Olivia Bennett",
        Profession = "Relief Logistics Manager",
        SocialMediaTag = "@OliviaBennett",
        SocialMediaDescription = "Relief Logistics Manager at @LifelineInit. Ensuring aid gets to where it's needed.",
        Affilation = 30,
        Credibility = 8.8f,
        Tier = 3
    },

    // Hands Together - Poverty Alleviation and Social Equality
    new {
        Name = "Margaret Dawson",
        Profession = "Community Organizer",
        SocialMediaTag = "@MargaretDawson",
        SocialMediaDescription = "Community Organizer at @HandsTogether. Championing social equality.",
        Affilation = 31,
        Credibility = 8.4f,
        Tier = 2
    },
    new {
        Name = "Omar Sinclair",
        Profession = "Policy Advocate",
        SocialMediaTag = "@OmarSinclair",
        SocialMediaDescription = "Policy Advocate at @HandsTogether. Pushing for reforms that uplift communities.",
        Affilation = 31,
        Credibility = 8.2f,
        Tier = 2
    },

    // World Healing Foundation (Clean Water & Sanitation Efforts)
    new {
        Name = "Dr. Samuel Carter",
        Profession = "Water Sanitation Expert",
        SocialMediaTag = "@SamuelCarter",
        SocialMediaDescription = "Water Sanitation Expert at @WorldHealing. Bringing clean water to those in need.",
        Affilation = 32,
        Credibility = 8.8f,
        Tier = 1
    },
    new {
        Name = "Isabella Mendez",
        Profession = "Humanitarian Coordinator",
        SocialMediaTag = "@IsabellaMendez",
        SocialMediaDescription = "Humanitarian Coordinator at @WorldHealing. Fighting for accessible sanitation.",
        Affilation = 32,
        Credibility = 8.6f,
        Tier = 2
    }
};



        foreach (var character in defaultCharacters)
        {
            _connection.Execute(
                "INSERT INTO Characters (Name, Profession, SocialMediaTag, SocialMediaDescription, Affilation, Credibility, Tier) VALUES (?, ?, ?, ?, ?, ?, ?)",
                character.Name,
                character.Profession,
                character.SocialMediaTag,
                character.SocialMediaDescription,
                character.Affilation,
                Math.Round(character.Credibility, 1),
                character.Tier
            );
        }


        var defaultCharacterBiases = new[]
        {
            // Sentinel Dynamics
            new { CharacterId = 1, BiasId = 3 },  // James Calloway -> Pro-Corporate
            new { CharacterId = 1, BiasId = 5 },  // James Calloway -> Pro-Government
            new { CharacterId = 1, BiasId = 2 },  // James Calloway -> Anti-Environment

            new { CharacterId = 2, BiasId = 3 },  // Rebecca Monroe -> Pro-Corporate
            new { CharacterId = 2, BiasId = 9 },  // Rebecca Monroe -> Nationalist
            new { CharacterId = 2, BiasId = 2 },  // Rebecca Monroe -> Anti-Environment

            // Solaris Innovations
            new { CharacterId = 3, BiasId = 1 },  // Ethan Caldwell -> Pro-Environment
            new { CharacterId = 3, BiasId = 7 },  // Ethan Caldwell -> Pro-Science
            new { CharacterId = 4, BiasId = 1 },  // Nadia Foster -> Pro-Environment
            new { CharacterId = 4, BiasId = 10 }, // Nadia Foster -> Globalist

            // CoreX Minerals
            new { CharacterId = 5, BiasId = 3 },  // Douglas Hayes -> Pro-Corporate
            new { CharacterId = 5, BiasId = 2 },  // Douglas Hayes -> Anti-Environment
            new { CharacterId = 6, BiasId = 3 },  // Megan Russell -> Pro-Corporate
            new { CharacterId = 6, BiasId = 2 },  // Megan Russell -> Anti-Environment

            // Summit Capital Group
            new { CharacterId = 7, BiasId = 3 },  // Victor Langley -> Pro-Corporate
            new { CharacterId = 7, BiasId = 13 }, // Victor Langley -> Elitist
            new { CharacterId = 7, BiasId = 2 },  // Victor Langley -> Anti-Environment

            new { CharacterId = 8, BiasId = 5 },  // Charlotte Brennan -> Pro-Government
            new { CharacterId = 8, BiasId = 13 }, // Charlotte Brennan -> Elitist

            // GreenPulse Movement
            new { CharacterId = 9, BiasId = 1 },  // Cassandra Yu -> Pro-Environment
            new { CharacterId = 9, BiasId = 10 }, // Cassandra Yu -> Globalist
            new { CharacterId = 10, BiasId = 1 }, // Liam Hutchinson -> Pro-Environment
            new { CharacterId = 10, BiasId = 10 },// Liam Hutchinson -> Globalist

            // Humanity’s Voice Coalition
            new { CharacterId = 11, BiasId = 6 }, // Amir Rahman -> Anti-Government
            new { CharacterId = 11, BiasId = 10 },// Amir Rahman -> Globalist
            new { CharacterId = 12, BiasId = 10 },// Sophia Martinez -> Globalist
            new { CharacterId = 12, BiasId = 17 },// Sophia Martinez -> Socialist

            // Unity Beyond Borders
            new { CharacterId = 13, BiasId = 10 },// Jonathan Wells -> Globalist
            new { CharacterId = 13, BiasId = 7 }, // Jonathan Wells -> Pro-Science
            new { CharacterId = 14, BiasId = 10 },// Amina Rahman -> Globalist
            new { CharacterId = 14, BiasId = 5 }, // Amina Rahman -> Pro-Government

            // Progressive Alliance
            new { CharacterId = 15, BiasId = 1 }, // Marissa Cohen -> Pro-Environment
            new { CharacterId = 15, BiasId = 10 },// Marissa Cohen -> Globalist
            new { CharacterId = 16, BiasId = 5 }, // Felix Grant -> Pro-Government
            new { CharacterId = 16, BiasId = 17 },// Felix Grant -> Socialist

            // Liberty Front
            new { CharacterId = 17, BiasId = 16 },// Travis Beaumont -> Libertarian
            new { CharacterId = 17, BiasId = 12 },// Travis Beaumont -> Populist
            new { CharacterId = 17, BiasId = 2 }, // Travis Beaumont -> Anti-Environment

            new { CharacterId = 18, BiasId = 16 },// Katie Hendricks -> Libertarian
            new { CharacterId = 18, BiasId = 5 }, // Katie Hendricks -> Pro-Government

            // Social Unity
            new { CharacterId = 19, BiasId = 10 },// Carlos Mendes -> Globalist
            new { CharacterId = 19, BiasId = 17 },// Carlos Mendes -> Socialist
            new { CharacterId = 19, BiasId = 4 }, // Carlos Mendes -> Anti-Corporate

            new { CharacterId = 20, BiasId = 10 },// Eleanor Hayes -> Globalist
            new { CharacterId = 20, BiasId = 17 },// Eleanor Hayes -> Socialist
            new { CharacterId = 20, BiasId = 4 }, // Eleanor Hayes -> Anti-Corporate

            // Daily Spectrum
            new { CharacterId = 21, BiasId = 10 },// David Blackwood -> Globalist
            new { CharacterId = 21, BiasId = 7 }, // David Blackwood -> Pro-Science
            new { CharacterId = 22, BiasId = 10 },// Samantha Ortega -> Globalist
            new { CharacterId = 22, BiasId = 7 }, // Samantha Ortega -> Pro-Science

            // Breaking Lens
            new { CharacterId = 23, BiasId = 11 },// Derek Malone -> Conspiracy-Minded
            new { CharacterId = 23, BiasId = 12 },// Derek Malone -> Populist
            new { CharacterId = 24, BiasId = 11 },// Jessica Vaughn -> Conspiracy-Minded
            new { CharacterId = 24, BiasId = 12 },// Jessica Vaughn -> Populist

            // Chronicle Insight
            new { CharacterId = 25, BiasId = 7 }, // Raj Patel -> Pro-Science
            new { CharacterId = 25, BiasId = 10 },// Raj Patel -> Globalist
            new { CharacterId = 26, BiasId = 7 }, // Melissa Cho -> Pro-Science
            new { CharacterId = 26, BiasId = 10 },// Melissa Cho -> Globalist

            // Green Vanguard
            new { CharacterId = 27, BiasId = 1 }, // Elena Radcliffe -> Pro-Environment
            new { CharacterId = 27, BiasId = 6 }, // Elena Radcliffe -> Anti-Government
            new { CharacterId = 27, BiasId = 4 }, // Elena Radcliffe -> Anti-Corporate

            new { CharacterId = 28, BiasId = 1 }, // Benjamin Holt -> Pro-Environment
            new { CharacterId = 28, BiasId = 7 }, // Benjamin Holt -> Pro-Science

            // Parity Now!
            new { CharacterId = 29, BiasId = 15 },// Zoe Sanders -> Techno-Pessimist
            new { CharacterId = 29, BiasId = 10 },// Zoe Sanders -> Globalist
            new { CharacterId = 30, BiasId = 10 },// Maya Delgado -> Globalist
            new { CharacterId = 30, BiasId = 17 },// Maya Delgado -> Socialist

            // Justice Impact
            new { CharacterId = 31, BiasId = 10 },// Liam Novak -> Globalist
            new { CharacterId = 31, BiasId = 17 },// Liam Novak -> Socialist
            new { CharacterId = 32, BiasId = 5 }, // Olivia Tran -> Pro-Government
            new { CharacterId = 32, BiasId = 7 }, // Olivia Tran -> Pro-Science

            // Order of the Silver Cross
            new { CharacterId = 33, BiasId = 18 },// Father Michael Graves -> Religious Fundamentalist
            new { CharacterId = 33, BiasId = 5 }, // Father Michael Graves -> Pro-Government
            new { CharacterId = 34, BiasId = 18 },// Sister Angela Turner -> Religious Fundamentalist
            new { CharacterId = 34, BiasId = 10 },// Sister Angela Turner -> Globalist

            // The Crescent Fellowship
            new { CharacterId = 35, BiasId = 10 },// Yusuf Al-Farid -> Globalist
            new { CharacterId = 35, BiasId = 18 },// Yusuf Al-Farid -> Religious Fundamentalist
            new { CharacterId = 36, BiasId = 17 },// Fatima Hassan -> Socialist
            new { CharacterId = 36, BiasId = 10 },// Fatima Hassan -> Globalist

            // Vanguard Institute of Innovation
            new { CharacterId = 37, BiasId = 7 }, // Elliot West -> Pro-Science
            new { CharacterId = 37, BiasId = 14 },// Elliot West -> Techno-Optimist
            new { CharacterId = 38, BiasId = 7 }, // Dr. Susan Caldwell -> Pro-Science
            new { CharacterId = 38, BiasId = 14 },// Dr. Susan Caldwell -> Techno-Optimist

            // Arclight Research Center
            new { CharacterId = 39, BiasId = 1 }, // Martin Lowe -> Pro-Environment
            new { CharacterId = 39, BiasId = 7 }, // Martin Lowe -> Pro-Science
            new { CharacterId = 40, BiasId = 1 }, // Elena Fischer -> Pro-Environment
            new { CharacterId = 40, BiasId = 14 },// Elena Fischer -> Techno-Optimist

            // Helios Institute of Space Studies
            new { CharacterId = 41, BiasId = 7 }, // Dr. Alan Reyes -> Pro-Science
            new { CharacterId = 41, BiasId = 14 },// Dr. Alan Reyes -> Techno-Optimist
            new { CharacterId = 42, BiasId = 7 }, // Sophia Kim -> Pro-Science
            new { CharacterId = 42, BiasId = 14 },// Sophia Kim -> Techno-Optimist

            // United Workers
            new { CharacterId = 43, BiasId = 17 },// Jack Mitchell -> Socialist
            new { CharacterId = 43, BiasId = 10 },// Jack Mitchell -> Globalist
            new { CharacterId = 43, BiasId = 4 }, // Jack Mitchell -> Anti-Corporate

            new { CharacterId = 44, BiasId = 17 },// Rebecca Lin -> Socialist
            new { CharacterId = 44, BiasId = 12 },// Rebecca Lin -> Populist
            new { CharacterId = 44, BiasId = 4 }, // Rebecca Lin -> Anti-Corporate

            // Global Labor Federation
            new { CharacterId = 45, BiasId = 17 },// Hector Morales -> Socialist
            new { CharacterId = 45, BiasId = 10 },// Hector Morales -> Globalist
            new { CharacterId = 45, BiasId = 4 }, // Hector Morales -> Anti-Corporate

            new { CharacterId = 46, BiasId = 17 },// Maria Vasquez -> Socialist
            new { CharacterId = 46, BiasId = 10 },// Maria Vasquez -> Globalist
            new { CharacterId = 46, BiasId = 4 }, // Maria Vasquez -> Anti-Corporate

            // The Solidarity Movement
            new { CharacterId = 47, BiasId = 17 },// Jamal Edwards -> Socialist
            new { CharacterId = 47, BiasId = 10 },// Jamal Edwards -> Globalist
            new { CharacterId = 47, BiasId = 4 }, // Jamal Edwards -> Anti-Corporate

            new { CharacterId = 48, BiasId = 17 },// Hannah Weiss -> Socialist
            new { CharacterId = 48, BiasId = 10 },// Hannah Weiss -> Globalist
            new { CharacterId = 48, BiasId = 4 }, // Hannah Weiss -> Anti-Corporate

            // NeuraCore AI
            new { CharacterId = 49, BiasId = 7 }, // Dr. Alan Voss -> Pro-Science
            new { CharacterId = 49, BiasId = 14 },// Dr. Alan Voss -> Techno-Optimist
            new { CharacterId = 50, BiasId = 7 }, // Samantha Lin -> Pro-Science
            new { CharacterId = 50, BiasId = 14 },// Samantha Lin -> Techno-Optimist

            // SkyLink Systems
            new { CharacterId = 51, BiasId = 7 }, // Ethan Caldwell -> Pro-Science
            new { CharacterId = 51, BiasId = 14 },// Ethan Caldwell -> Techno-Optimist
            new { CharacterId = 52, BiasId = 7 }, // Nadia Blake -> Pro-Science
            new { CharacterId = 52, BiasId = 14 },// Nadia Blake -> Techno-Optimist

            // NimbusTech Innovations
            new { CharacterId = 53, BiasId = 7 }, // Dr. Adrian Wells -> Pro-Science
            new { CharacterId = 53, BiasId = 14 },// Dr. Adrian Wells -> Techno-Optimist
            new { CharacterId = 54, BiasId = 7 }, // Lisa Cooper -> Pro-Science
            new { CharacterId = 54, BiasId = 14 },// Lisa Cooper -> Techno-Optimist

            // ByteForge Labs
            new { CharacterId = 55, BiasId = 7 }, // Cody Ramirez -> Pro-Science
            new { CharacterId = 55, BiasId = 14 },// Cody Ramirez -> Techno-Optimist
            new { CharacterId = 56, BiasId = 7 }, // Tina Song -> Pro-Science
            new { CharacterId = 56, BiasId = 14 },// Tina Song -> Techno-Optimist

            // GreenSpire Technologies
            new { CharacterId = 57, BiasId = 1 }, // Elena Vargas -> Pro-Environment
            new { CharacterId = 57, BiasId = 7 }, // Elena Vargas -> Pro-Science
            new { CharacterId = 57, BiasId = 10 },// Elena Vargas -> Globalist
            new { CharacterId = 58, BiasId = 1 }, // Jared Mitchell -> Pro-Environment
            new { CharacterId = 58, BiasId = 7 }, // Jared Mitchell -> Pro-Science

            // Lifeline Initiative
            new { CharacterId = 59, BiasId = 10 },// Daniel Harper -> Globalist
            new { CharacterId = 59, BiasId = 17 },// Daniel Harper -> Socialist
            new { CharacterId = 60, BiasId = 10 },// Olivia Bennett -> Globalist
            new { CharacterId = 60, BiasId = 17 },// Olivia Bennett -> Socialist

            // Hands Together
            new { CharacterId = 61, BiasId = 10 },// Margaret Dawson -> Globalist
            new { CharacterId = 61, BiasId = 17 },// Margaret Dawson -> Socialist
            new { CharacterId = 62, BiasId = 10 },// Omar Sinclair -> Globalist
            new { CharacterId = 62, BiasId = 17 },// Omar Sinclair -> Socialist

            // World Healing Foundation
            new { CharacterId = 63, BiasId = 10 },// Dr. Samuel Carter -> Globalist
            new { CharacterId = 63, BiasId = 7 }, // Dr. Samuel Carter -> Pro-Science
            new { CharacterId = 64, BiasId = 10 },// Isabella Mendez -> Globalist
            new { CharacterId = 64, BiasId = 17 } // Isabella Mendez -> Socialist
        };


        foreach (var cb in defaultCharacterBiases)
        {
            _connection.Execute("INSERT INTO CharacterBiases (CharacterId, BiasId) VALUES (?, ?)", cb.CharacterId, cb.BiasId);
        }



        var defaultTags = new[]
        {
            "#environment",
            "#corporate",
            "#government",
            "#health",
            "#technology",
            "#corruption",
            "#activism",
            "#disaster",
            "#economy",
            "#education",
            "#media",
            "#science",
            "#crime",
            "#religion",
            "#military",
            "#culture",
            "#energy",
            "#agriculture",
            "#transportation",
            "#space"

            };
        foreach (var tag in defaultTags)
        {
            _connection.Execute("INSERT INTO Tags (Name) VALUES (?)", tag);
        }

        var defaultOrganizationTags = new[]
        {
            // Sentinel Dynamics (ID: 1)
            new { OrganizationId = 1, TagId = 15 }, // #military
            new { OrganizationId = 1, TagId = 5 },  // #technology

            // Solaris Innovations (ID: 2)
            new { OrganizationId = 2, TagId = 17 }, // #energy
            new { OrganizationId = 2, TagId = 1 },  // #environment
            new { OrganizationId = 2, TagId = 5 },  // #technology

            // CoreX Minerals (ID: 3)
            new { OrganizationId = 3, TagId = 2 },  // #corporate
            new { OrganizationId = 3, TagId = 17 }, // #energy
            new { OrganizationId = 3, TagId = 1 },  // #environment

            // Summit Capital Group (ID: 4)
            new { OrganizationId = 4, TagId = 2 },  // #corporate
            new { OrganizationId = 4, TagId = 9 },  // #economy
            new { OrganizationId = 4, TagId = 6 },  // #corruption

            // GreenPulse Movement (ID: 5)
            new { OrganizationId = 5, TagId = 1 },  // #environment
            new { OrganizationId = 5, TagId = 7 },  // #activism

            // Humanity's Voice Coalition (ID: 6)
            new { OrganizationId = 6, TagId = 7 },  // #activism
            new { OrganizationId = 6, TagId = 3 },  // #government
            new { OrganizationId = 6, TagId = 4 },  // #health

            // Unity Beyond Borders (ID: 7)
            new { OrganizationId = 7, TagId = 7 },  // #activism
            new { OrganizationId = 7, TagId = 3 },  // #government
            new { OrganizationId = 7, TagId = 14 }, // #religion

            // Progressive Alliance (ID: 8)
            new { OrganizationId = 8, TagId = 3 },  // #government
            new { OrganizationId = 8, TagId = 1 },  // #environment
            new { OrganizationId = 8, TagId = 7 },  // #activism

            // Liberty Front (ID: 9)
            new { OrganizationId = 9, TagId = 3 },  // #government
            new { OrganizationId = 9, TagId = 9 },  // #economy
            new { OrganizationId = 9, TagId = 16 }, // #libertarian

            // Social Unity (ID: 10)
            new { OrganizationId = 10, TagId = 3 }, // #government
            new { OrganizationId = 10, TagId = 9 }, // #economy
            new { OrganizationId = 10, TagId = 12 }, // #populist

            // Daily Spectrum (ID: 11)
            new { OrganizationId = 11, TagId = 11 }, // #media
            new { OrganizationId = 11, TagId = 10 }, // #education
            new { OrganizationId = 11, TagId = 12 }, // #science

            // Breaking Lens (ID: 12)
            new { OrganizationId = 12, TagId = 11 }, // #media
            new { OrganizationId = 12, TagId = 6 },  // #corruption
            new { OrganizationId = 12, TagId = 13 }, // #sensationalism

            // Chronicle Insight (ID: 13)
            new { OrganizationId = 13, TagId = 11 }, // #media
            new { OrganizationId = 13, TagId = 12 }, // #science
            new { OrganizationId = 13, TagId = 5 },  // #technology

            // Green Vanguard (ID: 14)
            new { OrganizationId = 14, TagId = 1 },  // #environment
            new { OrganizationId = 14, TagId = 7 },  // #activism
            new { OrganizationId = 14, TagId = 16 }, // #radical

            // Parity Now! (ID: 15)
            new { OrganizationId = 15, TagId = 7 },  // #activism
            new { OrganizationId = 15, TagId = 16 }, // #culture
            new { OrganizationId = 15, TagId = 14 }, // #equality

            // Justice Impact (ID: 16)
            new { OrganizationId = 16, TagId = 7 },  // #activism
            new { OrganizationId = 16, TagId = 6 },  // #corruption
            new { OrganizationId = 16, TagId = 2 },  // #corporate

            // Order of the Silver Cross (ID: 17)
            new { OrganizationId = 17, TagId = 14 }, // #religion
            new { OrganizationId = 17, TagId = 4 },  // #health
            new { OrganizationId = 17, TagId = 7 },  // #activism

            // The Crescent Fellowship (ID: 18)
            new { OrganizationId = 18, TagId = 14 }, // #religion
            new { OrganizationId = 18, TagId = 16 }, // #culture
            new { OrganizationId = 18, TagId = 7 },  // #activism

            // Vanguard Institute of Innovation (ID: 19)
            new { OrganizationId = 19, TagId = 5 },  // #technology
            new { OrganizationId = 19, TagId = 12 }, // #science
            new { OrganizationId = 19, TagId = 17 }, // #innovation

            // Arclight Research Center (ID: 20)
            new { OrganizationId = 20, TagId = 5 },  // #technology
            new { OrganizationId = 20, TagId = 17 }, // #energy
            new { OrganizationId = 20, TagId = 1 },  // #environment

            // Helios Institute of Space Studies (ID: 21)
            new { OrganizationId = 21, TagId = 5 },  // #technology
            new { OrganizationId = 21, TagId = 20 }, // #space
            new { OrganizationId = 21, TagId = 12 }, // #science

            // United Workers (ID: 22)
            new { OrganizationId = 22, TagId = 9 },  // #economy
            new { OrganizationId = 22, TagId = 7 },  // #activism
            new { OrganizationId = 22, TagId = 8 },  // #labor

            // Global Labor Federation (ID: 23)
            new { OrganizationId = 23, TagId = 9 },  // #economy
            new { OrganizationId = 23, TagId = 7 },  // #activism
            new { OrganizationId = 23, TagId = 18 }, // #global

            // The Solidarity Movement (ID: 24)
            new { OrganizationId = 24, TagId = 9 },  // #economy
            new { OrganizationId = 24, TagId = 7 },  // #activism
            new { OrganizationId = 24, TagId = 8 },  // #labor

            // NeuraCore AI (ID: 25)
            new { OrganizationId = 25, TagId = 5 },  // #technology
            new { OrganizationId = 25, TagId = 4 },  // #health
            new { OrganizationId = 25, TagId = 17 }, // #innovation

            // SkyLink Systems (ID: 26)
            new { OrganizationId = 26, TagId = 5 },  // #technology
            new { OrganizationId = 26, TagId = 20 }, // #space
            new { OrganizationId = 26, TagId = 19 }, // #communication

            // NimbusTech Innovations (ID: 27)
            new { OrganizationId = 27, TagId = 5 },  // #technology
            new { OrganizationId = 27, TagId = 4 },  // #health
            new { OrganizationId = 27, TagId = 17 }, // #innovation

            // ByteForge Labs (ID: 28)
            new { OrganizationId = 28, TagId = 5 },  // #technology
            new { OrganizationId = 28, TagId = 10 }, // #education
            new { OrganizationId = 28, TagId = 16 }, // #entertainment

            // GreenSpire Technologies (ID: 29)
            new { OrganizationId = 29, TagId = 5 },  // #technology
            new { OrganizationId = 29, TagId = 1 },  // #environment
            new { OrganizationId = 29, TagId = 17 }, // #energy

            // Lifeline Initiative (ID: 30)
            new { OrganizationId = 30, TagId = 4 },  // #health
            new { OrganizationId = 30, TagId = 7 },  // #activism
            new { OrganizationId = 30, TagId = 8 },  // #disaster

            // Hands Together (ID: 31)
            new { OrganizationId = 31, TagId = 4 },  // #health
            new { OrganizationId = 31, TagId = 7 },  // #activism
            new { OrganizationId = 31, TagId = 18 }, // #global

            // World Healing Foundation (ID: 32)
            new { OrganizationId = 32, TagId = 4 },  // #health
            new { OrganizationId = 32, TagId = 1 },  // #environment
            new { OrganizationId = 32, TagId = 7 }   // #activism
        };

        foreach (var organizationTag in defaultOrganizationTags)
        {
            _connection.Execute("INSERT INTO OrganizationTags (OrganizationId, TagId) VALUES (?, ?)", organizationTag.OrganizationId, organizationTag.TagId);
        }


        var defaultBiasTags = new[] {
            // Pro-Environment
            new { BiasId = 1, TagId = 1 },  // #environment
            new { BiasId = 1, TagId = 7 },  // #activism

            // Anti-Environment
            new { BiasId = 2, TagId = 2 },  // #corporate
            new { BiasId = 2, TagId = 17 }, // #energy

            // Pro-Corporate
            new { BiasId = 3, TagId = 2 },  // #corporate
            new { BiasId = 3, TagId = 9 },  // #economy

            // Anti-Corporate
            new { BiasId = 4, TagId = 7 },  // #activism
            new { BiasId = 4, TagId = 6 },  // #corruption

            // Pro-Government
            new { BiasId = 5, TagId = 3 },  // #government
            new { BiasId = 5, TagId = 12 }, // #policy

            // Anti-Government
            new { BiasId = 6, TagId = 7 },  // #activism
            new { BiasId = 6, TagId = 6 },  // #corruption

            // Pro-Science
            new { BiasId = 7, TagId = 12 }, // #science
            new { BiasId = 7, TagId = 5 },  // #technology

            // Anti-Vax
            new { BiasId = 8, TagId = 4 },  // #health
            new { BiasId = 8, TagId = 13 }, // #conspiracy

            // Nationalist
            new { BiasId = 9, TagId = 3 },  // #government
            new { BiasId = 9, TagId = 16 }, // #culture

            // Globalist
            new { BiasId = 10, TagId = 18 }, // #global
            new { BiasId = 10, TagId = 9 },  // #economy

            // Conspiracy-Minded
            new { BiasId = 11, TagId = 13 }, // #conspiracy
            new { BiasId = 11, TagId = 11 }, // #media

            // Populist
            new { BiasId = 12, TagId = 3 },  // #government
            new { BiasId = 12, TagId = 9 },  // #economy

            // Elitist
            new { BiasId = 13, TagId = 9 },  // #economy
            new { BiasId = 13, TagId = 2 },  // #corporate

            // Techno-Optimist
            new { BiasId = 14, TagId = 5 },  // #technology
            new { BiasId = 14, TagId = 17 }, // #innovation

            // Techno-Pessimist
            new { BiasId = 15, TagId = 5 },  // #technology
            new { BiasId = 15, TagId = 8 },  // #disaster

            // Libertarian
            new { BiasId = 16, TagId = 3 },  // #government
            new { BiasId = 16, TagId = 9 },  // #economy

            // Socialist
            new { BiasId = 17, TagId = 9 },  // #economy
            new { BiasId = 17, TagId = 7 },  // #activism

            // Religious Fundamentalist
            new { BiasId = 18, TagId = 14 }, // #religion
            new { BiasId = 18, TagId = 16 }  // #culture
        };

        foreach (var biasTag in defaultBiasTags)
        {
            _connection.Execute("INSERT INTO BiasTags (BiasId, TagId) VALUES (?, ?)", biasTag.BiasId, biasTag.TagId);
        }


        // Event Types
        var defaultEventTypes = new[] {
            
            // Positive Events
            new { Name = "Breakthrough in Medicine", Description = "A major advancement in medical science or healthcare" },
            new { Name = "Renewable Energy Innovation", Description = "A new breakthrough in clean and renewable energy" },
            new { Name = "Scientific Discovery", Description = "A groundbreaking discovery in science or technology" },
            new { Name = "Economic Growth", Description = "A region or country experiences significant economic improvement" },
            new { Name = "Successful Space Mission", Description = "A milestone achievement in space exploration" },
            new { Name = "Landmark Environmental Policy", Description = "A government or corporation adopts a major eco-friendly initiative" },
            new { Name = "Tech Innovation", Description = "A new technology that positively impacts society is unveiled" },
            new { Name = "Cure for Disease", Description = "A major illness or condition gets an effective cure or treatment" },
            new { Name = "Major Charity Success", Description = "A large humanitarian or social justice initiative achieves its goal" },
            
            // Neutral / Mixed Events
            new { Name = "Global Summit on AI Ethics", Description = "World leaders and experts discuss responsible AI development" },
            new { Name = "New Infrastructure Project", Description = "A large-scale public or private construction project is announced" },
            new { Name = "New Space Exploration Initiative", Description = "A country or private company announces plans for space research" },
            
            // Negative Events
            new { Name = "Environmental Disaster", Description = "Ecological harm caused by human activity or natural causes" },
            new { Name = "Political Scandal", Description = "Corruption or misconduct in government institutions" },
            new { Name = "Health Crisis", Description = "Public health emergencies or disease outbreaks" },
            new { Name = "Corporate Fraud", Description = "Financial misconduct or unethical business practices" },
            new { Name = "Tech Controversy", Description = "Controversial use or failure of advanced technology" },
            new { Name = "Labor Dispute", Description = "Worker strikes or labor rights violations" },
            new { Name = "Conspiracy Theory", Description = "Emergence of widespread unverified claims" }
        };


        foreach (var eventType in defaultEventTypes)
        {
            _connection.Execute(
                @"INSERT INTO EventType (Name, Description) 
                VALUES (?, ?)",
                eventType.Name,
                eventType.Description
            );
        }

        var eventTypeTags = new[]
         {
            // Positive Events
            new { EventTypeId = 1, TagId = 12 }, // #science - Breakthrough in Medicine
            new { EventTypeId = 1, TagId = 4 },  // #health

            new { EventTypeId = 2, TagId = 1 },  // #environment - Renewable Energy Innovation
            new { EventTypeId = 2, TagId = 17 }, // #energy

            new { EventTypeId = 3, TagId = 12 }, // #science - Scientific Discovery
            new { EventTypeId = 3, TagId = 10 }, // #education

            new { EventTypeId = 4, TagId = 9 },  // #economy - Economic Growth
            new { EventTypeId = 4, TagId = 3 },  // #government

            new { EventTypeId = 5, TagId = 20 }, // #space - Successful Space Mission
            new { EventTypeId = 5, TagId = 12 }, // #science

            new { EventTypeId = 6, TagId = 1 },  // #environment - Landmark Environmental Policy
            new { EventTypeId = 6, TagId = 3 },  // #government

            new { EventTypeId = 7, TagId = 5 },  // #technology - Tech Innovation
            new { EventTypeId = 7, TagId = 12 }, // #science

            new { EventTypeId = 8, TagId = 4 },  // #health - Cure for Disease
            new { EventTypeId = 8, TagId = 12 }, // #science

            new { EventTypeId = 9, TagId = 7 },  // #activism - Major Charity Success
            new { EventTypeId = 9, TagId = 11 }, // #media

            // Neutral / Mixed Events
            new { EventTypeId = 10, TagId = 5 },  // #technology - Global Summit on AI Ethics
            new { EventTypeId = 10, TagId = 13 }, // #crime

            new { EventTypeId = 11, TagId = 9 },  // #economy - New Infrastructure Project
            new { EventTypeId = 11, TagId = 19 }, // #transportation

            new { EventTypeId = 12, TagId = 20 }, // #space - New Space Exploration Initiative
            new { EventTypeId = 12, TagId = 12 }, // #science

            // Negative Events
            new { EventTypeId = 13, TagId = 1 },  // #environment - Environmental Disaster
            new { EventTypeId = 13, TagId = 8 },  // #disaster

            new { EventTypeId = 14, TagId = 3 },  // #government - Political Scandal
            new { EventTypeId = 14, TagId = 6 },  // #corruption

            new { EventTypeId = 15, TagId = 4 },  // #health - Health Crisis
            new { EventTypeId = 15, TagId = 12 }, // #science

            new { EventTypeId = 16, TagId = 2 },  // #corporate - Corporate Fraud
            new { EventTypeId = 16, TagId = 9 },  // #economy

            new { EventTypeId = 17, TagId = 5 },  // #technology - Tech Controversy
            new { EventTypeId = 17, TagId = 13 }, // #crime

            new { EventTypeId = 18, TagId = 7 },  // #activism - Labor Dispute
            new { EventTypeId = 18, TagId = 9 },  // #economy

            new { EventTypeId = 19, TagId = 11 }, // #media - Conspiracy Theory
            new { EventTypeId = 19, TagId = 14 }  // #religion
        };

        foreach (var eventTypeTag in eventTypeTags)
        {
            _connection.Execute(
                @"INSERT INTO EventTypeTags (EventTypeId, TagId) VALUES (?, ?)",
                eventTypeTag.EventTypeId,
                eventTypeTag.TagId
            );
        }


        var defaultMedia = new[] {
            new { Name = "Player", Description = "Player's media", Readers = 0, Credibility = 1f },
        };

        foreach (var media in defaultMedia)
        {
            _connection.Execute(
                @"INSERT INTO Media (Name, Description, Readers, Credibility) 
                VALUES (?, ?, ?, ?)",
                media.Name,
                media.Description,
                media.Readers,
                Math.Round(media.Credibility, 1)
            );
        }

    }
}

