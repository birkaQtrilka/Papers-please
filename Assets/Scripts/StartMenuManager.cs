using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuManager : MonoBehaviour
{
    [SerializeField] Button startButton;
    [SerializeField] SceneTransitionManager sceneTransitionManager;
    private void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        sceneTransitionManager.FadeIn(() =>SceneManager.LoadScene(1));
    }

}
