using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

    // Labels in main panels
    private Label noteText, postText, aiNote, percentageOfCorrectness, percentageOfEnthusiasm;

    private List<Button> gameButtons = new List<Button>();
    private List<string> wordsPlayerSelected = new List<string>();
    private Dictionary<Label, List<string>> clickedWordsPerLabel = new Dictionary<Label, List<string>>();

    private AudioSource audioSource;

    private void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();
        document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        InitializePanels(root);
        InitializeTabs(root);
        InitializeLabels(root);
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

    private void InitializeLabels(VisualElement root)
    {
        noteText = root.Q<Label>("noteText");
        postText = root.Q<Label>("postText");
        aiNote = root.Q<Label>("aiNote");
        percentageOfCorrectness = root.Q<Label>("percentageOfCorrectness");
        percentageOfEnthusiasm = root.Q<Label>("percentageOfEnthusiasm");
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
        note.RegisterCallback<ClickEvent>(HandleNotePanel);
        news.RegisterCallback<ClickEvent>(callback => ChangeVisibility("newsPanel", true));
        post.RegisterCallback<ClickEvent>(callback => ChangeVisibility("postPanel", !post.ClassListContains("blocked-tab")));
        result.RegisterCallback<ClickEvent>(callback => ChangeVisibility("resultPanel", !result.ClassListContains("blocked-tab")));

        root.Q<Button>("mainExit").RegisterCallback<ClickEvent>(QuitGame);

        gameButtons = root.Query<Button>().ToList();
        gameButtons.ForEach(b => b.RegisterCallback<ClickEvent>(PlayClickSound));

        exit = root.Query<Button>(name: "exit").ToList();
        exit.ForEach(b => b.RegisterCallback<ClickEvent>(callback => b.parent.parent.style.display = DisplayStyle.None));
        backToSource = root.Query<Button>(name: "backToSource").ToList();
        backToSource.ForEach(b => b.RegisterCallback<ClickEvent>(callback => ManageBackToSourceButtons(b)));

        RegisterBackButton(root);
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
    private void QuitGame(ClickEvent e)
    {
        SceneManager.UnloadSceneAsync("Game");
        SceneManager.LoadScene("Management");
    }

    private void RegisterBackButton(VisualElement root)
    {
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

    private void OnTextUnclicked(PointerDownEvent evt, Label textLabel)
    {
        // Get the word that was unclicked
        Vector2 localMousePosition = evt.localPosition;
        string unclickedWord = GetWordAtPosition(textLabel, localMousePosition);

        if (!string.IsNullOrEmpty(unclickedWord))
        {

            // !!! Control, if all right.
            int postId = (int)textLabel.userData;
            int activeEventId = GameManager.instance.ActiveEventId; // use activeEventId instead of folderId for now

            wordsPlayerSelected.Remove(unclickedWord);
            clickedWordsPerLabel[textLabel].Remove(unclickedWord);

            DatabaseManager.Instance.RemoveSelectedWord(activeEventId, postId, unclickedWord);

            RemoveHighlight(textLabel, unclickedWord);
        }
    }


    private void OnDisable()
    {
        gameButtons.ForEach(b => b.UnregisterCallback<ClickEvent>(PlayClickSound));
        note.RegisterCallback<ClickEvent>(HandleNotePanel);
        news.RegisterCallback<ClickEvent>(callback => ChangeVisibility("newsPanel", true));
        post.RegisterCallback<ClickEvent>(callback => ChangeVisibility("postPanel", !post.ClassListContains("blocked-tab")));
        result.RegisterCallback<ClickEvent>(callback => ChangeVisibility("resultPanel", !result.ClassListContains("blocked-tab")));
    }

    public void StartContent(List<Posts> postsToShow)
    {

        //TODO: Add ID to post.userData!!!
        foreach (Posts post in postsToShow)
        {
            bool randomBoolean = MathUtils.RandomBoolean();
            VisualElement fillableNewsHolder = CreateNewsHolder(randomBoolean);

            Label newsTitleText = new() { text = provacativeWords[UnityEngine.Random.Range(0, provacativeWords.Length)], style = { fontSize = 32 } };
            fillableNewsHolder.Q("newsPictureHolder").Add(newsTitleText);

            Label newsContentText = fillableNewsHolder.Q<Label>("newsContentText");
            newsContentText.text = FormatContentText(post.Content, randomBoolean);

            fillableNewsHolder.RegisterCallback<ClickEvent>(callback => HandleNewsClick(post));
            newsPanel.Add(fillableNewsHolder);
        }
    }

    private VisualElement CreateNewsHolder(bool randomBoolean)
    {
        VisualTreeAsset originalAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameUI.uxml");
        VisualElement holder = originalAsset.Instantiate().Q("newsHolder");
        holder.style.display = DisplayStyle.Flex;
        holder.style.flexShrink = 1;
        holder.style.flexGrow = 0;
        if (randomBoolean)
        {
            holder.style.width = holder.style.maxWidth = holder.style.minWidth = new StyleLength(new Length(50, LengthUnit.Percent));
        }
        return holder;
    }

    private string FormatContentText(string content, bool randomBoolean)
    {
        return content.Length > (randomBoolean ? 36 : 22) ? content.Substring(0, randomBoolean ? 32 : 17) + "..." : content;
    }

    private void HandleNewsClick(Posts post)
    {
        if (post.CharacterId != null)
            ChangeDisplayOfPanel(socialMediaPanel, "postsHolder", post);
        if (post.OrganizationId != null)
            ChangeDisplayOfPanel(sourcePanel, "linksHolder", post);
    }

    private string GetLinkText(string innitText)
    {
        System.Text.StringBuilder formattedText = new();
        int charCount = 0;

        foreach (char c in innitText)
        {
            formattedText.Append(c);
            charCount++;
            if (charCount % 66 == 0)
                formattedText.Append('\n');
        }
        return formattedText.ToString() + "...";
    }

    private void Update()
    {
        if (wordsPlayerSelected.Any() && note.ClassListContains("blocked-tab"))
        {
            note.RemoveFromClassList("blocked-tab");
        }
    }
    private void ChangeDisplayOfPanel(VisualElement element, [AllowsNull] string elementToPutContentIn, Posts post)
    {
        element.style.display = DisplayStyle.Flex;
        if (elementToPutContentIn != null)
        {
            VisualElement element1 = document.rootVisualElement.Q(elementToPutContentIn);
            if (element1 is ListView listView)
            {
                Label textToPut = new();
                textToPut.text = GetLinkText(post.Content.Substring(0, (int)(post.Content.Length / 3)));
                textToPut.style.marginTop = new StyleLength(new Length(5, LengthUnit.Percent));
                textToPut.style.marginBottom = new StyleLength(new Length(5, LengthUnit.Percent));
                textToPut.style.color = Color.blue;
                textToPut.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                textToPut.style.height = new StyleLength(new Length(20, LengthUnit.Percent));
                textToPut.style.backgroundColor = new StyleColor(new Color(0.9f, 0.9f, 0.9f));

                //TODO: handle gen of content to social media user | web article
                textToPut.RegisterCallback<ClickEvent>(callback =>
                {
                    (elementToPutContentIn.Equals("linksHolder") ? webArticle : socialMedia).style.display = DisplayStyle.Flex;
                    element.style.display = DisplayStyle.None;
                });
                listView.hierarchy.Add(textToPut);
            }
        }
    }
    // Clicked words have to be added as a lists linked to specific event, 
    // following class will foreach each event that has filled list and add instance of "originalAsset" to notePanel 

    // VisualTreeAsset originalAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameUI.uxml");
    // VisualElement holder = originalAsset.Instantiate().Q("newsHolder");
    // holder.style.display = DisplayStyle.Flex;
    // holder.style.flexShrink = 1;
    // holder.style.flexGrow = 0;

    // when clicked on folder("originalAsset"), player will see selected by its words and will able to select them for postPanel
    private void HandleNotePanel(ClickEvent e)
    {
        var root = document.rootVisualElement;
        if (!note.ClassListContains("blocked-tab"))
        {
            ChangeVisibility("notePanel", true);
            Label textKeeper = new Label();
            textKeeper.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            textKeeper.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            textKeeper.style.fontSize = 24;
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

    //text click related stuff
    private void OnTextClicked(PointerDownEvent evt, Label textLabel)
    {
        Vector2 localMousePosition = evt.localPosition;
        string clickedWord = GetWordAtPosition(textLabel, localMousePosition);

        if (!string.IsNullOrEmpty(clickedWord))
        {

            // We need to put userData into postText before
            // int postId = (int)postText.userData;
            int activeEventId = GameManager.instance.ActiveEventId;
            wordsPlayerSelected.Add(clickedWord);
            clickedWordsPerLabel[textLabel].Add(clickedWord);
            // DatabaseManager.Instance.InsertSelectedWord(activeEventId, postId, clickedWord);

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

    private void RemoveHighlight(Label textLabel, string word)
    {
        string fullText = textLabel.text;

        string unhighlightedText = fullText.Replace($"<color=yellow>{word}</color>", word);

        textLabel.text = unhighlightedText;
    }

    public void LoadFolders()
    {
        //int activeEventId = GameManager.instance.ActiveEventId;
        //List<WordFolders> folders = DatabaseManager.Instance.GetFoldersForEvent(activeEventId);


        
        //TODO: GIVE A LOOK AND ADJUST
        VisualElement folderListPanel = document.rootVisualElement.Q("folderListPanel");
        folderListPanel.Clear(); // Clear previous items

        List<WordFolders> folders = DatabaseManager.Instance.GetFolders();

        foreach (WordFolders folder in folders)
        {
            Button folderButton = new Button();
            folderButton.text = folder.FolderName;
            folderButton.userData = folder.Id;

            // When clicked, set this folder as active.
            folderButton.RegisterCallback<ClickEvent>(evt =>
            {
                LoadSelectedWordsForFolder(folder.Id);
            });

            folderListPanel.Add(folderButton);
        }
    }

    private void LoadSelectedWordsForFolder(int folderId)
    {
        // Retrieve selected words for this folder from the DB.
        List<SelectedWords> words = DatabaseManager.Instance.GetSelectedWordsForFolder(folderId);

        // ADJUST IT TO YOURSELF
        VisualElement selectedWordsPanel = document.rootVisualElement.Q("selectedWordsPanel");
        selectedWordsPanel.Clear();

        foreach (SelectedWords sw in words)
        {
            Label wordLabel = new Label(sw.Word);
            selectedWordsPanel.Add(wordLabel);
        }
    }


    //buttons effects
    private void PlayClickSound(ClickEvent e)
    {
        audioSource.Play();
    }
}