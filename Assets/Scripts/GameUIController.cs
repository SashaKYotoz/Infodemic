using UnityEngine;
using UnityEngine.UIElements;

public class GameUIController : MonoBehaviour
{
    private Label contentText;
    
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        contentText = root.Q<Label>("contentText");

        var tab1 = root.Q<Button>("news");
        var tab2 = root.Q<Button>("post");
        var tab3 = root.Q<Button>("note");
        var tab4 = root.Q<Button>("result");

        tab1.clicked += () => UpdateContent("Tab 1 Content");
        tab2.clicked += () => UpdateContent("Tab 2 Content");
        tab3.clicked += () => UpdateContent("Tab 3 Content");
        tab4.clicked += () => UpdateContent("Tab 4 Content");
    }

    private void UpdateContent(string text)
    {
        contentText.text = text;
    }
}