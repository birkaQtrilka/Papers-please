using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    public GameObject pausePanel;
    [SerializeField] GameObject _paperRoot;
    [SerializeField] SelectManager _paperSelection;
    [SerializeField] GameObject _mainPage;
    [SerializeField] GameObject _optionsPage;
    [SerializeField] TMPro.TextMeshPro _volumeText;

    [Header("Optional effects")]
    [SerializeField] SceneTransitionManager sceneTransitionManager;

    [Header("Optional UI Buttons")]
    public UnityEngine.UI.Button resumeButton;
    public UnityEngine.UI.Button quitButton;
    public GameObject[] objectsToDisable;

    [Header("Audio")]
    [Tooltip("Leave empty to collect current scene sources on each pause.")]
    public AudioSource[] audioSources;
    [SerializeField, Range(0.01f, 1f)] float _volumeStep = 0.1f;

    public static bool IsPaused { get; private set; }

    UnityEngine.InputSystem.InputAction _pauseAction;
    readonly List<AudioSource> _pausedSources = new List<AudioSource>();
    bool[] _previousActiveStates;
    float _previousTimeScale;
    bool _previousListenerPause;
    bool _previousCursorVisible;
    CursorLockMode _previousCursorLock;
    bool _ownsPause;
    bool _quitting;

    void Awake()
    {
        _pauseAction = new UnityEngine.InputSystem.InputAction("Pause", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/escape");
        if (pausePanel != null) pausePanel.SetActive(false);
        if (_paperRoot != null) _paperRoot.SetActive(false);
        ShowMainPage();
    }

    void OnEnable()
    {
        _pauseAction.performed += OnPauseInput;
        _pauseAction.Enable();
        if (_paperSelection != null) _paperSelection.OnFieldClick.AddListener(OnPaperClicked);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    void OnDisable()
    {
        _pauseAction.performed -= OnPauseInput;
        _pauseAction.Disable();
        if (_paperSelection != null) _paperSelection.OnFieldClick.RemoveListener(OnPaperClicked);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        if (_ownsPause) Resume();
    }

    void OnDestroy()
    {
        _pauseAction.Dispose();
        if (_ownsPause) Resume();
    }

    void OnPauseInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_quitting) return;
        if (_ownsPause) Resume();
        else Pause();
    }

    void OnPaperClicked(Collider col)
    {
        if (!_ownsPause || _quitting || col == null || _paperRoot == null || !col.transform.IsChildOf(_paperRoot.transform)) return;
        switch (col.gameObject.name)
        {
            case "Resume": Resume(); break;
            case "Options": ShowOptionsPage(); break;
            case "Quit": QuitGame(); break;
            case "VolumeDown": ChangeVolume(-_volumeStep); break;
            case "VolumeUp": ChangeVolume(_volumeStep); break;
            case "Back": ShowMainPage(); break;
        }
    }

    public void Pause()
    {
        if (_ownsPause || IsPaused || _quitting) return;
        _ownsPause = true;
        IsPaused = true;
        _previousTimeScale = Time.timeScale;
        _previousCursorLock = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;
        _previousListenerPause = AudioListener.pause;
        var sources = audioSources != null && audioSources.Length > 0
            ? audioSources : Object.FindObjectsByType<AudioSource>();
        _pausedSources.Clear();
        foreach (var source in sources)
        {
            if (source == null || !source.isPlaying) continue;
            _pausedSources.Add(source);
            source.Pause();
        }
        // Also suspend sources started while paused; existing individually paused sources remain untouched.
        AudioListener.pause = true;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _previousActiveStates = new bool[objectsToDisable != null ? objectsToDisable.Length : 0];
        for (int i = 0; i < _previousActiveStates.Length; i++)
        {
            var item = objectsToDisable[i];
            if (item == null) continue;
            _previousActiveStates[i] = item.activeSelf;
            item.SetActive(false);
        }
        ShowMainPage();
        if (pausePanel != null) pausePanel.SetActive(true);
        if (_paperRoot != null) _paperRoot.SetActive(true);
    }

    public void Resume()
    {
        if (!_ownsPause) return;
        _ownsPause = false;
        IsPaused = false;
        if (_paperSelection != null) _paperSelection.ClearSelection();
        if (_paperRoot != null) _paperRoot.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        for (int i = 0; i < _previousActiveStates.Length; i++)
        {
            if (objectsToDisable[i] != null) objectsToDisable[i].SetActive(_previousActiveStates[i]);
        }
        Time.timeScale = _previousTimeScale;
        Cursor.lockState = _previousCursorLock;
        Cursor.visible = _previousCursorVisible;
        AudioListener.pause = _previousListenerPause;
        foreach (var source in _pausedSources)
            if (source != null) source.UnPause();
        _pausedSources.Clear();
    }

    public void ShowMainPage()
    {
        if (_paperSelection != null) _paperSelection.ClearSelection();
        if (_optionsPage != null) _optionsPage.SetActive(false);
        if (_mainPage != null) _mainPage.SetActive(true);
        RefreshVolume();
    }

    public void ShowOptionsPage()
    {
        if (!_ownsPause) return;
        if (_paperSelection != null) _paperSelection.ClearSelection();
        if (_mainPage != null) _mainPage.SetActive(false);
        if (_optionsPage != null) _optionsPage.SetActive(true);
        RefreshVolume();
    }

    public void ChangeVolume(float amount)
    {
        if (!_ownsPause || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        AudioListener.volume = Mathf.Clamp01(Mathf.Round((AudioListener.volume + amount) * 100f) / 100f);
        RefreshVolume();
    }

    void RefreshVolume()
    {
        if (_volumeText != null) _volumeText.SetText("{0}%", Mathf.Round(AudioListener.volume * 100f));
    }

    void QuitGame()
    {
        if (_quitting) return;
        _quitting = true;
        Resume();
        // The existing transition uses scaled time, so it must run before exiting.
        if (sceneTransitionManager != null)
        {
            if (Time.timeScale <= 0) Time.timeScale = 1;
            sceneTransitionManager.FadeIn(QuitApplication);
        }
        else QuitApplication();
    }

    public static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
