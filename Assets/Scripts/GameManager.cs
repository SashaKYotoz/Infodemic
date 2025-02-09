using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int _activeEventId;
    public int ActiveEventId => _activeEventId;
    private DatabaseManager _dbManager;
    private ArticleGenerator _articleGenerator;
    private EventGenerator _eventGenerator;
    public static GameManager instance;


    public void SetActiveEventId(int eventId)
    {
        _activeEventId = eventId;
        PlayerPrefs.SetInt("ActiveEventID", _activeEventId);
        PlayerPrefs.Save();
    }

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

        var currentEvent = DatabaseManager.Instance.GetEvent(_activeEventId);
        string coreTruth = currentEvent.CoreTruth;

        List<string> formattedTruth = JsonKeyFormatter.GetFormattedKeysFromJson(coreTruth);

        foreach (var s in formattedTruth) {
            Debug.Log(s);
        }
    }

}