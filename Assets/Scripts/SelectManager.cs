using UnityEngine;
using UnityEngine.Events;

public class SelectManager : MonoBehaviour
{
    [Tooltip("Camera from which perspective to shoot rays to the mouse")]
    [SerializeField] Camera renderCamera;
    [SerializeField] LayerMask _paperLayer;
    [SerializeField] AudioSource selectSound;

    [SerializeField] RectTransform renderImageRect;

    GameObject _lastSelection;

    public UnityEvent<Collider> OnFieldClick;

    void Update()
    {
        Ray ray = GetWorldRay();
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _paperLayer))
        {
            if (_lastSelection != null) _lastSelection.SetActive(false);
            var copy = _lastSelection;
            _lastSelection = hit.collider.transform.GetChild(0).gameObject;
            _lastSelection.SetActive(true);

            if (copy != _lastSelection)
            {
                selectSound.Play();
            }

            if (Input.GetMouseButtonDown(0))
            {

                OnFieldClick?.Invoke(hit.collider);
            }

        }
        else
        {
            if (_lastSelection != null)
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

}
