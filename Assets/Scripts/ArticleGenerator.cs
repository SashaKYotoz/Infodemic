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
You are a journalist writing an article for a news media outlet. Your goal is to construct a coherent and engaging news article based on the following event and evaluate the reliability of the information provided by the player's selected key phrases.

Event: {activeEvent.Title}

Participating Characters:
{charactersInfo}

Participating Organizations:
{organizationsInfo}

Core Truth:
{activeEvent.CoreTruth}

Original Posts:
{postsText}

The player has selected the following key phrases from the posts:
""{selectedWordsText}""

Before writing the article, analyze the selected key phrases:
- Verify that each selected phrase is supported by the core truth and details from the original posts.
- Identify if any selected phrases are contradictory or if the player has selected too many phrases.
- Provide an overall assessment of the veracity of the selected information on a scale of 1 (very unreliable) to 10 (completely reliable). This score will be used to determine the player's reward or demotion.

Then, construct the article ensuring:
- ALL selected key phrases are used naturally in the text.
- Details from original posts, characters, and organizations are referenced appropriately.
- The article follows a journalistic structure: Headline, Introduction, Main Body, Conclusion.
- No extra facts beyond those provided are added.
- The tone remains appropriate to the event's context.

Finally, output the result in the following JSON format:
{{
    ""title"": ""Generated article title"",
    ""content"": ""Full article text."",
    ""veracityScore"": ""(a float from 1 to 10 indicating reliability)""
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
            DatabaseManager.Instance.SaveArticle(newArticle);

            ChangeReputationLevel(newArticle);
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
