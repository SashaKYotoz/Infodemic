using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using SimpleJSON;
using SQLite4Unity3d;
using System.IO;
using System.Linq;

public class ArticleGenerator : MonoBehaviour
{

    private string apiUrl = "https://api-inference.huggingface.co/models/meta-llama/Meta-Llama-3-8B-Instruct/v1/chat/completions";
    private string apiKey = "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";


    private readonly SQLiteConnection _connection;

    public ArticleGenerator()
    {
        _connection = DatabaseManager.Instance.Connection;
    }

    public IEnumerator GenerateArticle()
    {
        int activeEventId = GameManager.instance.ActiveEventId;
        Events activeEvent = _connection.Find<Events>(activeEventId);

        List<string> selectedWords = DatabaseManager.Instance.GetSelectedWordsForEvent(activeEventId);
        string selectedWordsText = string.Join(", ", selectedWords);

        List<Posts> eventPosts = DatabaseManager.Instance.GetPostsForEvent(activeEventId);
        string postsText = string.Join("\n", eventPosts.Select(p =>
     $"[{(p.CharacterId != null ? "Character:" + p.CharacterId : "Organization:" + p.OrganizationId)}] {p.Content}"));

        List<int> characterIds = DatabaseManager.Instance.GetParticipatingCharacters(activeEventId);
        List<int> organizationIds = DatabaseManager.Instance.GetParticipatingOrganizations(activeEventId);

        List<Characters> characters = DatabaseManager.Instance.GetCharacterDetails(characterIds);
        List<Organizations> organizations = DatabaseManager.Instance.GetOrganizationDetails(organizationIds);

        string charactersInfo = string.Join("\n", characters.Select(c => $"{c.Name} ({c.Profession}) - Biases: {DatabaseManager.Instance.GetCharacterBiases(c.Id)}"));
        string organizationsInfo = string.Join("\n", organizations.Select(o => $"{o.Name} (Type: {DatabaseManager.Instance.GetOrganizationType(o.TypeId)}) - Credibility: {o.Credibility}"));

        string prompt = $@"
You are a journalist writing an article for a news media outlet. Your goal is to construct a coherent and engaging news article based on the following event:
Event {activeEvent.Title}

Participating Characters:
{charactersInfo}

Participating Organizations:
{organizationsInfo}

Core truth: {activeEvent.CoreTruth}

Original Posts:
{postsText}

The player has selected the following key phrases from posts:
""{selectedWordsText}""

Validation Rules:
- Use ALL selected key phrases naturally.
- Reference details from original posts, characters, and organizations.
- Follow a journalistic structure: Headline, Introduction, Main Body, Conclusion.
- Do NOT add extra facts beyond what is provided.
- Maintain an appropriate tone.

Output JSON format:
{{
    ""title"": ""Generated article title"",
    ""content"": ""Full article text.""
}}
        ";

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
            Debug.Log("ARTICLE: Raw API Response: " + jsonResponse);
        }
        else
        {
            Debug.LogError("ARTICLE: Error with Hugging Face API: " + request.error + "\nResponse: " + request.downloadHandler.text);
        }
    }

    private string EscapeJsonString(string input)
    {
        return input
            .Replace("\\", "\\\\")  // Escape backslashes
            .Replace("\"", "\\\"")  // Escape double quotes
            .Replace("\n", "\\n")   // Escape newlines
            .Replace("\r", "\\r")   // Escape carriage returns
            .Replace("\t", "\\t");  // Escape tabs
    }
}
