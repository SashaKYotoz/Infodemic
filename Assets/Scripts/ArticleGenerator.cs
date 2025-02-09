using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using SimpleJSON;
using SQLite4Unity3d;
using System.IO;
using System.Linq;
using NUnit.Framework.Constraints;

public class ArticleGenerator : MonoBehaviour
{

    private string apiUrl = "https://api-inference.huggingface.co/models/meta-llama/Meta-Llama-3-8B-Instruct/v1/chat/completions";
    private string apiKey = "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";

    private static ArticleGenerator _instance;

    private GameUIController gameUIController;


    private readonly SQLiteConnection _connection;

    public ArticleGenerator()
    {
        _connection = DatabaseManager.Instance.Connection;
    }
    public static ArticleGenerator Instance => _instance;
    public SQLiteConnection Connection => _connection;
    private void Awake()
    {
        gameUIController = GameObject.Find("GameUI").GetComponent<GameUIController>();
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator GenerateArticle()
    {
        int activeEventId = GameManager.instance.ActiveEventId;
        Events activeEvent = _connection.Find<Events>(activeEventId);
        List<SelectedWords> selectedWords = DatabaseManager.Instance.GetSelectedWordsForEvent(activeEventId).Where(w => w.IsApproved).ToList();
        string selectedWordsText = "";

        foreach (var word in selectedWords)
        {
            selectedWordsText += word.Word + ", ";
        }


        List<Posts> eventPosts = DatabaseManager.Instance.GetPostsForEvent(activeEventId);
        string postsText = string.Join("\n", eventPosts.Select(p =>
     $"[{(p.CharacterId != null ? "Character:" + p.CharacterId : "Organization:" + p.OrganizationId)}] {p.Content}"));

        List<int> characterIds = eventPosts.Where(p => p.CharacterId != null).Select(p => p.CharacterId.Value).Distinct().ToList();
        List<int> organizationIds = eventPosts.Where(p => p.OrganizationId != null).Select(p => p.OrganizationId.Value).Distinct().ToList();

        List<Characters> characters = DatabaseManager.Instance.GetCharacterDetails(characterIds);
        List<Organizations> organizations = DatabaseManager.Instance.GetOrganizationDetails(organizationIds);

        string charactersInfo = string.Join("\n", characters.Select(c => $"{c.Name} ({c.Profession}) - Biases: {DatabaseManager.Instance.GetCharacterBiases(c.Id)}"));
        string organizationsInfo = string.Join("\n", organizations.Select(o => $"{o.Name} (Type: {DatabaseManager.Instance.GetOrganizationType(o.TypeId)}) - Credibility: {o.Credibility}"));

        string prompt = $@"
You are a professional journalist tasked with writing a high-quality news article for a reputable media outlet. Your output must be strictly valid JSON with no extraneous text. Follow the instructions exactly.

The article must be based solely on the data provided below. Use this data to construct a coherent, engaging, and fact-based article. Importantly, you must use the player's selected key phrases exactly as given—even if they include irrelevant or incorrect information. You must compare the player's input against the provided Core Truth. If significant parts of the Core Truth are missing from the player's input, or if many of the selected phrases are irrelevant, contradictory, or nonsensical compared to the Core Truth, you must lower the overall veracity score accordingly. For example, if more than half of the player's selected phrases are unsupported or misleading, the veracity score should be low (closer to 1).

Below is the data:

-----------------------------
Event:
{activeEvent.Title}

Core Truth (in JSON format):
{activeEvent.CoreTruth}

Participating Characters:
{charactersInfo}

Participating Organizations:
{organizationsInfo}

Original Posts:
{postsText}

Player's Selected Key Phrases (comma-separated):
""{selectedWordsText}""
-----------------------------

Analysis Instructions:
1. Compare each selected key phrase with the Core Truth and Original Posts.
2. If many of the player's key phrases do not appear in or contradict the Core Truth, or if there are extra phrases that are irrelevant or misleading, lower the veracity score significantly. (For instance, if over 50% of the phrases are off, the score should be below 5; if none of the key phrases match the Core Truth, the score should be 1.)
3. Provide a single overall veracity score (a float from 1 to 10) based solely on the reliability and consistency of the selected key phrases with the provided data.

Article Requirements:
- Use ALL the selected key phrases exactly as provided, even if they include errors.
- Reference relevant details from the Original Posts, Participating Characters, and Participating Organizations.
- Follow a clear journalistic structure: Headline, Introduction, Main Body, and Conclusion.
- Do not add any extra facts or data beyond what is provided.
- Ensure the tone and style are serious, factual, and written in the first person.
- The article should be rich in detail (at least 2-3 sentences per section) and include subtle clues that both reinforce the Core Truth and expose any discrepancies in the player's input.

Output Requirements:
Output your response strictly in the following JSON structure with no extra commentary:

{{
    ""title"": ""Generated article title"",
    ""description"": ""A neutral one-sentence summary of the event without analysis."",
    ""veracityScore"": ""(a float from 1 to 10 indicating reliability, adjusted based on the player's input vs. the Core Truth)""
}}

Important:
- Output valid JSON only.
- Do not include any markdown formatting or extra text.
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

            var json = JSON.Parse(jsonResponse);
            var articleData = json["choices"][0]["message"]["content"];
            var articleJson = JSON.Parse(articleData);

            // Create Article
            var newArticle = new Articles
            {
                MediaId = 1,
                EventId = GameManager.instance.ActiveEventId,
                Title = articleJson["title"],
                Content = articleJson["content"],
                VeracityScore = articleJson["veracityScore"]
            };
            Debug.Log("TITLE: " + newArticle.Title);
            Debug.Log("CONTENT" + newArticle.Content);
            DatabaseManager.Instance.SaveArticle(newArticle);
            ChangeReputationLevel(newArticle);
            gameUIController.HandleArticle(newArticle);
        }
        else
        {
            Debug.LogError("ARTICLE: Error with Hugging Face API: " + request.error + "\nResponse: " + request.downloadHandler.text);
            StartCoroutine(GenerateArticle());
        }
    }

    private void ChangeReputationLevel(Articles article)
    {
        var media = DatabaseManager.Instance.GetMedia(article.MediaId);

        float baseline = 5f;
        float factor = 0.1f; // This factor determines how much the veracity score shifts credibility.
        float deltaCredibility = (article.VeracityScore - baseline) * factor;

        media.Credibility = Mathf.Clamp(media.Credibility + deltaCredibility, 1f, 10f);

        int additionalReaders = Mathf.RoundToInt(article.VeracityScore / 10f * 1000f);
        media.Readers += additionalReaders;
        _connection.Update(media);

        Debug.Log($"Media updated. New Credibility: {media.Credibility}, New Readers: {media.Readers}");
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
