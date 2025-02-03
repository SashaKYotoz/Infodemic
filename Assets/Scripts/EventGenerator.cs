using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using SimpleJSON;
using SQLite4Unity3d;
using System.IO;
using System.Linq;
using UnityEngine.InputSystem;

public class EventGenerator : MonoBehaviour
{

    private string apiUrl = "https://api-inference.huggingface.co/models/meta-llama/Meta-Llama-3-8B-Instruct/v1/chat/completions";
    private string apiKey = "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";

    private SQLiteConnection _connection;
    private string dbPath;

    public PostCreator postCreator;
    private void Start()
    {
        dbPath = Path.Combine(Application.persistentDataPath, "infodemic.db");
        _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

        postCreator = new PostCreator(_connection);

        StartCoroutine(GenerateEvent());
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Generating event...");
            StartCoroutine(GenerateEvent());
        }
    }
    
    private IEnumerator GenerateEvent()
    {
        int randomEventTypeId = Random.Range(1, 20);
        var eventType = _connection.Find<EventType>(randomEventTypeId);

        var organizations = GetRelevantOrganizations(randomEventTypeId);
        var characters = GetRelevantCharacters(randomEventTypeId);

        // Build the prompt
        string prompt = $@"
Generate a news event in STRICT JSON format using ONLY these database entities:

Event Type: {eventType.Name} ({eventType.Description})

Available Organizations (ID: Name):
{string.Join("\n", organizations.Select(o => $"- ID {o.Id}: {o.Name} ({o.Description}, Type: {GetOrganizationType(o.TypeId)}, Credibility: {o.Credibility})"))}

Available Characters (ID: Name):
{string.Join("\n", characters.Select(c => $"- ID {c.Id}: {c.Name} ({c.Profession}, Affiliation: {GetOrganizationName(c.Affilation)}, Credibility: {c.Credibility}, Biases: {GetCharacterBiases(c.Id)})"))}

Requirements:
1. Use UP TO 3 organizations and UP TO 6 characters from the lists above, based on event complexity. DO NOT INVENT NEW ENTITIES.
2. For each character, generate a social media post that:
   - Reflects their unique biases (for example, pro-corporate characters downplay negative impacts while activist characters exaggerate them) and considers their credibility.
   - Uses a natural, varied tone; avoid a repetitive or overly formal template.
   - May manipulate, exaggerate, or omit some of the 'coreTruth' variables.
   - Provides subtle clues so that, when all posts are considered together, the complete truth (the full set of key facts) can be deduced.
3. The 'coreTruth' object should include 3-5 key facts about the event (e.g., scientificAccuracy, emissionsReduction, initialCost). You may add more key facts if the event context requires it, but ensure each fact is logical and of high quality.
 For example (replace with your own data):
    ""coreTruth"": {{
      ""scientificAccuracy: ""95%"",
      ""emissionsReduction"": ""60%"",
      ""initialCost"": ""10 million USD""
   }}
4. In the generated posts:
   - Do not include extra phrases like 'As [Organization]' or hashtags.
   - Use the exact source format:
       - For characters: ""Character:[ID]""
       - For organizations: ""Organization:[ID]""
5. Follow this strict JSON format exactly:
{{
    ""title"": ""Event Title"",
    ""location"": ""City, Country"",
    ""coreTruth"": {{ 
        // Generate at least 3 key facts; add more if relevant.
        ""factKey1"": ""value"",
        ""factKey2"": ""value"",
        ""factKey3"": ""value""
    }},
    ""generatedContent"": [
        {{
            ""source"": ""Organization:[ID]"" or ""Character:[ID]"",
            ""content"": ""Their perspective/statement"",
            ""isTruthful"": ""true"" or ""false"",
            ""distortions"": [""changed fact1"", ""omitted fact2""]
        }}
    ]
}}

Ensure that:
- All numerical and textual key facts are realistic and coherent.
- Each post has a distinct, natural voice.
- The overall JSON output is valid.
";

        // Escape special characters in the prompt
        prompt = EscapeJsonString(prompt);

        // Build the request body
        string requestBody = $@"
{{
    ""messages"": [
        {{
            ""role"": ""user"",
            ""content"": ""{prompt}""
        }}
    ],
    ""max_tokens"": 2048,
    ""stream"": false
}}";

        // Send the request
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("Raw API Response: " + jsonResponse);
            SaveEventData(jsonResponse, eventType.Id);
            //ExtractAndPrintArticle(jsonResponse);
        }
        else
        {
            Debug.LogError("Error with Hugging Face API: " + request.error + "\nResponse: " + request.downloadHandler.text);
        }
    }

    // Helper method to escape JSON strings
    private string EscapeJsonString(string input)
    {
        return input
            .Replace("\\", "\\\\")  // Escape backslashes
            .Replace("\"", "\\\"")  // Escape double quotes
            .Replace("\n", "\\n")   // Escape newlines
            .Replace("\r", "\\r")   // Escape carriage returns
            .Replace("\t", "\\t");  // Escape tabs
    }

    private string GetOrganizationType(int typeId)
    {
        return _connection.Find<OrganizationTypes>(typeId)?.Name ?? "Unknown";
    }

    private string GetOrganizationName(int? orgId)
    {
        return orgId.HasValue ? _connection.Find<Organizations>(orgId.Value)?.Name : "Independent";
    }

    private string GetCharacterBiases(int characterId)
    {
        return string.Join(", ", _connection.Query<Bias>(
            "SELECT b.Name FROM Biases b " +
            "JOIN CharacterBiases cb ON b.Id = cb.BiasId " +
            "WHERE cb.CharacterId = ?", characterId)
            .Select(b => b.Name));
    }
    private List<Organizations> GetRelevantOrganizations(int eventTypeId)
    {
        return _connection.Query<Organizations>(@"
        SELECT o.* FROM Organizations o
        JOIN OrganizationTags ot ON o.Id = ot.OrganizationId
        JOIN EventTypeTags ett ON ot.TagId = ett.TagId
        WHERE ett.EventTypeId = ?
        AND o.LastUsedEventId IS NULL OR o.LastUsedEventId < datetime('now','-7 day')
        LIMIT 3
    ", eventTypeId);
    }

    private List<Characters> GetRelevantCharacters(int eventTypeId)
    {
        return _connection.Query<Characters>(@"
        SELECT c.* FROM Characters c
        JOIN CharacterBiases cb ON c.Id = cb.CharacterId
        JOIN BiasTags bt ON cb.BiasId = bt.BiasId
        JOIN EventTypeTags ett ON bt.TagId = ett.TagId
        WHERE ett.EventTypeId = ?
        AND c.LastUsedEventId IS NULL OR c.LastUsedEventId < datetime('now','-3 day')
        LIMIT 6
    ", eventTypeId);
    }

    private string GetBiasContext(List<Characters> characters)
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


    private void SaveEventData(string jsonResponse, int eventTypeId)
    {
        var json = JSON.Parse(jsonResponse);
        var eventData = json["choices"][0]["message"]["content"];
        var eventJson = JSON.Parse(eventData);

        // Save event
        var newEvent = new Event
        {
            Title = eventJson["title"],
            Location = eventJson["location"],
            GeneratedContent = eventJson["generatedContent"].ToString(),
            CoreTruth = eventJson["coreTruth"].ToString(),
            EventTypeId = eventTypeId
        };
        _connection.Insert(newEvent);

        // Save posts
        postCreator.CreateAndSavePosts(eventJson, newEvent.Id);

        Debug.Log("Event and posts saved successfully!");
    }

}
