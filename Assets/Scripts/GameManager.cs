using System.Collections.Generic;
using System.Data.Common;
using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int _activeEventId;
    public int ActiveEventId => _activeEventId;
    private DatabaseManager _dbManager;
    private ArticleGenerator _articleGenerator;
    private EventGenerator _eventGenerator;
    public static GameManager instance;

    private void Awake()
    {
        _dbManager = gameObject.AddComponent<DatabaseManager>();
        _articleGenerator = gameObject.AddComponent<ArticleGenerator>();
        _eventGenerator = gameObject.AddComponent<EventGenerator>();
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        if (PlayerPrefs.HasKey("ActiveEventID"))
        {
            _activeEventId = PlayerPrefs.GetInt("ActiveEventID");
        }
        else
        {
            _activeEventId = 0;
            PlayerPrefs.SetInt("ActiveEventID", _activeEventId);
            PlayerPrefs.Save();
            StartCoroutine(_eventGenerator.GenerateEvent());
        }
    }

    public void SaveGeneratedWords(SelectedWords selectedWords)
    {
        _dbManager.SaveSelectedWords(selectedWords);
    }
}