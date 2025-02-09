using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    private UIDocument document;
    private VisualTreeAsset originalAsset;

    // Panels
    private VisualElement newsPanel, postWordsPanel, sourcePanel, socialMediaPanel, socialMedia, webArticle;

    // Tabs
    private Button news, post, note, result;
    // Widgets
    private List<Button> exit, backToSource;

    private List<Button> gameButtons = new List<Button>();
    private Dictionary<Label, List<string>> clickedWordsPerLabel = new Dictionary<Label, List<string>>();
    private Dictionary<Label, string> originalTextPerLabel = new Dictionary<Label, string>();
    private List<string> wordsPlayerSelected = new List<string>();

    private VisualElement currentChosenPanel;

    [SerializeField]
    private AudioSource buttonClickSource;
    [SerializeField]
    private AudioSource unlockTabSource;
    private void OnEnable()
    {
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
        postWordsPanel = root.Q("postWordsPanel");
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
        originalAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameUI.uxml");
        int activeEventId = GameManager.instance.ActiveEventId;
        Debug.Log($"Active event id: {activeEventId}");
        if (activeEventId != 0)
        {
            Debug.Log("Initializated");
            List<Posts> postsToShow = DatabaseManager.Instance.GetPostsForEvent(activeEventId);
            StartContent(postsToShow);

            LoadFolders();
        }
        UpdateWidgetsInfo();
    }

    private void RegisterCallbacks(VisualElement root)
    {
        note.RegisterCallback<ClickEvent>(callback => ChangeVisibility("notePanel", !note.ClassListContains("blocked-tab"), LoadFolders));
        news.RegisterCallback<ClickEvent>(callback => ChangeVisibility("newsPanel", true));
        post.RegisterCallback<ClickEvent>(callback => ChangeVisibility("postPanel", !post.ClassListContains("blocked-tab"), UpdateTextForPost));
        result.RegisterCallback<ClickEvent>(callback => ChangeVisibility("resultPanel", !result.ClassListContains("blocked-tab"), UpdateResultPanel));

        root.Q<Button>("mainExit").RegisterCallback<ClickEvent>(ShowModalWindow);
        root.Q<Button>("postButton").RegisterCallback<ClickEvent>(callback => StartCoroutine(ArticleGenerator.Instance.GenerateArticle()));
        root.Q<Button>("backToFolder").RegisterCallback<ClickEvent>(callback => LoadFolders());

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
        post.RegisterCallback<ClickEvent>(callback => ChangeVisibility("postPanel", !post.ClassListContains("blocked-tab"), UpdateTextForPost));
        result.RegisterCallback<ClickEvent>(callback => ChangeVisibility("resultPanel", !result.ClassListContains("blocked-tab"), UpdateResultPanel));
    }

    public void StartContent(List<Posts> postsToShow)
    {
        ScrollView newsPanel = document.rootVisualElement.Q<ScrollView>("newsPanel");
        newsPanel.Clear();
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
        // webArticle.style.display = DisplayStyle.None;
        socialMedia.style.display = DisplayStyle.Flex;
        // element.style.display = DisplayStyle.Flex;
        // if (element.Equals(webArticle))
        // {
        //     Label articleTitle = webArticle.Q<Label>("articleTitle");
        //     SetLabelText(articleTitle,
        //     DatabaseManager.Instance.GetOrganizationName(post.OrganizationId.Value)
        //     + "\n" + DatabaseManager.Instance.GetOrganizations(post.OrganizationId.Value).SocialMediaTag
        //     + "\n\n" + DatabaseManager.Instance.GetOrganizations(post.OrganizationId.Value).Description,
        //     post.Id);

        //     Label articleContent = webArticle.Q<Label>("articleContent");
        //     SetLabelText(articleContent, post.Content, post.Id);
        // }
        // else
        // {
        Label personInfo = socialMedia.Q<Label>("personInfo");
        string titleInfo = element.Equals(webArticle) ?
        DatabaseManager.Instance.GetOrganizationName(post.OrganizationId.Value)
        + "\n" + DatabaseManager.Instance.GetOrganizations(post.OrganizationId.Value).SocialMediaTag
        + "\n\n" + DatabaseManager.Instance.GetOrganizations(post.OrganizationId.Value).Description
        : DatabaseManager.Instance.GetCharacterName(post.CharacterId.Value)
        + "\n" + DatabaseManager.Instance.GetCharacter(post.CharacterId.Value).SocialMediaTag
        + "\n\n" + DatabaseManager.Instance.GetCharacter(post.CharacterId.Value).SocialMediaDescription;
        SetLabelText(
            personInfo, titleInfo, post.Id);

        Label postsText = socialMedia.Q<Label>("postsText");
        postsText.text = "";
        foreach (Posts p in element.Equals(webArticle)
        ? DatabaseManager.Instance.GetPostsForOrganization(post.OrganizationId.Value)
        : DatabaseManager.Instance.GetPostsForCharacter(post.CharacterId.Value))
        {
            if (!postsText.text.Contains(p.Content))
                SetLabelText(postsText, p.Content + "\n\n", p.Id);
        }
        // }
    }
    private void SetLabelText(Label label, string text, int postId)
    {
        label.text = text;
        label.userData = postId;
        if (originalTextPerLabel.ContainsKey(label))
        {
            originalTextPerLabel[label] = text;
        }
        else
        {
            originalTextPerLabel.Add(label, text);
        }
        RegisterTextLabelCallbacks(document.rootVisualElement);
        LoadHighlights(label);
    }

    private void Update()
    {
        if (wordsPlayerSelected.Any() && note.ClassListContains("blocked-tab"))
            PlayUnlockEffects(note);
        if (document.rootVisualElement.Q<ScrollView>("wordsHolder").childCount > 0 && post.ClassListContains("blocked-tab"))
            PlayUnlockEffects(post);
    }
    private void PlayUnlockEffects(VisualElement tab)
    {
        if (tab.ClassListContains("blocked-tab"))
            tab.RemoveFromClassList("blocked-tab");
        unlockTabSource.Play();
    }

    public void ChangeVisibility(string panelName, bool condition, Action? task = null)
    {
        var root = document.rootVisualElement;
        string[] panelNames = { "postPanel", "newsPanel", "resultPanel", "notePanel" };
        if (panelName != "postPanel")
            postWordsPanel.style.display = DisplayStyle.None;
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
                DatabaseManager.Instance.InsertSelectedWord(activeEventId, (int)textLabel.userData, clickedWord);
            }
            else
            {
                OnTextUnclicked(clickedWord, textLabel);
            }
            UpdateHighlights(textLabel);
            LoadSelectedWordsForEvent(GameManager.instance.ActiveEventId);
        }
    }


    private void OnTextUnclicked(string unclickedWord, Label textLabel)
    {
        int postId = (int)textLabel.userData;
        int activeEventId = GameManager.instance.ActiveEventId;
        wordsPlayerSelected.Remove(unclickedWord);
        clickedWordsPerLabel[textLabel].Remove(unclickedWord);
        DatabaseManager.Instance.RemoveSelectedWord(activeEventId, postId, unclickedWord);

        UpdateHighlights(textLabel);
    }
    private string GetWordAtPosition(Label textLabel, Vector2 clickPosition)
    {
        float effectiveX = clickPosition.x - textLabel.resolvedStyle.paddingLeft;
        float effectiveY = clickPosition.y - textLabel.resolvedStyle.paddingTop;
        Debug.Log($"ClickPosition: {clickPosition}, EffectiveX: {effectiveX}, EffectiveY: {effectiveY}");

        string text = originalTextPerLabel.ContainsKey(textLabel) ? originalTextPerLabel[textLabel] : textLabel.text;

        string[] words = System.Text.RegularExpressions.Regex.Split(text, @"\s+");

        float availableWidth = textLabel.contentRect.width
                               - textLabel.resolvedStyle.paddingLeft
                               - textLabel.resolvedStyle.paddingRight;
        float spaceWidth = textLabel.MeasureTextSize(" ", 0, VisualElement.MeasureMode.Undefined,
                                                       0, VisualElement.MeasureMode.Undefined).x;
        Debug.Log($"AvailableWidth: {availableWidth}, SpaceWidth: {spaceWidth}");

        List<(string word, float width)> measuredWords = new List<(string, float)>();
        foreach (string word in words)
        {
            float w = textLabel.MeasureTextSize(word, 0, VisualElement.MeasureMode.Undefined,
                                                  0, VisualElement.MeasureMode.Undefined).x;
            measuredWords.Add((word, w));
            Debug.Log($"Word: {word}, Width: {w}");
        }

        List<List<(string word, float width)>> lines = new List<List<(string, float)>>();
        List<(string word, float width)> currentLine = new List<(string, float)>();
        float currentLineWidth = 0f;
        foreach (var mw in measuredWords)
        {
            if (currentLine.Count == 0)
            {
                currentLine.Add(mw);
                currentLineWidth = mw.width;
            }
            else if (currentLineWidth + spaceWidth + mw.width <= availableWidth)
            {
                currentLine.Add(mw);
                currentLineWidth += spaceWidth + mw.width;
            }
            else
            {
                lines.Add(new List<(string, float)>(currentLine));
                currentLine.Clear();
                currentLine.Add(mw);
                currentLineWidth = mw.width;
            }
        }
        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }

        Debug.Log($"Total Lines: {lines.Count}");
        for (int i = 0; i < lines.Count; i++)
        {
            string lineContent = string.Join(" ", lines[i].Select(item => item.word));
            Debug.Log($"Line {i}: {lineContent}");
        }

        float computedLineHeight = textLabel.resolvedStyle.fontSize * 1.2f;
        if (lines.Count > 0)
        {
            string firstLineText = string.Join(" ", lines[0].Select(item => item.word));
            computedLineHeight = textLabel.MeasureTextSize(firstLineText, 0, VisualElement.MeasureMode.Undefined,
                                                             0, VisualElement.MeasureMode.Undefined).y;
        }
        Debug.Log($"ComputedLineHeight: {computedLineHeight}");

        int lineIndex = Mathf.FloorToInt(effectiveY / computedLineHeight);
        if (lineIndex < 0 || lineIndex >= lines.Count)
        {
            Debug.Log("Click outside of text lines.");
            return "";
        }
        var clickedLine = lines[lineIndex];

        float lineWidth = clickedLine.Sum(item => item.width) + spaceWidth * (clickedLine.Count - 1);
        float lineOffset = 0f;
        var align = textLabel.resolvedStyle.unityTextAlign;
        if (align == TextAnchor.MiddleCenter || align == TextAnchor.UpperCenter || align == TextAnchor.LowerCenter)
        {
            lineOffset = (availableWidth - lineWidth) / 2f;
        }
        else if (align == TextAnchor.MiddleRight || align == TextAnchor.UpperRight || align == TextAnchor.LowerRight)
        {
            lineOffset = availableWidth - lineWidth;
        }
        Debug.Log($"LineOffset: {lineOffset}, LineWidth: {lineWidth}");

        float tolerance = 3.5f;
        for (int i = 0; i < clickedLine.Count; i++)
        {
            string prefix = string.Join(" ", clickedLine.Take(i).Select(item => item.word));
            if (!string.IsNullOrEmpty(prefix))
                prefix += " ";
            float prefixWidth = textLabel.MeasureTextSize(prefix, 0, VisualElement.MeasureMode.Undefined,
                                                            0, VisualElement.MeasureMode.Undefined).x;
            float wordStartX = lineOffset + prefixWidth;
            float wordWidth = textLabel.MeasureTextSize(clickedLine[i].word, 0, VisualElement.MeasureMode.Undefined,
                                                        0, VisualElement.MeasureMode.Undefined).x;
            Debug.Log($"Word '{clickedLine[i].word}' starts at {wordStartX} with width {wordWidth}");

            // If the effective click X falls within the bounds (with a small tolerance), return this word.
            if (effectiveX >= wordStartX - tolerance && effectiveX <= wordStartX + wordWidth + tolerance)
            {
                Debug.Log($"Detected word: {clickedLine[i].word} (line {lineIndex}, startX: {wordStartX}, width: {wordWidth})");
                return clickedLine[i].word;
            }
        }

        return "";
    }

    private void LoadHighlights(Label textLabel)
    {
        int postId = (int)textLabel.userData;
        List<SelectedWords> selectedWordsList = DatabaseManager.Instance.GetSelectedWordsForPost(postId);
        if (selectedWordsList.Count > 0 && originalTextPerLabel.ContainsKey(textLabel))
        {
            List<string> loadedWords = selectedWordsList.Select(sw => sw.Word).ToList();
            clickedWordsPerLabel[textLabel] = loadedWords;

            string originalText = originalTextPerLabel[textLabel];
            string highlightedText = originalText;
            foreach (SelectedWords sw in selectedWordsList)
            {
                string pattern = $@"\b{Regex.Escape(sw.Word)}\b";
                highlightedText = Regex.Replace(highlightedText, pattern, $"<color=yellow>{sw.Word}</color>");
            }
            textLabel.text = highlightedText.Trim();
        }
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

        postWordsPanel.style.display = DisplayStyle.None;
        note.RemoveFromClassList("blocked-tab");
        post.RemoveFromClassList("blocked-tab");
        document.rootVisualElement.Q("selectedWordsPanel").style.display = DisplayStyle.None;
        document.rootVisualElement.Q("backToFolder").style.display = DisplayStyle.None;
        VisualElement folderListPanel = document.rootVisualElement.Q("folderListPanel");
        folderListPanel.style.display = DisplayStyle.Flex;
        folderListPanel.Clear();

        List<WordFolders> folders = DatabaseManager.Instance.GetFolders();

        foreach (WordFolders folder in folders)
        {
            VisualElement holder = originalAsset.Instantiate().Q("noteNewsFolder");
            holder.style.display = DisplayStyle.Flex;
            holder.style.flexShrink = 1;
            holder.style.flexGrow = 0;
            Button folderButton = holder.Q<Button>("folderButton");
            holder.Q<Label>("folderName").text = folder.FolderName.Substring(0, 8);
            holder.userData = folder.Id;

            folderButton.RegisterCallback<ClickEvent>(evt => LoadSelectedWordsForEvent(folder.Id));

            folderListPanel.Add(holder);
        }
    }

    private void LoadSelectedWordsForEvent(int eventId)
    {
        document.rootVisualElement.Q("folderListPanel").style.display = DisplayStyle.None;
        if (document.rootVisualElement.Q("notePanel").style.display == DisplayStyle.Flex)
        {
            postWordsPanel.style.display = DisplayStyle.Flex;
            document.rootVisualElement.Q("backToFolder").style.display = DisplayStyle.Flex;
            socialMedia.style.display = DisplayStyle.None;
            webArticle.style.display = DisplayStyle.None;

            List<SelectedWords> words = DatabaseManager.Instance.GetSelectedWordsForEvent(eventId);

            VisualElement selectedWordsPanel = document.rootVisualElement.Q("selectedWordsPanel");
            selectedWordsPanel.style.display = DisplayStyle.Flex;
            selectedWordsPanel.Clear();

            ScrollView wordsHolder = document.rootVisualElement.Q<ScrollView>("wordsHolder");
            wordsHolder.Clear();

            foreach (string s in DatabaseManager.Instance.GetFormattedCoreTruth(eventId))
            {
                VisualElement panel = originalAsset.Instantiate().Q("childPostWordsPanel");
                panel.style.display = DisplayStyle.Flex;
                Label coreTruthTitle = panel.Q<Label>("coreTruthTitle");
                coreTruthTitle.text = s + ":";
                VisualElement coreTruthWordsContainer = panel.Q<VisualElement>("wordsPanel");
                coreTruthWordsContainer.Clear();
                panel.RegisterCallback<ClickEvent>(evt =>
                {
                    currentChosenPanel = panel;
                    buttonClickSource.Play();
                });
                wordsHolder.Add(panel);
            }

            foreach (SelectedWords sw in words)
            {
                if (!IsWordInUI(selectedWordsPanel, sw) && !IsWordInUI(currentChosenPanel, sw))
                {
                    Label wordLabel = new Label(sw.Word) { text = sw.Word };
                    selectedWordsPanel.Add(wordLabel);

                    UpdateWordApprovalStatus(wordLabel, sw);

                    wordLabel.RegisterCallback<ClickEvent>(evt =>
                    {
                        if (currentChosenPanel != null)
                        {
                            if (wordLabel.parent == selectedWordsPanel)
                            {
                                sw.IsApproved = true;
                                DatabaseManager.Instance.UpdateEntity(sw);
                                UpdateWordApprovalStatus(wordLabel, sw);
                            }
                            else if (wordLabel.parent == currentChosenPanel)
                            {
                                sw.IsApproved = false;
                                DatabaseManager.Instance.UpdateEntity(sw);
                                UpdateWordApprovalStatus(wordLabel, sw);
                            }
                        }
                    });
                }
            }
        }
    }
    private bool IsWordInUI(VisualElement parentPanel, SelectedWords sw)
    {
        if (parentPanel == null)
            return false;
        foreach (var child in parentPanel.Children())
        {
            if (child is Label wordLabel && wordLabel.text == sw.Word)
            {
                return true;
            }
        }
        return false;
    }
    private void UpdateWordApprovalStatus(Label wordLabel, SelectedWords sw)
    {
        VisualElement selectedWordsPanel = document.rootVisualElement.Q("selectedWordsPanel");
        if (currentChosenPanel != null)
        {
            buttonClickSource.Play();
            if (sw.IsApproved)
            {
                if (wordLabel.parent != currentChosenPanel)
                {
                    selectedWordsPanel.Remove(wordLabel);
                    currentChosenPanel.contentContainer.Add(wordLabel);
                }
            }
            else
            {
                if (wordLabel.parent != selectedWordsPanel)
                {
                    currentChosenPanel.Remove(wordLabel);
                    selectedWordsPanel.Add(wordLabel);
                    UpdateTextForPost();
                }
            }
        }
    }

    private void UpdateTextForPost()
    {
        int activeEventId = GameManager.instance.ActiveEventId;

        List<SelectedWords> approvedWords = DatabaseManager.Instance.GetSelectedWordsForEvent(activeEventId)
                                                        .Where(sw => sw.IsApproved)
                                                        .ToList();
        Label postText = document.rootVisualElement.Q<Label>("postText");
        postText.text = string.Join(" ", approvedWords.Select(sw => sw.Word));
    }
    private void UpdateWidgetsInfo()
    {
        Label popularityLabel = document.rootVisualElement.Q<Label>("popularityLabel");
        popularityLabel.text = "" + DatabaseManager.Instance.GetMedia(1).Readers;
        Label trustLabel = document.rootVisualElement.Q<Label>("trustLabel");
        trustLabel.text = DatabaseManager.Instance.GetMedia(1).Credibility + "/10";
    }
    public void HandleArticle(Articles article)
    {
        Label postText = document.rootVisualElement.Q<Label>("postText");
        postText.text = article.Content;
        result.RemoveFromClassList("blocked-tab");
        PlayUnlockEffects(result);
        UpdateWidgetsInfo();
    }
    private void UpdateResultPanel()
    {
        Label resultPopularityLabel = document.rootVisualElement.Q<Label>("resultPopularityLabel");
        resultPopularityLabel.text = DatabaseManager.Instance.GetMedia(1).Readers.ToString();

        Label resultTrustLabel = document.rootVisualElement.Q<Label>("resultTrustLabel");
        resultTrustLabel.text = DatabaseManager.Instance.GetMedia(1).Credibility + "/10";

        int targetPopularity = DatabaseManager.Instance.GetMedia(1).Readers;
        float targetTrust = DatabaseManager.Instance.GetMedia(1).Credibility;

        StartCoroutine(AnimateLabel(resultTrustLabel, targetTrust, "/10"));
        StartCoroutine(AnimateLabel(resultPopularityLabel, targetPopularity));

        Label noteByAI = document.rootVisualElement.Q<Label>("noteByAI");
        // noteByAI.text += ;
    }

    private IEnumerator AnimateLabel(Label label, float targetValue, string suffix = "")
    {
        float currentValue = 0f;
        float increment = targetValue / 8f;
        for (int i = 0; i < 8; i++)
        {
            currentValue += increment;
            if (currentValue > targetValue)
                currentValue = targetValue;

            buttonClickSource.Play();
            label.text = Mathf.RoundToInt(currentValue).ToString() + suffix;

            yield return new WaitForSeconds(0.25f);
        }
    }

    //buttons effects
    private void PlayClickSound(ClickEvent e)
    {
        buttonClickSource.Play();
    }
}