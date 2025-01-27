using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
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
    private List<Button> goToBrowserButtons = new List<Button>();

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

        goToBrowserButtons = root.Query<Button>(name: "goBackButton").ToList();
        goToBrowserButtons.ForEach(b => b.RegisterCallback<ClickEvent>(PlayClickSound));
    }
    private void Start()
    {
        TextGen();
    }
    private void TextGen()
    {
        int randomAmount = Random.Range(1, 4);
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
            fillableNewsHolder.RegisterCallbackOnce<ClickEvent>(callback => ChangeDisplayOfPanel(callback, sourcePanel, "linksHolder", "sometext" + i));
            newsPanel.Add(fillableNewsHolder);
        }
    }
    private void OnDisable()
    {
        goToBrowserButtons.ForEach(b => b.UnregisterCallback<ClickEvent>(PlayClickSound));
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
                Label textToPut = new Label();
                if (textToPut != null)
                    textToPut.text = contentToPutIn;
                textToPut.style.marginTop = new StyleLength(new Length(5, LengthUnit.Percent));
                textToPut.style.color = Color.black;
                textToPut.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
                textToPut.style.height = new StyleLength(new Length(15, LengthUnit.Percent));
                listView.hierarchy.Add(textToPut);
            }
        }
    }
    //TODO: collect words clicked by player to list
}