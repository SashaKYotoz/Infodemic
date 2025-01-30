using UnityEngine;
using SQLite4Unity3d;
using System;
using Mono.Cecil.Cil;
using Unity.VisualScripting;

public class DatabaseManager : MonoBehaviour
{
    private SQLiteConnection _connection;

    private void Start()
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

    private void CreateTables()
    {


        _connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Tags (
                Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                Name TEXT UNIQUE
            );");

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
                Date TEXT,
                Location TEXT,
                GeneratedContent TEXT,
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
            CREATE TABLE IF NOT EXISTS OrganizationTags (
                OrganizationId INTEGER,
                TagId INTEGER,
                FOREIGN KEY (OrganizationId) REFERENCES Organization(Id),
                FOREIGN KEY (TagId) REFERENCES Tags(Id)
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
                LastUsedEventId INTEGER,
                FOREIGN KEY (TypeId) REFERENCES OrganizationTypes(Id)
            );");

        Debug.Log("All tables created successfully!");

        InsertDefaultValues();
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
        new { Name = "Sentinel Dynamics", TypeId = 1, Description = "A defense contractor specializing in advanced weaponry.", Credibility = 6.8f },
        new { Name = "Solaris Innovations", TypeId = 1, Description = "A renewable energy corporation pushing clean energy tech.", Credibility = 8.4f },
        new { Name = "CoreX Minerals", TypeId = 1, Description = "A global leader in rare earth mineral extraction.", Credibility = 5.9f },
        new { Name = "Summit Capital Group", TypeId = 1, Description = "A controversial private equity firm.", Credibility = 4.5f },
    
        // NGOs 
        new { Name = "GreenPulse Movement", TypeId = 2, Description = "Focuses on reducing plastic waste and cleaning oceans.", Credibility = 7.5f },
        new { Name = "Humanity's Voice Coalition", TypeId = 2, Description = "Defends human rights in the world.", Credibility = 7.8f },
        new { Name = "Unity Beyond Borders", TypeId = 2, Description = "Specializes in mediating peace treaties in conflict zones.", Credibility = 7.4f },


        // Political Parties
        new { Name = "Progressive Alliance", TypeId = 3, Description = "A forward-thinking party focused on climate reform.", Credibility = 7.2f },
        new { Name = "Liberty Front", TypeId = 3, Description = "A libertarian party promoting deregulation and free markets.", Credibility = 6.5f },
        new { Name = "Social Unity", TypeId = 3, Description = "Populist party advocating for economic equality.", Credibility = 5.8f },

        // Media
        new { Name = "Daily Spectrum", TypeId = 4, Description = "A reputable media outlet known for investigative journalism.", Credibility = 8.7f },
        new { Name = "Breaking Lens", TypeId = 4, Description = "A popular tabloid known for sensational headlines.", Credibility = 5.2f },
        new { Name = "Chronicle Insight", TypeId = 4, Description = "A digital-first platform for in-depth reporting.", Credibility = 7.6f },
    
        // Activists Group
        new { Name = "Green Vanguard", TypeId = 5, Description = "A radical environmentalist group.", Credibility = 6.1f },
        new { Name = "Parity Now!", TypeId = 5, Description = "A movement for gender equality", Credibility = 7.9f },
        new { Name = "Justice Impact", TypeId = 5, Description = "Campaigning against multinational corporate abuses.", Credibility = 7.2f },

        // Religious Organizations
        new { Name = "Order of the Silver Cross", TypeId = 6, Description = "A Catholic organization focused on humanitarian aid.", Credibility = 8.3f },
        new { Name = "The Crescent Fellowship", TypeId = 6, Description = "A Muslim organization promoting interfaith dialogue.", Credibility = 8.5f },

        // Universities / Research Institutions 

        new { Name = "Vanguard Institute of Innovation", TypeId = 7, Description = "Pioneers advanced technologies in AI and quantum computing.", Credibility = 9.2f },
        new { Name = "Arclight Research Center", TypeId = 7, Description = "Focuses on renewable energy breakthroughs and sustainable technologies.", Credibility = 8.8f },
        new { Name = "Helios Institute of Space Studies", TypeId = 7, Description = "A world leader in aerospace research and planetary science.", Credibility = 9.1f },

        // Labor Unions
        new { Name = "United Workers", TypeId = 8, Description = "A labor union advocating for worker rights in manufacturing.", Credibility = 7.5f },
        new { Name = "Global Labor Federation", TypeId = 8, Description = "An international union focused on promoting fair labor practices globally.", Credibility = 8.5f },
        new { Name = "The Solidarity Movement", TypeId = 8, Description = "An activist labor union focused on workers' rights across multiple industries.", Credibility = 7.7f },

        // Tech Startups
        new { Name = "NeuraCore AI", TypeId = 9, Description = "A startup focused on advanced AI for medical diagnostics.", Credibility = 8.6f },
        new { Name = "SkyLink Systems", TypeId = 9, Description = "Building next-gen satellite internet solutions.", Credibility = 7.9f },
        new { Name = "NimbusTech Innovations", TypeId = 9, Description = "A startup focused on creating AI-powered solutions for the healthcare industry.", Credibility = 8.3f },
        new { Name = "ByteForge Labs", TypeId = 9, Description = "Developing cutting-edge virtual reality experiences for education and entertainment.", Credibility = 8.7f },
        new { Name = "GreenSpire Technologies", TypeId = 9, Description = "A clean-tech startup focused on sustainable energy solutions for urban environments.", Credibility = 8.5f },

        // Charity/Foundation
        new { Name = "Lifeline Initiative", TypeId = 10, Description = "A charity focused on disaster relief and rebuilding efforts around the world.", Credibility = 9.0f },
        new { Name = "Hands Together", TypeId = 10, Description = "Supports global initiatives for poverty alleviation and social equality.", Credibility = 8.3f },
        new { Name = "World Healing Foundation", TypeId = 10, Description = "Focuses on providing clean water and sanitation to communities in need.", Credibility = 8.7f },
    };

        foreach (var org in defaultOrganizations)
        {
            if (_connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Organizations WHERE Name = ?", org.Name) == 0)
            {
                _connection.Execute(
                    "INSERT INTO Organizations (Name, TypeId, Description, Credibility) VALUES (?, ?, ?, ?)",
                    org.Name,
                    org.TypeId,
                    org.Description,
                    Math.Round(org.Credibility, 1)
                );
            }
        }


        var defaultCharacters = new[]
{
        // Sentinel Dynamics (Pro-Corporate, Pro-Government, Nationalist)
        new { Name = "James Calloway", Profession = "Defense Consultant", Affilation = 1, Credibility = 7.2f, Tier = 2},
        new { Name = "Rebecca Monroe", Profession = "Military Strategist", Affilation = 1, Credibility = 6.5f, Tier = 1},

        // Solaris Innovations (Pro-Environment, Techno-Optimist, Pro-Science)
        new { Name = "Ethan Caldwell", Profession = "Renewable Energy Scientist", Affilation = 2, Credibility = 8.9f, Tier = 3},
        new { Name = "Nadia Foster", Profession = "Sustainability Consultant", Affilation = 2, Credibility = 7.8f, Tier = 2},

        // CoreX Minerals (Pro-Corporate, Anti-Environment)
        new { Name = "Douglas Hayes", Profession = "Mining Executive", Affilation = 3, Credibility = 6.3f, Tier = 2},
        new { Name = "Megan Russell", Profession = "Resource Extraction Analyst", Affilation = 3, Credibility = 5.7f, Tier = 1},

        // Summit Capital Group (Elitist, Pro-Corporate, Anti-Government)
        new { Name = "Victor Langley", Profession = "Investment Tycoon", Affilation = 4, Credibility = 4.1f, Tier = 3},
        new { Name = "Charlotte Brennan", Profession = "Economic Strategist", Affilation = 4, Credibility = 4.8f, Tier = 2},

        // GreenPulse Movement (Pro-Environment, Socialist)
        new { Name = "Cassandra Yu", Profession = "Climate Activist", Affilation = 5, Credibility = 8.3f, Tier = 2},
        new { Name = "Liam Hutchinson", Profession = "Wildlife Conservationist", Affilation = 5, Credibility = 7.6f, Tier = 2},

        // Humanity’s Voice Coalition (Pro-Government, Globalist)
        new { Name = "Amir Rahman", Profession = "Human Rights Lawyer", Affilation = 6, Credibility = 8.2f, Tier = 3},
        new { Name = "Sophia Martinez", Profession = "NGO Coordinator", Affilation = 6, Credibility = 7.9f, Tier = 2},

        // Unity Beyond Borders (Mediating Peace Treaties)
        new { Name = "Jonathan Wells", Profession = "Conflict Resolution Expert", Affilation = 7, Credibility = 8.2f, Tier = 2},
        new { Name = "Amina Rahman", Profession = "International Relations Specialist", Affilation = 7, Credibility = 7.5f, Tier = 2},

        // Progressive Alliance (Climate Reform Party)
        new { Name = "Marissa Cohen", Profession = "Environmental Policy Advisor", Affilation = 8, Credibility = 7.8f, Tier = 2},
        new { Name = "Felix Grant", Profession = "Senator", Affilation = 8, Credibility = 6.9f, Tier = 2},

        // Liberty Front (Libertarian, Anti-Government)
        new { Name = "Travis Beaumont", Profession = "Political Commentator", Affilation = 9, Credibility = 6.4f, Tier = 1},
        new { Name = "Katie Hendricks", Profession = "Policy Analyst", Affilation = 9, Credibility = 6.8f, Tier = 2},

        // Social Unity (Populist Economic Party)
        new { Name = "Carlos Mendes", Profession = "Grassroots Organizer", Affilation = 10, Credibility = 5.6f, Tier = 1},
        new { Name = "Eleanor Hayes", Profession = "Labor Economist", Affilation = 10, Credibility = 6.4f, Tier = 2},

        // Daily Spectrum (Investigative Media)
        new { Name = "David Blackwood", Profession = "Investigative Journalist", Affilation = 11, Credibility = 8.9f, Tier = 3},
        new { Name = "Samantha Ortega", Profession = "Political Correspondent", Affilation = 11, Credibility = 8.1f, Tier = 2},

        // Breaking Lens (Populist, Conspiracy-Minded)
        new { Name = "Derek Malone", Profession = "Investigative Blogger", Affilation = 12, Credibility = 5.1f, Tier = 1},
        new { Name = "Jessica Vaughn", Profession = "Freelance Journalist", Affilation = 12, Credibility = 5.5f, Tier = 2},

        // Chronicle Insight (Digital-First Journalism)
        new { Name = "Raj Patel", Profession = "Digital Editor", Affilation = 13, Credibility = 7.4f, Tier = 2},
        new { Name = "Melissa Cho", Profession = "Data Journalist", Affilation = 13, Credibility = 7.9f, Tier = 2},

        // Green Vanguard (Pro-Environment, Techno-Pessimist)
        new { Name = "Elena Radcliffe", Profession = "Eco-Warrior", Affilation = 14, Credibility = 6.2f, Tier = 1},
        new { Name = "Benjamin Holt", Profession = "Climate Researcher", Affilation = 14, Credibility = 6.5f, Tier = 2},

        // Parity Now! (Gender Equality Movement)
        new { Name = "Zoe Sanders", Profession = "Women's Rights Advocate", Affilation = 15, Credibility = 8.2f, Tier = 2},
        new { Name = "Maya Delgado", Profession = "Activist", Affilation = 15, Credibility = 7.6f, Tier = 2},

        // Justice Impact (Anti-Corporate Activism)
        new { Name = "Liam Novak", Profession = "Corporate Watchdog", Affilation = 16, Credibility = 7.0f, Tier = 2},
        new { Name = "Olivia Tran", Profession = "Legal Analyst", Affilation = 16, Credibility = 7.3f, Tier = 2},

        // Order of the Silver Cross (Religious Fundamentalist, Pro-Government)
        new { Name = "Father Michael Graves", Profession = "Catholic Bishop", Affilation = 17, Credibility = 8.4f, Tier = 3},
        new { Name = "Sister Angela Turner", Profession = "Missionary", Affilation = 17, Credibility = 7.9f, Tier = 2},

        // The Crescent Fellowship (Interfaith Muslim Organization)
        new { Name = "Yusuf Al-Farid", Profession = "Interfaith Diplomat", Affilation = 18, Credibility = 8.7f, Tier = 3},
        new { Name = "Fatima Hassan", Profession = "Community Organizer", Affilation = 18, Credibility = 8.3f, Tier = 2},

        // Vanguard Institute of Innovation (AI and Quantum Research)
        new { Name = "Elliot West", Profession = "AI Ethics Researcher", Affilation = 19, Credibility = 9.1f, Tier = 3},
        new { Name = "Dr. Susan Caldwell", Profession = "Quantum Computing Specialist", Affilation = 19, Credibility = 9.3f, Tier = 3},
        
        // Arclight Research Center (Renewable Energy)
        new { Name = "Martin Lowe", Profession = "Renewable Energy Scientist", Affilation = 20, Credibility = 8.5f, Tier = 3},
        new { Name = "Elena Fischer", Profession = "Sustainable Tech Engineer", Affilation = 20, Credibility = 8.7f, Tier = 3},

        // Helios Institute of Space Studies (Space Exploration)
        new { Name = "Dr. Alan Reyes", Profession = "Astrophysicist", Affilation = 21, Credibility = 9.0f, Tier = 3},
        new { Name = "Sophia Kim", Profession = "Aerospace Engineer", Affilation = 21, Credibility = 8.9f, Tier = 3},

        // United Workers (Manufacturing Labor Union)
        new { Name = "Jack Mitchell", Profession = "Union Organizer", Affilation = 22, Credibility = 7.6f, Tier = 2},
        new { Name = "Rebecca Lin", Profession = "Factory Worker Representative", Affilation = 22, Credibility = 7.3f, Tier = 2},

        // Global Labor Federation (International Union)
        new { Name = "Hector Morales", Profession = "Union Representative", Affilation = 23, Credibility = 8.1f, Tier = 3},
        new { Name = "Maria Vasquez", Profession = "Human Rights Advocate", Affilation = 23, Credibility = 8.4f, Tier = 3},

        // The Solidarity Movement (Activist Union)
        new { Name = "Jamal Edwards", Profession = "Strike Coordinator", Affilation = 24, Credibility = 7.2f, Tier = 2},
        new { Name = "Hannah Weiss", Profession = "Labor Rights Journalist", Affilation = 24, Credibility = 7.8f, Tier = 2},
        
        // NeuraCore AI (Techno-Optimist, Pro-Science)
        new { Name = "Dr. Alan Voss", Profession = "AI Researcher", Affilation = 25, Credibility = 9.1f, Tier = 3},
        new { Name = "Samantha Lin", Profession = "Machine Learning Engineer", Affilation = 25, Credibility = 8.5f, Tier = 2},

        // SkyLink Systems (Satellite Internet Startup)
        new { Name = "Ethan Caldwell", Profession = "Satellite Engineer", Affilation = 26, Credibility = 7.7f, Tier = 2},
        new { Name = "Nadia Blake", Profession = "Telecommunications Specialist", Affilation = 26, Credibility = 8.0f, Tier = 2},

        // NimbusTech Innovations (AI Healthcare Startup)
        new { Name = "Dr. Adrian Wells", Profession = "AI Medical Researcher", Affilation = 27, Credibility = 8.4f, Tier = 3},
        new { Name = "Lisa Cooper", Profession = "Health Tech Developer", Affilation = 27, Credibility = 8.2f, Tier = 3},

        // ByteForge Labs (VR Development)
        new { Name = "Cody Ramirez", Profession = "VR Experience Designer", Affilation = 28, Credibility = 8.6f, Tier = 3},
        new { Name = "Tina Song", Profession = "Game Developer", Affilation = 28, Credibility = 8.3f, Tier = 3},

        // Lifeline Initiative (Disaster Relief Charity)
        new { Name = "Daniel Harper", Profession = "Humanitarian Coordinator", Affilation = 30, Credibility = 9.0f, Tier = 3},
        new { Name = "Olivia Bennett", Profession = "Relief Logistics Manager", Affilation = 30, Credibility = 8.8f, Tier = 3},

        // Hands Together - Poverty Alleviation and Social Equality
        new { Name = "Margaret Dawson", Profession = "Community Organizer", Affilation = 31, Credibility = 8.4f, Tier = 2},
        new { Name = "Omar Sinclair", Profession = "Policy Advocate", Affilation = 31, Credibility = 8.2f, Tier = 2},

        // World Healing Foundation - Clean Water & Sanitation Efforts
        new { Name = "Dr. Samuel Carter", Profession = "Water Sanitation Expert", Affilation = 32, Credibility = 8.8f, Tier = 1},
        new { Name = "Isabella Mendez", Profession = "Humanitarian Coordinator", Affilation = 32, Credibility = 8.6f, Tier = 2}
};

        foreach (var character in defaultCharacters)
        {
            _connection.Execute(
                "INSERT INTO Characters (Name, Profession, Affilation, Credibility, Tier, LastUsedEventId) VALUES (?, ?, ?, ?, ?, ?)",
                character.Name,
                character.Profession,
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
            new { CharacterId = 2, BiasId = 3 },  // Rebecca Monroe -> Pro-Corporate
            new { CharacterId = 2, BiasId = 9 },  // Rebecca Monroe -> Nationalist

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
            new { CharacterId = 8, BiasId = 5 },  // Charlotte Brennan -> Pro-Government
            new { CharacterId = 8, BiasId = 13 }, // Charlotte Brennan -> Elitist

            // GreenPulse Movement
            new { CharacterId = 9, BiasId = 1 },  // Cassandra Yu -> Pro-Environment
            new { CharacterId = 9, BiasId = 10 }, // Cassandra Yu -> Globalist
            new { CharacterId = 10, BiasId = 1 }, // Liam Hutchinson -> Pro-Environment
            new { CharacterId = 10, BiasId = 10 }, // Liam Hutchinson -> Globalist

            // Humanity’s Voice Coalition
            new { CharacterId = 11, BiasId = 6 }, // Amir Rahman -> Anti-Government
            new { CharacterId = 11, BiasId = 10 }, // Amir Rahman -> Globalist
            new { CharacterId = 12, BiasId = 10 }, // Sophia Martinez -> Globalist
            new { CharacterId = 12, BiasId = 17 }, // Sophia Martinez -> Socialist

            // Unity Beyond Borders
            new { CharacterId = 13, BiasId = 10 }, // Jonathan Wells -> Globalist
            new { CharacterId = 13, BiasId = 7 },  // Jonathan Wells -> Pro-Science
            new { CharacterId = 14, BiasId = 10 }, // Amina Rahman -> Globalist
            new { CharacterId = 14, BiasId = 5 },  // Amina Rahman -> Pro-Government

            // Progressive Alliance
            new { CharacterId = 15, BiasId = 1 },  // Marissa Cohen -> Pro-Environment
            new { CharacterId = 15, BiasId = 10 }, // Marissa Cohen -> Globalist
            new { CharacterId = 16, BiasId = 5 },  // Felix Grant -> Pro-Government
            new { CharacterId = 16, BiasId = 17 }, // Felix Grant -> Socialist

            // Liberty Front
            new { CharacterId = 17, BiasId = 16 }, // Travis Beaumont -> Libertarian
            new { CharacterId = 17, BiasId = 12 }, // Travis Beaumont -> Populist
            new { CharacterId = 18, BiasId = 16 }, // Katie Hendricks -> Libertarian
            new { CharacterId = 18, BiasId = 5 },  // Katie Hendricks -> Pro-Government

            // Social Unity
            new { CharacterId = 19, BiasId = 10 }, // Carlos Mendes -> Globalist
            new { CharacterId = 19, BiasId = 17 }, // Carlos Mendes -> Socialist
            new { CharacterId = 20, BiasId = 10 }, // Eleanor Hayes -> Globalist
            new { CharacterId = 20, BiasId = 17 }, // Eleanor Hayes -> Socialist

            // Daily Spectrum
            new { CharacterId = 21, BiasId = 10 }, // David Blackwood -> Globalist
            new { CharacterId = 21, BiasId = 7 },  // David Blackwood -> Pro-Science
            new { CharacterId = 22, BiasId = 10 }, // Samantha Ortega -> Globalist
            new { CharacterId = 22, BiasId = 7 },  // Samantha Ortega -> Pro-Science

            // Breaking Lens
            new { CharacterId = 23, BiasId = 11 }, // Derek Malone -> Conspiracy-Minded
            new { CharacterId = 23, BiasId = 12 }, // Derek Malone -> Populist
            new { CharacterId = 24, BiasId = 11 }, // Jessica Vaughn -> Conspiracy-Minded
            new { CharacterId = 24, BiasId = 12 }, // Jessica Vaughn -> Populist

            // Chronicle Insight
            new { CharacterId = 25, BiasId = 7 },  // Raj Patel -> Pro-Science
            new { CharacterId = 25, BiasId = 10 }, // Raj Patel -> Globalist
            new { CharacterId = 26, BiasId = 7 },  // Melissa Cho -> Pro-Science
            new { CharacterId = 26, BiasId = 10 }, // Melissa Cho -> Globalist

            // Green Vanguard
            new { CharacterId = 27, BiasId = 1 },  // Elena Radcliffe -> Pro-Environment
            new { CharacterId = 27, BiasId = 6 },  // Elena Radcliffe -> Anti-Government
            new { CharacterId = 28, BiasId = 1 },  // Benjamin Holt -> Pro-Environment
            new { CharacterId = 28, BiasId = 7 },  // Benjamin Holt -> Pro-Science

            // Parity Now!
            new { CharacterId = 29, BiasId = 15 }, // Zoe Sanders -> Techno-Pessimist
            new { CharacterId = 29, BiasId = 10 }, // Zoe Sanders -> Globalist
            new { CharacterId = 30, BiasId = 10 }, // Maya Delgado -> Globalist
            new { CharacterId = 30, BiasId = 17 }, // Maya Delgado -> Socialist

            // Justice Impact
            new { CharacterId = 31, BiasId = 10 }, // Liam Novak -> Globalist
            new { CharacterId = 31, BiasId = 17 }, // Liam Novak -> Socialist
            new { CharacterId = 32, BiasId = 5 },  // Olivia Tran -> Pro-Government
            new { CharacterId = 32, BiasId = 7 },  // Olivia Tran -> Pro-Science

            // Order of the Silver Cross
            new { CharacterId = 33, BiasId = 18 }, // Father Michael Graves -> Religious Fundamentalist
            new { CharacterId = 33, BiasId = 5 },  // Father Michael Graves -> Pro-Government
            new { CharacterId = 34, BiasId = 18 }, // Sister Angela Turner -> Religious Fundamentalist
            new { CharacterId = 34, BiasId = 10 }, // Sister Angela Turner -> Globalist

            // The Crescent Fellowship
            new { CharacterId = 35, BiasId = 10 }, // Yusuf Al-Farid -> Globalist
            new { CharacterId = 35, BiasId = 18 }, // Yusuf Al-Farid -> Religious Fundamentalist
            new { CharacterId = 36, BiasId = 17 }, // Fatima Hassan -> Socialist
            new { CharacterId = 36, BiasId = 10 }, // Fatima Hassan -> Globalist

            // Vanguard Institute of Innovation
            new { CharacterId = 37, BiasId = 7 },  // Elliot West -> Pro-Science
            new { CharacterId = 37, BiasId = 14 }, // Elliot West -> Techno-Optimist
            new { CharacterId = 38, BiasId = 7 },  // Dr. Susan Caldwell -> Pro-Science
            new { CharacterId = 38, BiasId = 14 }, // Dr. Susan Caldwell -> Techno-Optimist

            // Arclight Research Center
            new { CharacterId = 39, BiasId = 1 },  // Martin Lowe -> Pro-Environment
            new { CharacterId = 39, BiasId = 7 },  // Martin Lowe -> Pro-Science
            new { CharacterId = 40, BiasId = 1 },  // Elena Fischer -> Pro-Environment
            new { CharacterId = 40, BiasId = 14 }, // Elena Fischer -> Techno-Optimist

            // Helios Institute of Space Studies
            new { CharacterId = 41, BiasId = 7 },  // Dr. Alan Reyes -> Pro-Science
            new { CharacterId = 41, BiasId = 14 }, // Dr. Alan Reyes -> Techno-Optimist
            new { CharacterId = 42, BiasId = 7 },  // Sophia Kim -> Pro-Science
            new { CharacterId = 42, BiasId = 14 }, // Sophia Kim -> Techno-Optimist

            // United Workers
            new { CharacterId = 43, BiasId = 17 }, // Jack Mitchell -> Socialist
            new { CharacterId = 43, BiasId = 10 }, // Jack Mitchell -> Globalist
            new { CharacterId = 44, BiasId = 17 }, // Rebecca Lin -> Socialist
            new { CharacterId = 44, BiasId = 12 }, // Rebecca Lin -> Populist

            // Global Labor Federation
            new { CharacterId = 45, BiasId = 17 }, // Hector Morales -> Socialist
            new { CharacterId = 45, BiasId = 10 }, // Hector Morales -> Globalist
            new { CharacterId = 46, BiasId = 17 }, // Maria Vasquez -> Socialist
            new { CharacterId = 46, BiasId = 10 }, // Maria Vasquez -> Globalist

            // The Solidarity Movement
            new { CharacterId = 47, BiasId = 17 }, // Jamal Edwards -> Socialist
            new { CharacterId = 47, BiasId = 10 }, // Jamal Edwards -> Globalist
            new { CharacterId = 48, BiasId = 17 }, // Hannah Weiss -> Socialist
            new { CharacterId = 48, BiasId = 10 }, // Hannah Weiss -> Globalist

            // NeuraCore AI
            new { CharacterId = 49, BiasId = 7 },  // Dr. Alan Voss -> Pro-Science
            new { CharacterId = 49, BiasId = 14 }, // Dr. Alan Voss -> Techno-Optimist
            new { CharacterId = 50, BiasId = 7 },  // Samantha Lin -> Pro-Science
            new { CharacterId = 50, BiasId = 14 }, // Samantha Lin -> Techno-Optimist

            // SkyLink Systems
            new { CharacterId = 51, BiasId = 7 },  // Ethan Caldwell -> Pro-Science
            new { CharacterId = 51, BiasId = 14 }, // Ethan Caldwell -> Techno-Optimist
            new { CharacterId = 52, BiasId = 7 },  // Nadia Blake -> Pro-Science
            new { CharacterId = 52, BiasId = 14 }, // Nadia Blake -> Techno-Optimist

            // NimbusTech Innovations
            new { CharacterId = 53, BiasId = 7 },  // Dr. Adrian Wells -> Pro-Science
            new { CharacterId = 53, BiasId = 14 }, // Dr. Adrian Wells -> Techno-Optimist
            new { CharacterId = 54, BiasId = 7 },  // Lisa Cooper -> Pro-Science
            new { CharacterId = 54, BiasId = 14 }, // Lisa Cooper -> Techno-Optimist

            // ByteForge Labs
            new { CharacterId = 55, BiasId = 7 },  // Cody Ramirez -> Pro-Science
            new { CharacterId = 55, BiasId = 14 }, // Cody Ramirez -> Techno-Optimist
            new { CharacterId = 56, BiasId = 7 },  // Tina Song -> Pro-Science
            new { CharacterId = 56, BiasId = 14 }, // Tina Song -> Techno-Optimist

            // Lifeline Initiative
            new { CharacterId = 57, BiasId = 10 }, // Daniel Harper -> Globalist
            new { CharacterId = 57, BiasId = 17 }, // Daniel Harper -> Socialist
            new { CharacterId = 58, BiasId = 10 }, // Olivia Bennett -> Globalist
            new { CharacterId = 58, BiasId = 17 }, // Olivia Bennett -> Socialist

            // Hands Together
            new { CharacterId = 59, BiasId = 10 }, // Margaret Dawson -> Globalist
            new { CharacterId = 59, BiasId = 17 }, // Margaret Dawson -> Socialist
            new { CharacterId = 60, BiasId = 10 }, // Omar Sinclair -> Globalist
            new { CharacterId = 60, BiasId = 17 }, // Omar Sinclair -> Socialist

            // World Healing Foundation
            new { CharacterId = 61, BiasId = 10 }, // Dr. Samuel Carter -> Globalist
            new { CharacterId = 61, BiasId = 7 },  // Dr. Samuel Carter -> Pro-Science
            new { CharacterId = 62, BiasId = 10 }, // Isabella Mendez -> Globalist
            new { CharacterId = 62, BiasId = 17 }, // Isabella Mendez -> Socialist


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
            new { Name = "Peace Agreement Signed", Description = "Two or more nations or groups sign a peace accord" },
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

            new { EventTypeId = 3, TagId = 3 },  // #government - Peace Agreement Signed
            new { EventTypeId = 3, TagId = 7 },  // #activism

            new { EventTypeId = 4, TagId = 12 }, // #science - Scientific Discovery
            new { EventTypeId = 4, TagId = 10 }, // #education

            new { EventTypeId = 5, TagId = 9 },  // #economy - Economic Growth
            new { EventTypeId = 5, TagId = 3 },  // #government

            new { EventTypeId = 6, TagId = 20 }, // #space - Successful Space Mission
            new { EventTypeId = 6, TagId = 12 }, // #science

            new { EventTypeId = 7, TagId = 1 },  // #environment - Landmark Environmental Policy
            new { EventTypeId = 7, TagId = 3 },  // #government

            new { EventTypeId = 8, TagId = 5 },  // #technology - Tech Innovation
            new { EventTypeId = 8, TagId = 12 }, // #science

            new { EventTypeId = 9, TagId = 4 },  // #health - Cure for Disease
            new { EventTypeId = 9, TagId = 12 }, // #science

            new { EventTypeId = 10, TagId = 7 },  // #activism - Major Charity Success
            new { EventTypeId = 10, TagId = 11 }, // #media

            // Neutral / Mixed Events
            new { EventTypeId = 11, TagId = 5 },  // #technology - Global Summit on AI Ethics
            new { EventTypeId = 11, TagId = 13 }, // #crime

            new { EventTypeId = 12, TagId = 9 },  // #economy - New Infrastructure Project
            new { EventTypeId = 12, TagId = 19 }, // #transportation

            new { EventTypeId = 13, TagId = 20 }, // #space - New Space Exploration Initiative
            new { EventTypeId = 13, TagId = 12 }, // #science

            // Negative Events
            new { EventTypeId = 14, TagId = 1 },  // #environment - Environmental Disaster
            new { EventTypeId = 14, TagId = 8 },  // #disaster

            new { EventTypeId = 15, TagId = 3 },  // #government - Political Scandal
            new { EventTypeId = 15, TagId = 6 },  // #corruption

            new { EventTypeId = 16, TagId = 4 },  // #health - Health Crisis
            new { EventTypeId = 16, TagId = 12 }, // #science

            new { EventTypeId = 17, TagId = 2 },  // #corporate - Corporate Fraud
            new { EventTypeId = 17, TagId = 9 },  // #economy

            new { EventTypeId = 18, TagId = 5 },  // #technology - Tech Controversy
            new { EventTypeId = 18, TagId = 13 }, // #crime

            new { EventTypeId = 19, TagId = 7 },  // #activism - Labor Dispute
            new { EventTypeId = 19, TagId = 9 },  // #economy

            new { EventTypeId = 20, TagId = 11 }, // #media - Conspiracy Theory
            new { EventTypeId = 20, TagId = 14 }  // #religion
        };

        foreach (var eventTypeTag in eventTypeTags)
        {
            _connection.Execute(
                @"INSERT INTO EventTypeTags (EventTypeId, TagId) VALUES (?, ?)",
                eventTypeTag.EventTypeId,
                eventTypeTag.TagId
            );
        }

    }
}

