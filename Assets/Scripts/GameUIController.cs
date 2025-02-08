using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    private System.Random random = new System.Random();
    private string[] provacativeWords = { "Wow!", "Impossible!", "Sensation!", "Don't miss" };
    private UIDocument document;

    // Panels
    private VisualElement newsPanel, sourcePanel, socialMediaPanel, socialMedia, webArticle;

    // Tabs
    private Button news, post, note, result;
    // Widgets
    private List<Button> exit, backToSource;

    private List<Button> gameButtons = new List<Button>();
    private Dictionary<Label, List<string>> clickedWordsPerLabel = new Dictionary<Label, List<string>>();
    private Dictionary<Label, string> originalTextPerLabel = new Dictionary<Label, string>();
    private List<string> wordsPlayerSelected = new List<string>();

    private AudioSource audioSource;

    private void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        InitializePanels(root);
        InitializeTabs(root);
        RegisterCallbacks(root);
    }

    private void InitializePanels(VisualElement root)
    {
        newsPanel = root.Q("newsPanel");
        sourcePanel = root.Q("sourcePanel");
        socialMedia = root.Q("socialMedia");
        socialMediaPanel = root.Q("socialMediaPanel");
        webArticle = root.Q("webArticle");
    }

    private void InitializeTabs(VisualElement root)
    {
        news = root.Q<Button>("news");
        post = root.Q<Button>("post");
        note = root.Q<Button>("note");
        result = root.Q<Button>("result");
    }


    private void Start()
    {
        int activeEventId = GameManager.instance.ActiveEventId;
        Debug.Log($"Active event id: {activeEventId}");
        if (activeEventId != 0)
        {
            Debug.Log("Initializated");
            List<Posts> postsToShow = DatabaseManager.Instance.GetPostsForEvent(activeEventId);
            StartContent(postsToShow);

            LoadFolders();
        }
    }

    private void RegisterCallbacks(VisualElement root)
    {
        note.RegisterCallback<ClickEvent>(callback => ChangeVisibility("notePanel", !note.ClassListContains("blocked-tab"), LoadFolders));
        news.RegisterCallback<ClickEvent>(callback => ChangeVisibility("newsPanel", true));
        post.RegisterCallback<ClickEvent>(callback => ChangeVisibility("postPanel", !post.ClassListContains("blocked-tab"), UpdateTextForPost));
        result.RegisterCallback<ClickEvent>(callback => ChangeVisibility("resultPanel", !result.ClassListContains("blocked-tab")));

        root.Q<Button>("mainExit").RegisterCallback<ClickEvent>(ShowModalWindow);
        root.Q<Button>("postButton").RegisterCallback<ClickEvent>(callback => StartCoroutine(ArticleGenerator.Instance.GenerateArticle()));

        gameButtons = root.Query<Button>().ToList();
        gameButtons.ForEach(b => b.RegisterCallback<ClickEvent>(PlayClickSound));

        exit = root.Query<Button>(name: "exit").ToList();
        exit.ForEach(b => b.RegisterCallback<ClickEvent>(callback => b.parent.parent.style.display = DisplayStyle.None));
        backToSource = root.Query<Button>(name: "backToSource").ToList();
        // backToSource.ForEach(b => b.RegisterCallback<ClickEvent>(callback => ManageBackToSourceButtons(b)));
        RegisterTextLabelCallbacks(root);
    }
    private void ManageBackToSourceButtons(Button b)
    {
        if (b.parent.parent == socialMedia)
        {
            socialMedia.style.display = DisplayStyle.None;
            socialMediaPanel.style.display = DisplayStyle.Flex;
        }
        else
        {
            b.parent.parent.style.display = DisplayStyle.None;
            sourcePanel.style.display = DisplayStyle.Flex;
        }
    }
    private void ShowModalWindow(ClickEvent e)
    {
        VisualElement holder = document.rootVisualElement.Q("modalWindow");
        holder.style.display = DisplayStyle.Flex;
        Button quitGameButton = holder.Q<Button>("quitGameButton");
        Button cancelButton = holder.Q<Button>("cancelButton");
        quitGameButton.RegisterCallback<ClickEvent>(callback => QuitGame());
        cancelButton.RegisterCallback<ClickEvent>(callback => holder.style.display = DisplayStyle.None);
    }
    private void QuitGame()
    {
        SceneManager.UnloadSceneAsync("Game");
        SceneManager.LoadScene("Management");
    }

    private void RegisterTextLabelCallbacks(VisualElement root)
    {
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


    private void OnDisable()
    {
        gameButtons.ForEach(b => b.UnregisterCallback<ClickEvent>(PlayClickSound));
        note.RegisterCallback<ClickEvent>(callback => ChangeVisibility("notePanel", !note.ClassListContains("blocked-tab"), LoadFolders));
        news.RegisterCallback<ClickEvent>(callback => ChangeVisibility("newsPanel", true));
        post.RegisterCallback<ClickEvent>(callback => ChangeVisibility("postPanel", !post.ClassListContains("blocked-tab")));
        result.RegisterCallback<ClickEvent>(callback => ChangeVisibility("resultPanel", !result.ClassListContains("blocked-tab")));
    }

    public void StartContent(List<Posts> postsToShow)
    {
        ScrollView newsPanel = document.rootVisualElement.Q<ScrollView>("newsPanel");
        foreach (Posts post in postsToShow)
        {
            VisualElement fillableNewsHolder = CreateNewsHolder(post);
            fillableNewsHolder.userData = post.Id;
            fillableNewsHolder.RegisterCallback<ClickEvent>(callback =>
                ChangeDisplayOfPanel(post.OrganizationId != null ? webArticle : socialMedia, post));
            newsPanel.contentContainer.Add(fillableNewsHolder);
        }
    }

    private VisualElement CreateNewsHolder(Posts post)
    {
        VisualTreeAsset originalAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameUI.uxml");
        VisualElement holder = originalAsset.Instantiate().Q("userPostHolder");
        Label userName = holder.Q<Label>("userName");
        Label userText = holder.Q<Label>("userText");

        holder.style.display = DisplayStyle.Flex;

        string sourceName = "Unknown";
        if (post.CharacterId != null)
        {
            sourceName = DatabaseManager.Instance.GetCharacterName(post.CharacterId.Value);
        }
        else if (post.OrganizationId != null)
        {
            sourceName = DatabaseManager.Instance.GetOrganizationName(post.OrganizationId.Value);
        }
        userName.text = sourceName;
        userText.text = post.Content;

        return holder;
    }

    private void ChangeDisplayOfPanel(VisualElement element, Posts post)
    {
        webArticle.style.display = DisplayStyle.None;
        socialMedia.style.display = DisplayStyle.None;
        element.style.display = DisplayStyle.Flex;
        if (element.Equals(webArticle))
        {
            Label articleContent = webArticle.Q<Label>("articleContent");
            Label articleTitle = webArticle.Q<Label>("articleTitle");

            SetLabelText(articleContent, post.Content);
            SetLabelText(articleTitle, DatabaseManager.Instance.GetOrganizationName(post.OrganizationId.Value));
        }
        else
        {
            Label socialMediaContent = socialMedia.Q<Label>("socialMediaContent");
            Label personInfo = socialMedia.Q<Label>("personInfo");

            SetLabelText(socialMediaContent, post.Content);
            SetLabelText(personInfo, DatabaseManager.Instance.GetCharacterName(post.CharacterId.Value));
        }
    }
    private void SetLabelText(Label label, string text)
    {
        label.text = text;
        if (originalTextPerLabel.ContainsKey(label))
        {
            originalTextPerLabel[label] = text;
        }
        else
        {
            originalTextPerLabel.Add(label, text);
        }
    }

    private void Update()
    {
        if (wordsPlayerSelected.Any() && note.ClassListContains("blocked-tab"))
        {
            note.RemoveFromClassList("blocked-tab");
        }
    }

    public void ChangeVisibility(string panelName, bool condition, Action? task = null)
    {
        var root = document.rootVisualElement;
        string[] panelNames = { "postPanel", "newsPanel", "resultPanel", "notePanel" };

        if (condition)
        {
            foreach (string panel in panelNames)
            {
                root.Q("contentPanel").Q<VisualElement>(panel).style.display = (panel == panelName) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            task?.Invoke();
        }
    }

    private void OnTextClicked(PointerDownEvent evt, Label textLabel)
    {
        Vector2 localMousePosition = evt.localPosition;
        string clickedWord = GetWordAtPosition(textLabel, localMousePosition);

        if (!string.IsNullOrEmpty(clickedWord))
        {
            if (!clickedWordsPerLabel[textLabel].Contains(clickedWord))
            {
                int activeEventId = GameManager.instance.ActiveEventId;
                wordsPlayerSelected.Add(clickedWord);
                clickedWordsPerLabel[textLabel].Add(clickedWord);
                // DatabaseManager.Instance.InsertSelectedWord(activeEventId, (int)textLabel.userData, clickedWord);
            }
            else
            {
                OnTextUnclicked(clickedWord, textLabel);
            }
            UpdateHighlights(textLabel);
        }
    }

    private void OnTextUnclicked(string unclickedWord, Label textLabel)
    {
        // int postId = (int)textLabel.userData;
        int activeEventId = GameManager.instance.ActiveEventId;
        wordsPlayerSelected.Remove(unclickedWord);
        clickedWordsPerLabel[textLabel].Remove(unclickedWord);
        // DatabaseManager.Instance.RemoveSelectedWord(activeEventId, postId, unclickedWord);

        UpdateHighlights(textLabel);
    }
    private string GetWordAtPosition(Label textLabel, Vector2 position)
    {
        // Adjust for any left/top padding in the label.
        float effectiveX = position.x - textLabel.resolvedStyle.paddingLeft;
        float effectiveY = position.y - textLabel.resolvedStyle.paddingTop;

        // Use the stored original text (without rich text tags)
        string fullText = originalTextPerLabel.ContainsKey(textLabel)
                            ? originalTextPerLabel[textLabel]
                            : textLabel.text;
        string[] words = fullText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        float availableWidth = textLabel.contentRect.width;
        float lineHeight = textLabel.resolvedStyle.fontSize * 1.2f;

        List<List<string>> lines = new List<List<string>>();
        List<string> currentLine = new List<string>();
        float currentLineWidth = 0f;
        float spaceWidth = textLabel.MeasureTextSize(" ", 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;

        foreach (string word in words)
        {
            float wordWidth = textLabel.MeasureTextSize(word, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            if (currentLine.Count == 0)
            {
                currentLine.Add(word);
                currentLineWidth = wordWidth;
            }
            else if (currentLineWidth + spaceWidth + wordWidth <= availableWidth)
            {
                currentLine.Add(word);
                currentLineWidth += spaceWidth + wordWidth;
            }
            else
            {
                lines.Add(new List<string>(currentLine));
                currentLine.Clear();
                currentLine.Add(word);
                currentLineWidth = wordWidth;
            }
        }
        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }

        int clickedLineIndex = Mathf.FloorToInt(effectiveY / lineHeight);
        if (clickedLineIndex < 0 || clickedLineIndex >= lines.Count)
        {
            return "";
        }
        List<string> clickedLineWords = lines[clickedLineIndex];

        // Use a tolerance (in pixels) to help capture very small words.
        float tolerance = 2f; // adjust this value as needed
        float accumulatedX = 0f;
        foreach (string word in clickedLineWords)
        {
            float wordWidth = textLabel.MeasureTextSize(word, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
            if (effectiveX >= (accumulatedX - tolerance) && effectiveX <= (accumulatedX + wordWidth + tolerance))
            {
                Debug.Log($"Clicked word: {word} at effective position ({effectiveX}, {effectiveY}) on line {clickedLineIndex}");
                return word;
            }
            accumulatedX += wordWidth + spaceWidth;
        }

        return "";
    }


    private void UpdateHighlights(Label textLabel)
    {
        if (!originalTextPerLabel.ContainsKey(textLabel))
            return;

        string originalText = originalTextPerLabel[textLabel];
        string[] words = originalText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> selectedWords = clickedWordsPerLabel[textLabel];

        string highlightedText = "";
        foreach (string word in words)
        {
            if (selectedWords.Contains(word))
            {
                highlightedText += $"<color=yellow>{word}</color> ";
            }
            else
            {
                highlightedText += word + " ";
            }
        }
        textLabel.text = highlightedText.Trim();
    }


    public void LoadFolders()
    {
        //int activeEventId = GameManager.instance.ActiveEventId;
        //List<WordFolders> folders = DatabaseManager.Instance.GetFoldersForEvent(activeEventId);

        //TODO: GIVE A LOOK AND ADJUST
        document.rootVisualElement.Q("selectedWordsPanel").style.display = DisplayStyle.None;
        VisualElement folderListPanel = document.rootVisualElement.Q("folderListPanel");
        folderListPanel.style.display = DisplayStyle.Flex;
        folderListPanel.Clear(); // Clear previous items

        List<WordFolders> folders = DatabaseManager.Instance.GetFolders();

        foreach (WordFolders folder in folders)
        {
            VisualTreeAsset originalAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameUI.uxml");
            VisualElement holder = originalAsset.Instantiate().Q("noteNewsFolder");
            holder.style.display = DisplayStyle.Flex;
            holder.style.flexShrink = 1;
            holder.style.flexGrow = 0;
            Button folderButton = holder.Q<Button>("folderButton");
            holder.Q<Label>("folderName").text = folder.FolderName.Substring(0, 8);
            holder.userData = folder.Id;

            // When clicked, set this folder as active.
            folderButton.RegisterCallback<ClickEvent>(evt => LoadSelectedWordsForEvent(folder.Id));

            folderListPanel.Add(holder);
        }
    }

    private void LoadSelectedWordsForEvent(int eventId)
    {
        document.rootVisualElement.Q("folderListPanel").style.display = DisplayStyle.None;
        // Retrieve selected words for this folder from the DB.
        List<SelectedWords> words = DatabaseManager.Instance.GetSelectedWordsForEvent(eventId);

        // ADJUST IT TO YOURSELF
        document.rootVisualElement.Q("folderListPanel").style.display = DisplayStyle.None;
        VisualElement selectedWordsPanel = document.rootVisualElement.Q("selectedWordsPanel");
        selectedWordsPanel.style.display = DisplayStyle.Flex;
        selectedWordsPanel.Clear();

        foreach (SelectedWords sw in words)
        {
            Label wordLabel = new Label(sw.Word) { text = sw.Word };
            selectedWordsPanel.Add(wordLabel);
        }
    }

    private void UpdateTextForPost()
    {
        Label postText = document.rootVisualElement.Q<Label>("postText");
        // postText.text = text;
    }
    public void HandleArticle(Articles article)
    {
        Label postText = document.rootVisualElement.Q<Label>("postText");
        postText.text = article.Content;
        result.RemoveFromClassList("blocked-tab");
    }

    //buttons effects
    private void PlayClickSound(ClickEvent e)
    {
        audioSource.Play();
    }
}