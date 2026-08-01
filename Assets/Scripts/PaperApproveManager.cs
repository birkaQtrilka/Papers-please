using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PaperApproveManager : MonoBehaviour
{
    [Tooltip("Camera from which perspective to shoot rays to the mouse")]
    [SerializeField] Camera renderCamera;
    [SerializeField] LayerMask _paperLayer;
    [SerializeField] SceneTransitionManager sceneTransitionManager;
    [SerializeField] Bomb bomb;
    [SerializeField] float explosionDuration;
    [SerializeField] AudioSource selectSound;

    public UnityEvent<bool> PaperEvaluated;

    [SerializeField] RectTransform renderImageRect;

    GameObject _lastSelection;

    void Update()
    {
        Ray ray = GetWorldRay();
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _paperLayer))
        {
            if(_lastSelection != null) _lastSelection.SetActive(false);
            var copy = _lastSelection;
            _lastSelection = hit.collider.transform.GetChild(0).gameObject;
            _lastSelection.SetActive(true);

            if(copy != _lastSelection)
            {
                selectSound.Play();
            }

            if (Input.GetMouseButtonDown(0)) {

                OnFieldCLick(hit.collider.gameObject.name);

            }

        }
        else
        {
            if(_lastSelection != null)
            {
                _lastSelection.SetActive(false);
                _lastSelection = null;
            }
        }
    }

    Ray GetWorldRay()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            renderImageRect,
            Input.mousePosition,
            null,
            out Vector2 localPoint
        );

        Rect rect = renderImageRect.rect;

        // Convert to UV (0–1)
        Vector2 uv = new Vector2(
            (localPoint.x - rect.x) / rect.width,
            (localPoint.y - rect.y) / rect.height
        );

        // Convert to pixel coords of render texture
        Vector2 pixelPos = new Vector2(
            uv.x * renderCamera.pixelWidth,
            uv.y * renderCamera.pixelHeight
        );

        return renderCamera.ScreenPointToRay(pixelPos);
    }

    void OnFieldCLick(string fieldName)
    {
        switch (fieldName)
        {
            case "Approve":
                PaperEvaluated?.Invoke(true);
                break;
            case "Disapprove":
                PaperEvaluated?.Invoke(false);
                break;
            case "Reset":
                OnResetClick();
                break;
            case "ResetBomb":
                OnResetBombClick();
                break;
            case "Quit":
                OnQuitClick();
                break;
        }
    }

    void OnResetBombClick()
    {
        StartCoroutine(BombExplosionCoroutine(() =>
        {
            if (sceneTransitionManager != null)
            {
                sceneTransitionManager.FadeIn(() => SceneManager.LoadScene(1));
            }
            else
                SceneManager.LoadScene(1);
        }));
    }
    void OnResetClick()
    {
            if (sceneTransitionManager != null)
            {
                sceneTransitionManager.FadeIn(() => SceneManager.LoadScene(1));
            }
            else
                SceneManager.LoadScene(1);
    }

    void OnQuitClick()
    {
        StartCoroutine(BombExplosionCoroutine(() =>
        {
            if(sceneTransitionManager != null)
            {
                sceneTransitionManager.FadeIn(() => PauseManager.QuitApplication());
            }
            else
                PauseManager.QuitApplication();
        }));
    }

    IEnumerator BombExplosionCoroutine(Action action)
    {
        bomb.Explode();
        yield return new WaitForSeconds(explosionDuration);
        action();
    }
}
