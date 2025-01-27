using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using SimpleJSON;
using SQLite4Unity3d;
using System.IO;

public class NewsGenerator : MonoBehaviour
{
    private string apiUrl = "https://api-inference.huggingface.co/models/meta-llama/Meta-Llama-3-8B-Instruct/v1/chat/completions";
    private string apiKey = "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";

    [SerializeField] private List<string> topics = new List<string> { "Technology", "Health", "Environment", "Politics", "Sports" };

    private SQLiteConnection connection;
    private string dbPath;

    private void Start()
    {
        dbPath = Path.Combine(Application.persistentDataPath, "infodemic.db");
        Debug.Log(dbPath);
        connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        DisplayArticles();
        string topic = topics[Random.Range(0, topics.Count)];
       // StartCoroutine(GenerateText(topic));
    }
    

    // To remove!!
    public void DisplayArticles()
    {
        List<Article> articles = GetAllArticles();
        foreach (var article in articles)
        {
            Debug.Log($"Title: {article.Title}");
            Debug.Log($"Content: {article.Content}");
        }
    }
    // To remove!!
    public List<Article> GetAllArticles()
    {
        // Retrieve all articles from the database
        return connection.Query<Article>("SELECT * FROM Article");
    }

    private IEnumerator GenerateText(string topic)
    {
        string prompt = $"Write a detailed and comprehensive news article on the topic '{topic}' in JSON format. The JSON should contain two fields: 'title' and 'content'. The 'title' should be a clear and engaging headline for the article, and the 'content' should be a solid, continuous article with no links, no external references, and no incomplete sentences.";

        string requestBody = $@"
        {{
            ""messages"": [
                {{
                    ""role"": ""user"",
                    ""content"": ""{prompt}""
                }}
            ],
            ""max_tokens"": 1000,
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
            Debug.Log("Raw API Response: " + jsonResponse);
            ExtractAndPrintArticle(jsonResponse);
        }
        else
        {
            Debug.LogError("Error with Hugging Face API: " + request.error + "\nResponse: " + request.downloadHandler.text);
        }
    }


    public void SaveArticle(string title, string content)
    {
        Article article = new Article
        {
            Title = title,
            Content = content
        };

        connection.Insert(article); // Insert the article into the database
        Debug.Log("Article saved to database.");
    }

    private void ExtractAndPrintArticle(string jsonResponse)
    {
        try
        {
            var root = JSON.Parse(jsonResponse);

            // Navigate to the content field
            if (root["choices"] != null && root["choices"].Count > 0)
            {
                string content = root["choices"][0]["message"]["content"];

                // Clean the JSON content inside the string
                int jsonStart = content.IndexOf('{');
                int jsonEnd = content.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {

                    string articleJson = content.Substring(jsonStart, jsonEnd - jsonStart + 1);

                    // Parse the cleaned JSON
                    var articleData = JSON.Parse(articleJson);

                    string title = articleData["title"];
                    string articleContent = articleData["content"];

                    Debug.Log($"Title: {title}");
                    Debug.Log($"Content: {articleContent}");

                    SaveArticle(title, articleContent);
                }
                else
                {
                    Debug.LogError("Failed to extract JSON object from the 'content' field.");
                }
            }
            else
            {
                Debug.LogError("No valid 'content' field found in the response.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error parsing response: " + ex.Message);
        }
    }
}
