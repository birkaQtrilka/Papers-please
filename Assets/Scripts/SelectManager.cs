using UnityEngine;
using UnityEngine.Events;

public class SelectManager : MonoBehaviour
{
    [Tooltip("Camera from which perspective to shoot rays to the mouse")]
    [SerializeField] Camera renderCamera;
    [SerializeField] LayerMask _paperLayer;
    [SerializeField] AudioSource selectSound;
    [SerializeField] bool enableDisableSelection = true;
    [SerializeField] RectTransform renderImageRect;

    GameObject _lastSelection;
    UnityEngine.InputSystem.InputAction _pointerAction;
    UnityEngine.InputSystem.InputAction _clickAction;
    UnityEngine.UI.RawImage _renderImage;
    Camera _uiCamera;

    public UnityEvent<Collider> OnFieldClick = new UnityEvent<Collider>();
    public UnityEvent<GameObject> OnFieldEnter = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> OnFieldExit = new UnityEvent<GameObject>();

    void Awake()
    {
        _pointerAction = new UnityEngine.InputSystem.InputAction("MenuPointer", UnityEngine.InputSystem.InputActionType.Value, "<Pointer>/position");
        _clickAction = new UnityEngine.InputSystem.InputAction("MenuClick", UnityEngine.InputSystem.InputActionType.Button, "<Mouse>/leftButton");
        UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(_clickAction, "<Touchscreen>/primaryTouch/press");
        if (renderImageRect != null)
        {
            _renderImage = renderImageRect.GetComponent<UnityEngine.UI.RawImage>();
            var canvas = renderImageRect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _uiCamera = canvas.worldCamera;
        }
    }

    void OnEnable()
    {
        _clickAction.performed += OnClick;
        _pointerAction.Enable();
        _clickAction.Enable();
    }

    void OnDisable()
    {
        _clickAction.performed -= OnClick;
        _clickAction.Disable();
        _pointerAction.Disable();
        ClearSelection();
    }

    void OnDestroy()
    {
        _clickAction.Dispose();
        _pointerAction.Dispose();
    }

    void Update()
    {
        if (!TryGetHit(out RaycastHit hit))
        {
            ClearSelection();
            return;
        }

        var selection = hit.collider.transform.childCount > 0
            ? hit.collider.transform.GetChild(0).gameObject : null;
        if (_lastSelection == selection) return;
        ClearSelection();
        _lastSelection = selection;
        if (_lastSelection == null) return;
        if (enableDisableSelection) _lastSelection.SetActive(true);
        if (selectSound != null) selectSound.Play();
        OnFieldEnter?.Invoke(_lastSelection);
    }

    void OnClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (TryGetHit(out RaycastHit hit)) OnFieldClick?.Invoke(hit.collider);
    }

    public void ClearSelection()
    {
        if (_lastSelection == null) return;
        if (enableDisableSelection) _lastSelection.SetActive(false);
        OnFieldExit?.Invoke(_lastSelection);
        _lastSelection = null;
    }

    bool TryGetHit(out RaycastHit hit)
    {
        hit = default;
        if (renderCamera == null || renderImageRect == null || !renderImageRect.gameObject.activeInHierarchy)
            return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(renderImageRect,
                _pointerAction.ReadValue<Vector2>(), _uiCamera, out Vector2 point)) return false;
        var rect = renderImageRect.rect;
        if (rect.width <= 0 || rect.height <= 0 || !rect.Contains(point)) return false;
        var uv = new Vector2((point.x - rect.x) / rect.width, (point.y - rect.y) / rect.height);
        var uvRect = _renderImage != null ? _renderImage.uvRect : new Rect(0, 0, 1, 1);
        uv = new Vector2(uvRect.x + uv.x * uvRect.width, uvRect.y + uv.y * uvRect.height);
        var ray = renderCamera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0));
        return Physics.Raycast(ray, out hit, renderCamera.farClipPlane, _paperLayer, QueryTriggerInteraction.Ignore);
    }



}
