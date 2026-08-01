using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    [Tooltip("Assign your Pause Panel GameObject here")]
    public GameObject pausePanel;

    [Header("Optional effects")]
    [SerializeField] SceneTransitionManager sceneTransitionManager;

    [Header("Optional UI Buttons")]
    public Button resumeButton;
    public Button quitButton;

    public GameObject[] objectsToDisable; 

    [Header("Audio")]
    [Tooltip("Leave empty to auto-find all AudioSources in the scene")]
    public AudioSource[] audioSources;

    public static bool IsPaused { get; private set; } = false;




    void Start()
    {
        // Make sure the panel starts hidden
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Wire up optional buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Auto-collect audio sources if none were assigned
        if (audioSources == null || audioSources.Length == 0)
            audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;

        // Freeze time
        Time.timeScale = 0f;

        // Silence all audio
        SetAudioPaused(true);

        // Show the pause panel
        if (pausePanel != null)
            pausePanel.SetActive(true);
        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            if (objectsToDisable[i] != null)
                objectsToDisable[i].SetActive(false);
        }
    }

    public void Resume()
    {
        IsPaused = false;

        // Restore time
        Time.timeScale = 1f;

        // Restore audio
        SetAudioPaused(false);

        // Hide the pause panel
        if (pausePanel != null)
            pausePanel.SetActive(false);
        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            if (objectsToDisable[i] != null)
                objectsToDisable[i].SetActive(true);
        }
    }

    void SetAudioPaused(bool paused)
    {
        // Refresh list in case new AudioSources were spawned
        if (audioSources == null || audioSources.Length == 0)
            audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        foreach (AudioSource src in audioSources)
        {
            if (src == null) continue;

            if (paused)
                src.Pause();
            else
                src.UnPause();
        }
    }

    void QuitGame()
    {
        if(sceneTransitionManager != null)
        {
            Resume();
            sceneTransitionManager.FadeIn(() => QuitApplication());
        }
        else
        {
            QuitApplication();
        }

    }

    public static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}