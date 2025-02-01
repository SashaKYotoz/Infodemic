using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using SimpleJSON;
using SQLite4Unity3d;
using System.IO;
using System.Linq;

public class EventGenerator : MonoBehaviour
{

    private string apiUrl = "https://api-inference.huggingface.co/models/meta-llama/Meta-Llama-3-8B-Instruct/v1/chat/completions";
    private string apiKey = "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";

    private SQLiteConnection _connection;
    private string dbPath;

    private void Start()
    {
        dbPath = Path.Combine(Application.persistentDataPath, "infodemic.db");
        _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

        StartCoroutine(GenerateEvent());
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
{string.Join("\n", characters.Select(c => $"- ID {c.Id}: {c.Name} ({c.Profession}, Affilation: {GetOrganizationName(c.Affilation)}, Credibility: {c.Credibility}, Biases: {GetCharacterBiases(c.Id)})"))}

Requirements:
1. Use UP TO 3 organizations and UP TO 6 characters from the lists above. DO NOT INVENT NEW ENTITIES.
2. Generate 3-5 key facts about the event (e.g., damage amount, casualties, responsible party) and include them in 'coreTruth' instead of factNUM.
3. For each character, create a social media post that:
   - Reflects their biases (e.g., pro-corporate characters downplay damage, activists exaggerate it) and credibility.
   - Looks alive and realistic.
   - Manipulates or omits some of the 'coreTruth' variables.
   - Includes subtle clues that players can use to reconstruct the full truth.
4. Ensure the full truth into the 'coreTruth' can be deduced by cross-referencing all posts
5. In 'source' field, use EXACTLY one of these formats:
   - For characters: ""Character:[ID]""
   - For organizations: ""Organization:[ID]""
6. Never use names - only reference shown IDs
7. Format as STRICT JSON. Use the provided template:
{{
    ""title"": ""Event Title"",
    ""location"": ""City, Country"",
    ""coreTruth"": {{
        // AI-generated key facts about the event. They can be textual, percentual or numerical values.
        ""fact1"": ""value"",
        ""fact2"": ""value"",
        ""fact3"": ""another_value""
    }},
    ""generatedContent"": [
        {{
            ""source"": ""Organization: ID / Character: ID"",
            ""content"": ""Their perspective/statement"",
            ""isTruthful"": ""true/false"",
            ""distortions"": [""changed fact1"", ""omitted fact2""]
        }}
    ]
}}";

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

    private List<Character> GetRelevantCharacters(int eventTypeId)
    {
        return _connection.Query<Character>(@"
        SELECT c.* FROM Characters c
        JOIN CharacterBiases cb ON c.Id = cb.CharacterId
        JOIN BiasTags bt ON cb.BiasId = bt.BiasId
        JOIN EventTypeTags ett ON bt.TagId = ett.TagId
        WHERE ett.EventTypeId = ?
        AND c.LastUsedEventId IS NULL OR c.LastUsedEventId < datetime('now','-3 day')
        LIMIT 6
    ", eventTypeId);
    }

    private string GetBiasContext(List<Character> characters)
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
    var newEvent = new Event {
        Title = eventJson["title"],
        Location = eventJson["location"],
        GeneratedContent = eventJson["generatedContent"].ToString(),
        CoreTruth = eventJson["coreTruth"].ToString(),
        EventTypeId = eventTypeId
    };
    _connection.Insert(newEvent);

    // Save posts
    foreach (var post in eventJson["generatedContent"].AsArray)
    {
        var source = post.Value["source"].Value;
        var parts = source.Split(':');
        
        var newPost = new Posts {
            EventId = newEvent.Id,
            Content = post.Value["content"],
        };

        if (parts[0] == "Character" && int.TryParse(parts[1], out int charId))
        {
            newPost.CharacterId = charId;
        }
        else if (parts[0] == "Organization" && int.TryParse(parts[1], out int orgId))
        {
            newPost.OrganizationId = orgId;
        }
        else
        {
            Debug.LogError($"Invalid source format: {source}");
            continue;
        }

        _connection.Insert(newPost);
    }

        Debug.Log("Event and posts saved successfully!");
    }

}
