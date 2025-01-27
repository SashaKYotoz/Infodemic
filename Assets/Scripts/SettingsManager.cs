using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    private UIDocument document;
    private AudioSource audioSource;
    private VisualElement mainMenu, settingsPanel, creditsPanel, container;
    private Button startGameButton, settingsButton, creditsButton, backButtonSettings, backButtonCredits;
    private Slider gammaSlider;
    private DropdownField resolutionDropdown;
    private Toggle vsyncToggle;
    private float scrollValue = 0;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        document = GetComponent<UIDocument>();

        // Panels
        mainMenu = document.rootVisualElement.Q("Panel");
        settingsPanel = document.rootVisualElement.Q("SettingsPanel");
        creditsPanel = document.rootVisualElement.Q("CreditsPanel");
        container = document.rootVisualElement.Q("Container");

        // Buttons
        startGameButton = document.rootVisualElement.Q<Button>("StartButton");
        settingsButton = document.rootVisualElement.Q<Button>("SettingsButton");
        creditsButton = document.rootVisualElement.Q<Button>("CreditsButton");
        backButtonSettings = document.rootVisualElement.Q<Button>("BackButtonSettings");
        backButtonCredits = document.rootVisualElement.Q<Button>("BackButtonCredits");

        // Settings Elements
        gammaSlider = document.rootVisualElement.Q<Slider>("GammaSlider");
        resolutionDropdown = document.rootVisualElement.Q<DropdownField>("ResolutionDropdown");
        vsyncToggle = document.rootVisualElement.Q<Toggle>("VsyncToggle");

        // Event Listeners
        startGameButton.RegisterCallback<ClickEvent>(LoadGame);
        settingsButton.clicked += () => { PlaySound(); ShowSettings(); };
        creditsButton.clicked += () => { PlaySound(); ShowCredits(); };
        backButtonSettings.clicked += () => { PlaySound(); ShowMainMenu(); };
        backButtonCredits.clicked += () => { PlaySound(); ShowMainMenu(); };
        gammaSlider.RegisterValueChangedCallback(evt => { PlaySound(); AdjustGamma(evt.newValue); });
        resolutionDropdown.RegisterValueChangedCallback(evt => { PlaySound(); ChangeResolution(evt.newValue); });
        vsyncToggle.RegisterValueChangedCallback(evt => { PlaySound(); ToggleVSync(evt.newValue); });

        // Initialize UI
        settingsPanel.style.display = DisplayStyle.None;
        creditsPanel.style.display = DisplayStyle.None;
        InitializeResolutionDropdown();
    }
    private void Update()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel") * 2f;

        scrollValue += scrollInput;

        scrollValue = Mathf.Clamp(scrollValue, -2, 2);
        float zoom = Mathf.Lerp(1f, 1.4f, (scrollValue - -2) / (2 - -2));
        mainMenu.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(zoom * 100, LengthUnit.Percent), new Length(zoom * 100, LengthUnit.Percent)));
        settingsPanel.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(zoom * 100, LengthUnit.Percent), new Length(zoom * 100, LengthUnit.Percent)));
        creditsPanel.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(zoom * 100, LengthUnit.Percent), new Length(zoom * 100, LengthUnit.Percent)));
    }
    private void OnDisable()
    {
        startGameButton.UnregisterCallback<ClickEvent>(LoadGame);
    }
    private void LoadGame(ClickEvent clickEvent)
    {
        container.style.visibility = Visibility.Hidden;
        StartCoroutine(StartGame());
    }

    private IEnumerator StartGame()
    {
        float startValue = Mathf.Lerp(1f, 1.4f, (scrollValue - -2) / (2 - -2));
        float endValue = 2f;
        float duration = 4f;
        float timeElapsed = 0f;
        Color startColor = mainMenu.style.backgroundColor.value;
        Color endColor = new Color(0f, 0f, 0.5f);
        while (timeElapsed < duration)
        {
            float value = Mathf.Lerp(startValue, endValue, timeElapsed / duration);
            mainMenu.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(value * 100, LengthUnit.Percent), new Length(value * 100, LengthUnit.Percent)));
            mainMenu.style.color = new StyleColor(Color.Lerp(startColor, endColor, timeElapsed / duration));
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        mainMenu.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(200, LengthUnit.Percent), new Length(200, LengthUnit.Percent)));
        mainMenu.style.color = endColor;
        SceneManager.LoadScene("Game");
        SceneManager.UnloadSceneAsync("Management");
    }

    private void PlaySound()
    {
        audioSource.Play();
    }

    private void ShowSettings()
    {
        mainMenu.style.display = DisplayStyle.None;
        settingsPanel.style.display = DisplayStyle.Flex;
    }

    private void ShowCredits()
    {
        mainMenu.style.display = DisplayStyle.None;
        creditsPanel.style.display = DisplayStyle.Flex;
    }

    private void ShowMainMenu()
    {
        settingsPanel.style.display = DisplayStyle.None;
        creditsPanel.style.display = DisplayStyle.None;
        mainMenu.style.display = DisplayStyle.Flex;
    }

    private void AdjustGamma(float value)
    {
        RenderSettings.ambientLight = Color.white * value;
    }

    private void ChangeResolution(string resolution)
    {
        string[] res = resolution.Split('x');
        if (res.Length == 2 && int.TryParse(res[0], out int width) && int.TryParse(res[1], out int height))
        {
            Screen.SetResolution(width, height, Screen.fullScreen);
        }
    }

    private void ToggleVSync(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;
    }

    private void InitializeResolutionDropdown()
    {
        List<string> resolutions = new List<string>
        {
            "1920x1080", "1600x900", "1280x720", "1024x768"
        };
        resolutionDropdown.choices = resolutions;
        resolutionDropdown.value = resolutions[0];

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            resolutionDropdown.SetEnabled(false);
        }
    }
}