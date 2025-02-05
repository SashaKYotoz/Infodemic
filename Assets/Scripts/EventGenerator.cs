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
    public PostCreator postCreator;
    private void Start()
    {
        _connection = DatabaseManager.Instance.Connection;

        postCreator = new PostCreator(_connection);

        StartCoroutine(GenerateEvent());
    }

    // private void Update()
    // {
    //     if (Keyboard.current.spaceKey.wasPressedThisFrame)
    //     {
    //         Debug.Log("Generating event...");
    //         StartCoroutine(GenerateEvent());
    //     }
    // }

    public IEnumerator GenerateEvent()
    {
        int randomEventTypeId = Random.Range(1, 19);
        var eventType = _connection.Find<EventType>(randomEventTypeId);

        var organizations = DatabaseManager.Instance.GetRelevantOrganizations(randomEventTypeId);
        var characters = DatabaseManager.Instance.GetRelevantCharacters(randomEventTypeId);

        // Build the prompt
        string prompt = $@"
Generate a news event in STRICT JSON format using ONLY these database entities:

Event Type: {eventType.Name} ({eventType.Description})

Available Organizations (ID: Name):
{string.Join("\n", organizations.Select(o => $"- ID {o.Id}: {o.Name} ({o.Description}, Type: {DatabaseManager.Instance.GetOrganizationType(o.TypeId)}, Credibility: {o.Credibility})"))}

Available Characters (ID: Name):
{string.Join("\n", characters.Select(c => $"- ID {c.Id}: {c.Name} ({c.Profession}, Affiliation: {DatabaseManager.Instance.GetOrganizationName(c.Affilation)}, Credibility: {c.Credibility}, Biases: {DatabaseManager.Instance.GetCharacterBiases(c.Id)})"))}

Requirements:
1. For 'coreTruth', generate 3-5 context-specific facts BASED ON EVENT TYPE:
   - Analyze '{eventType.Name}' and '{eventType.Description}'
   - Choose measurable/verifiable dimensions relevant to this event type
   - Examples for guidance (DO NOT COPY THESE):
     * Environmental event → ""carbonOffset"", ""biodiversityImpact"", ""regulatoryCost""
     * Medical event → ""trialSuccessRate"", ""sideEffectFrequency"", ""productionScale""
     * Political event → ""voterSupport"", ""campaignCost"", ""scandalSeverity""
   - Ensure all facts are NUMERICAL VALUES WITH UNITS or EXPLICIT RATINGS

2. For posts:
   - Each post must manipulate 1-2 coreTruth facts through:
     * Exaggeration (e.g., ""60% reduction"" → ""massive 95% cut"")
     * Omission (e.g., ignoring facts like timeToMarket)
     * False attribution (e.g., ""independent study shows"" without citation)
   - Add subtle clues when distorting facts by:
     * Including round number approximations (e.g., ""about 20 million"")
     * Using weasel words (e.g., ""appears to"", ""some argue"")
     * Employing emotional language that affects credibility
   - IMPORTANT: In every post, include at least one reference (direct or indirect) to one of the coreTruth facts. For example, a post might include a phrase like ""reports suggest a pollution volume near 10 million gallons"" or ""some argue that the regulatory cost might be in the billions"". 
   - Make each post longer and richer in detail (at least 2-3 sentences) to ensure there is sufficient context and clues for players to piece together the truth.

3. In the generated posts:
   - Do not start your sentences with 'As [Organization]' or 'As [Profession]' or similar and do not use hashtags.
   - Use the exact source format:
       * For characters: ""Character:[ID]""
       * For organizations: ""Organization:[ID]""

4. Required JSON structure:
{{
    ""title"": ""Event Title"",
    ""description"": ""Neutral 1-sentence summary WITHOUT analysis"",
    ""coreTruth"": {{
        ""fact1"": ""valueWithUnit"",
        ""fact2"": ""rating/scale"",
        ""fact3"": ""percentage""
    }},
    ""generatedContent"": [
        {{
            ""source"": ""Organization:[ID]"" or ""Character:[ID]"",
            ""content"": ""Natural-sounding statement reflecting their bias, using 2-3 sentences and including at least one subtle reference to one of the coreTruth facts"",
            ""isTruthful"": ""true"" or ""false"" in base of the content and distortions,
            ""distortions"": [""Exaggerated fact1"", ""Changed fact2"", ""Omitted fact3""]
        }}
    ]
}}

Validation Rules:
- ALL coreTruth keys must be camelCase technical terms.
- NO overlapping/redundant facts in coreTruth.
- ALL numerical values must be realistically plausible.
- EVERY post must reference at least 1 coreTruth fact (directly or indirectly and truthfully or distorted).
- Each post must contain enough detail (2-3 sentences) to include subtle clues.
- DISTINCT voices: sarcastic/technical/emotional based on character.
- The posts are written in the first person.
- At least 4 posts were generated.
";


        // Escape special characters in the prompt
        prompt = EscapeJsonString(prompt);
        Debug.Log(prompt);

        // Build the request body
        string requestBody = $@"
{{
    ""messages"": [
        {{
            ""role"": ""user"",
            ""content"": ""{prompt}""
        }}
    ],
    ""max_tokens"": 3000,
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

    private void SaveEventData(string jsonResponse, int eventTypeId)
    {
        var json = JSON.Parse(jsonResponse);
        var eventData = json["choices"][0]["message"]["content"];
        var eventJson = JSON.Parse(eventData);

        // Save event
        var newEvent = new Events
        {
            Title = eventJson["title"],
            Description = eventJson["description"],
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
