using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuManager : MonoBehaviour
{
    [SerializeField] SelectManager startButton;
    [SerializeField] SceneTransitionManager sceneTransitionManager;
    private void OnEnable()
    {
        startButton.OnFieldClick.AddListener(OnStartButtonClicked);
    }
    private void OnDisable()
    {
        startButton.OnFieldClick.RemoveListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked(Collider col)
    {
        switch (col.gameObject.name)
        {
            case "Start":
                sceneTransitionManager.FadeIn(() => SceneManager.LoadScene(1));
                break;
            case "Quit":
                Application.Quit();
                break;
            default:
                break;
        }
    }

}
