using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewsConstructor : MonoBehaviour
{
    public TextAsset newsJsonData;
    public TextAsset contentJsonData;
    public List<Template> templates = new List<Template>();

    [System.Serializable]
    public class NewsData
    {
        public List<string> newsTypes;
        public List<string> sourceTypes;
    }

    [System.Serializable]
    private class Content
    {
        public string name;
        public string lastname;
    }

    [System.Serializable]
    private class ContentRoot
    {
        public List<Content> contents;
    }

    void Start()
    {
        LoadTemplates();
    }

    void LoadTemplates()
    {
        NewsData newsData = LoadNewsData();
        List<Content> contentList = LoadContent();

        if (newsData.newsTypes.Count == 0 || newsData.sourceTypes.Count == 0 || contentList.Count == 0)
        {
            Debug.LogWarning("One or more JSON files are empty. No templates created.");
            return;
        }

        for (int i = 0; i < Mathf.Min(newsData.newsTypes.Count, newsData.sourceTypes.Count, contentList.Count); i++)
        {
            Template template = new Template(
                contentList[Random.Range(0, contentList.Count)].name,
                contentList[Random.Range(0, contentList.Count)].lastname,
                newsData.newsTypes[Random.Range(0, newsData.newsTypes.Count)],
                newsData.sourceTypes[Random.Range(0, newsData.sourceTypes.Count)]
            );
            templates.Add(template);
        }
    }

    NewsData LoadNewsData()
    {
        if (newsJsonData == null)
        {
            Debug.LogWarning("News JSON file is missing.");
            return new NewsData { newsTypes = new List<string>(), sourceTypes = new List<string>() };
        }

        NewsData parsedData = JsonUtility.FromJson<NewsData>(newsJsonData.text);
        return parsedData != null ? parsedData : new NewsData { newsTypes = new List<string>(), sourceTypes = new List<string>() };
    }

    List<Content> LoadContent()
    {
        if (contentJsonData == null)
        {
            Debug.LogWarning("Content JSON file is missing.");
            return new List<Content>();
        }

        ContentRoot parsedData = JsonUtility.FromJson<ContentRoot>(contentJsonData.text);
        return parsedData != null && parsedData.contents != null ? parsedData.contents : new List<Content>();
    }
}