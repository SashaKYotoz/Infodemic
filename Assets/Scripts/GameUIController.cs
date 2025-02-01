using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    private UIDocument document;
    //Panels
    private VisualElement newsPanel, sourcePanel, socialMedia, webArticle;
    //tabs
    private Button news, post, note, result;
    // labels in main panels
    private Label noteText, postText, aiNote, percentageOfCorrectness, percentageOfEnthusiasm;
    // social media panel's labels
    private Label personInfo, socialMediaContent;
    // web article's labels
    private Label articleContent, articleTitle;
    private List<Button> gameButtons = new List<Button>();
    private List<string> wordsPlayerSelected = new List<string>();

    private Dictionary<Label, List<string>> clickedWordsPerLabel = new Dictionary<Label, List<string>>();

    private AudioSource audioSource;

    private void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();

        document = GetComponent<UIDocument>();

        var root = document.rootVisualElement;
        newsPanel = root.Q("newsPanel");
        sourcePanel = root.Q("sourcePanel");
        socialMedia = root.Q("socialMedia");
        webArticle = root.Q("webArticle");

        news = root.Q<Button>("news");
        post = root.Q<Button>("post");
        note = root.Q<Button>("note");
        result = root.Q<Button>("result");
        noteText = root.Q<Label>("noteText");
        postText = root.Q<Label>("postText");
        aiNote = root.Q<Label>("aiNote");
        percentageOfCorrectness = root.Q<Label>("percentageOfCorrectness");
        percentageOfEnthusiasm = root.Q<Label>("percentageOfEnthusiasm");
        personInfo = root.Q<Label>("personInfo");
        socialMediaContent = root.Q<Label>("socialMediaContent");
        articleContent = root.Q<Label>("articleContent");
        articleTitle = root.Q<Label>("articleTitle");

        note.RegisterCallback<ClickEvent>(HandleNotePanel);
        news.RegisterCallback<ClickEvent>(callback => ChangeVisibility("newsPanel",true));
        post.RegisterCallback<ClickEvent>(callback => ChangeVisibility("postPanel", !post.ClassListContains("blocked-tab")));
        result.RegisterCallback<ClickEvent>(callback => ChangeVisibility("resultPanel", !result.ClassListContains("blocked-tab")));

        gameButtons = root.Query<Button>().ToList();
        gameButtons.ForEach(b => b.RegisterCallback<ClickEvent>(PlayClickSound));
        root.Query<Button>(name: "goBackButton").ToList().ForEach(b => b.RegisterCallback<ClickEvent>(callback =>
        {
            if (root.Q("socialMedia").style.display == DisplayStyle.Flex)
            {
                root.Q("socialMediaPanel").style.display = DisplayStyle.Flex;
                root.Q("socialMedia").style.display = DisplayStyle.None;
            }
            else
            {
                root.Q("sourcePanel").style.display = DisplayStyle.Flex;
                root.Q("webArticle").style.display = DisplayStyle.None;
            }
        }));

        var textLabels = root.Query<Label>().Where(label => label.ClassListContains("supports_change")).ToList();
        foreach (var label in textLabels)
        {
            if (!clickedWordsPerLabel.ContainsKey(label))
            {
                clickedWordsPerLabel[label] = new List<string>();
            }
            label.RegisterCallback<PointerDownEvent>(evt => OnTextClicked(evt, label));
        }
    }

    private void HandleNotePanel(ClickEvent e)
    {
        var root = document.rootVisualElement;
        if (!note.ClassListContains("blocked-tab"))
        {
            ChangeVisibility("notePanel",true);
            Label textKeeper = new Label();
            textKeeper.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            textKeeper.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            textKeeper.style.fontSize = 22;
            textKeeper.style.color = Color.white;
            foreach (string s in wordsPlayerSelected)
            {
                textKeeper.text += string.Format("{0} {1}", textKeeper.text.Count() % 34 == 0 ? "\n" : "\t", s);
            }
            root.Q("notePanel").Add(textKeeper);
        }
    }
    public void ChangeVisibility(string panelName, bool condition)
    {
        var root = document.rootVisualElement;
        string[] panelNames = { "postPanel", "newsPanel", "resultPanel", "notePanel" };

        if (condition)
        {
            foreach (string panel in panelNames)
            {
                root.Q("contentPanel").Q<VisualElement>(panel).style.display = (panel == panelName) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
    private void Start()
    {
        TextGen();
    }
    private void Update()
    {
        if (wordsPlayerSelected.Any() && note.ClassListContains("blocked-tab"))
        {
            note.RemoveFromClassList("blocked-tab");
        }
    }
    private void TextGen()
    {
        int randomAmount = UnityEngine.Random.Range(1, 4);
        for (int i = 0; i < randomAmount; i++)
        {
            VisualTreeAsset originalAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameUI.uxml");
            VisualElement fillableNewsHolder = originalAsset.Instantiate().Q("newsHolder");
            fillableNewsHolder.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            fillableNewsHolder.style.height = new StyleLength(new Length(15, LengthUnit.Percent));
            fillableNewsHolder.style.display = DisplayStyle.Flex;
            Label newsTextLabel = fillableNewsHolder.Q<Label>("newsHolderText");
            newsTextLabel.text = "abyaka";
            VisualElement newsPictureHolder = fillableNewsHolder.Q<VisualElement>("newsPictureHolder");
            fillableNewsHolder.RegisterCallback<ClickEvent>(callback => ChangeDisplayOfPanel(callback, sourcePanel, "linksHolder", "sometext" + i));
            newsPanel.Add(fillableNewsHolder);
        }
    }
    private void OnDisable()
    {
        gameButtons.ForEach(b => b.UnregisterCallback<ClickEvent>(PlayClickSound));
        note.RegisterCallback<ClickEvent>(HandleNotePanel);
        news.RegisterCallback<ClickEvent>(callback => ChangeVisibility("newsPanel",true));
        post.RegisterCallback<ClickEvent>(callback => ChangeVisibility("postPanel", !post.ClassListContains("blocked-tab")));
        result.RegisterCallback<ClickEvent>(callback => ChangeVisibility("resultPanel", !result.ClassListContains("blocked-tab")));
    }
    private void PlayClickSound(ClickEvent e)
    {
        audioSource.Play();
    }
    private void ChangeDisplayOfPanel(ClickEvent e, VisualElement element, [AllowsNull] string elementToPutContentIn, [AllowsNull] string contentToPutIn)
    {
        element.style.display = DisplayStyle.Flex;
        if (elementToPutContentIn != null)
        {
            VisualElement element1 = document.rootVisualElement.Q(elementToPutContentIn);
            if (element1 is ListView listView)
            {
                Label textToPut = new();
                if (contentToPutIn != null)
                    textToPut.text = contentToPutIn;
                textToPut.style.marginTop = new StyleLength(new Length(5, LengthUnit.Percent));
                textToPut.style.color = Color.black;
                textToPut.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                textToPut.style.height = new StyleLength(new Length(25, LengthUnit.Percent));
                textToPut.style.backgroundColor = new StyleColor(new Color(0.9f, 0.9f, 0.9f));
                textToPut.RegisterCallback<ClickEvent>(callback =>
                {
                    webArticle.style.display = DisplayStyle.Flex;
                    element.style.display = DisplayStyle.None;
                });
                listView.hierarchy.Add(textToPut);
            }
        }
    }
    //TODO: collect words clicked by player to list
    private void OnTextClicked(PointerDownEvent evt, Label textLabel)
    {
        Vector2 localMousePosition = evt.localPosition;
        string clickedWord = GetWordAtPosition(textLabel, localMousePosition);

        if (!string.IsNullOrEmpty(clickedWord))
        {
            wordsPlayerSelected.Add(clickedWord);
            clickedWordsPerLabel[textLabel].Add(clickedWord);
            HighlightWord(textLabel, clickedWord);
        }
    }

    private string GetWordAtPosition(Label textLabel, Vector2 position)
    {
        string fullText = textLabel.text;
        string[] words = fullText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        float accumulatedWidth = 0f;
        float spaceWidth = textLabel.MeasureTextSize(" ", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

        for (int i = 0; i < words.Length; i++)
        {
            float wordWidth = textLabel.MeasureTextSize(words[i], 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

            if (position.x >= accumulatedWidth && position.x <= accumulatedWidth + wordWidth)
            {
                Debug.Log($"Clicked word: {words[i]} at position {position}");
                return words[i];
            }
            accumulatedWidth += wordWidth + spaceWidth;
        }

        return "";
    }


    private void HighlightWord(Label textLabel, string word)
    {
        string fullText = textLabel.text;
        string highlightedText = "";

        string[] words = fullText.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i++)
        {
            if (words[i] == word)
            {
                highlightedText += $"<color=yellow>{words[i]}</color> ";
            }
            else
            {
                highlightedText += words[i] + " ";
            }
        }

        textLabel.text = highlightedText.Trim();
    }
}