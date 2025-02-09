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

        // Fetch characters and organizations
        List<int> characterIds = eventPosts.Where(p => p.CharacterId != null).Select(p => p.CharacterId.Value).Distinct().ToList();
        List<int> organizationIds = eventPosts.Where(p => p.OrganizationId != null).Select(p => p.OrganizationId.Value).Distinct().ToList();

        List<Characters> characters = DatabaseManager.Instance.GetCharacterDetails(characterIds);
        List<Organizations> organizations = DatabaseManager.Instance.GetOrganizationDetails(organizationIds);

        string charactersInfo = string.Join("\n", characters.Select(c => $"{c.Name} ({c.Profession}) - Biases: {DatabaseManager.Instance.GetCharacterBiases(c.Id)}"));
        string organizationsInfo = string.Join("\n", organizations.Select(o => $"{o.Name} (Type: {DatabaseManager.Instance.GetOrganizationType(o.TypeId)}) - Credibility: {o.Credibility}"));

        // Updated prompt
        string prompt = $@"
Notes on Word Panels:
The player's selected words are organized according to predefined word panels for the event. The mapping of word panels to fact keys is as follows:
   - Time To Market
   - Distance Traveled
   - Team Size
The selected words are formatted as lines where each line contains a panel name followed by a colon and the player's selected value. For example:
   Time To Market: 200
   Team Size: 150 members
   Distance Traveled: [value, if provided]
These values should be used to replace the corresponding fact keys in the Core Truth.

You are a news generator for a media literacy game. The event details above include a Core Truth in JSON format with several fact keys (for example, ""teamSize"", ""size"", ""timeToMarket"", etc.). The player has provided their selected words for each fact key using the word panels format described above. Note that the player's selections might be formatted differently or be less detailed than the original data. For instance:
   - If the Core Truth contains:
         ""teamSize"": ""150 members""
     and the player submits:
         Team Size: 150 members
     this should be considered a perfect match.
   - Similarly, if the Core Truth has:
         ""size"": ""10 m^2""
     and the player chooses:
         Size: small
     this is acceptable unless the original posts indicate that more precise details were available.

Specific Points and Validation Rules:
1. Article Generation:
   - Generate a realistic news article written in a neutral, third-person media tone, as if produced by a professional news outlet.
   - The article must integrate the player's selected words (provided in the word panels format) in place of the original truth data for each corresponding fact key.
   - The article may include quotes or references from characters or organizations to support the narrative.
   - The narrative should be realistic and resemble actual media reporting, avoiding first-person narration.

2. Data Replacement and Validation:
   - Replace each fact's original truth data with the player's selected words, even if the details differ.
   - Evaluate each fact by comparing the player's selections with the original Core Truth:
       a. If the selected words capture the intended meaning (e.g., numeric values or qualitative descriptions), consider it a perfect match.
       b. If a fact key is not mentioned in the original posts, do not penalize the player's score for that fact.
       c. If the player's selection is less precise but still conveys the correct sense, assign a high veracity rating.
       d. If the selection is completely off or missing, reduce the veracity rating accordingly.

3. Veracity Scoring and Verdict:
   - Assign a veracity score on a scale from 1 to 10:
         1 indicates very poor accuracy (completely random or missing relevant information).
         10 indicates near-perfect accuracy.
   - Provide a concise, supportive verdict that explains the player's performance in clear, non-technical language using friendly and encouraging terms. When referring to fact keys, use user-friendly names (e.g., ""Time To Market"" instead of ""timeToMarket"").

4. Input Data
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

Player's Selected Words for Core Truth:
{selectedWordsText}

Output Requirements:
Your final response must be strictly in JSON format containing only the following keys:
- ""title"": The title of the generated article.
- ""content"": The full article content, written in a realistic news reporting style in the third person and incorporating the player's selected words.
- ""veracityScore"": A number between 1 and 10 representing the accuracy of the player's selections.
- ""verdict"": A concise, supportive explanation of the player's performance.

Do not include any additional text or formatting outside of valid JSON.
";





        prompt = EscapeJsonString(prompt);
        Debug.Log(prompt);

        // Build and send request (unchanged)
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
                VeracityScore = articleJson["veracityScore"],
                Verdict = articleJson["verdict"]
            };
            Debug.Log("TITLE: " + newArticle.Title);
            Debug.Log("CONTENT" + newArticle.Content);
            Debug.Log("VERDICT: " + newArticle.Verdict);
            DatabaseManager.Instance.SaveArticle(newArticle);
            gameUIController.HandleArticle(newArticle);
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
        float factor = 0.3f; // This factor determines how much the veracity score shifts credibility.
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
