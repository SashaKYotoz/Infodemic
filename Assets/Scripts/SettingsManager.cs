using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsManager : MonoBehaviour
{
    private UIDocument document;
    private AudioSource audioSource;
    private VisualElement mainMenu, settingsPanel, creditsPanel;
    private Button settingsButton, creditsButton, backButtonSettings, backButtonCredits;
    private Slider gammaSlider;
    private DropdownField resolutionDropdown;
    private Toggle vsyncToggle;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        document = GetComponent<UIDocument>();

        // Panels
        mainMenu = document.rootVisualElement.Q("Panel");
        settingsPanel = document.rootVisualElement.Q("SettingsPanel");
        creditsPanel = document.rootVisualElement.Q("CreditsPanel");

        // Buttons
        settingsButton = document.rootVisualElement.Q<Button>("SettingsButton");
        creditsButton = document.rootVisualElement.Q<Button>("CreditsButton");
        backButtonSettings = document.rootVisualElement.Q<Button>("BackButtonSettings");
        backButtonCredits = document.rootVisualElement.Q<Button>("BackButtonCredits");

        // Settings Elements
        gammaSlider = document.rootVisualElement.Q<Slider>("GammaSlider");
        resolutionDropdown = document.rootVisualElement.Q<DropdownField>("ResolutionDropdown");
        vsyncToggle = document.rootVisualElement.Q<Toggle>("VsyncToggle");

        // Event Listeners
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