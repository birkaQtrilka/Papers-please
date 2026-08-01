using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Vector3 offset;
    public float minAngle = -20f;
    public float maxAngle = 35f;

    public bool constraintX = false;
    public float xAngleRange = 120f;
    public float constraintMiddle = 0f;

    public bool isCinematic;
    Vector2 turn;
    public Camera cam { get; private set; }

    public float targetFOV = 60;
    public float currentFOV
    {
        get => cam.fieldOfView;
    }
    [SerializeField] Transform xRotator;
    [SerializeField] Transform yRotator;
    [SerializeField] Transform Movement;
    [SerializeField] Transform RotationBase;

    [SerializeField] float fovSpeed = 3;
    [SerializeField] 
    float deadzone = 0.05f;

    [Range(0f, 1f)]
    public float cameraSmoothness = 0.5f;
    public float sensitivity;

    [Range(0f, 1f)]
    public float movementSmoothness = 0.5f;
    [SerializeField] bool lockCursor = true;
    private void Start()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null)
            UnityEngine.Debug.Log("Camera not found");
        else
            targetFOV = cam.fieldOfView;
        ReadRotation();
        if (lockCursor)
        { 
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    public void SetViewRotation(Vector2 rotation)
    {
        if (xRotator == yRotator)
        {
            xRotator.localRotation = Quaternion.Euler(-rotation.y, rotation.x, 0);
        }
        else
        {
            xRotator.localRotation = Quaternion.Euler(-rotation.y, 0, 0);
            yRotator.localRotation = Quaternion.Euler(0, rotation.x, 0);
        }
    }
    public void ReadRotation()
    {
        turn.x = yRotator.rotation.eulerAngles.y;
        turn.y = -yRotator.rotation.eulerAngles.x;

        if (turn.y < -90)
            turn.y += 360;
        if (turn.y > 90)
            turn.y -= 360;
    }
    Vector2 mouseDelta;
    private void Update()
    {
        mouseDelta.x = Input.GetAxis("Mouse X");
        mouseDelta.y = Input.GetAxis("Mouse Y");


        cam.fieldOfView = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovSpeed);
    }

    private void LateUpdate()
    {
        if (isCinematic)
            return;

        Vector2 mousePos = Input.mousePosition;

        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 offset = mousePos - center;

        // Normalize (-1 to 1)
        Vector2 normalized = new Vector2(
            offset.x / center.x,
            offset.y / center.y
        );

        // Apply sensitivity scaling
        turn.x = normalized.x * sensitivity;
        turn.y = normalized.y * sensitivity;

        if (Mathf.Abs(normalized.x) < deadzone) normalized.x = 0;
        if (Mathf.Abs(normalized.y) < deadzone) normalized.y = 0;

        turn.y = Mathf.Clamp(turn.y, minAngle, maxAngle);

        if (constraintX)
        {
            float dev = Mathf.DeltaAngle(constraintMiddle, turn.x);
            dev = Mathf.Clamp(dev, -xAngleRange, xAngleRange);
            turn.x = constraintMiddle + dev;
        }

        Quaternion baseRotation = Quaternion.identity;

        Quaternion yaw = Quaternion.AngleAxis(turn.x, baseRotation * Vector3.up);

        Quaternion pitch = Quaternion.AngleAxis(-turn.y, yaw * baseRotation * Vector3.right);

        Quaternion targetRotation = pitch * yaw * baseRotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            cameraSmoothness);
    }

    private void FixedUpdate()
    {
        if(Movement == null) return;
        transform.position = Vector3.Lerp(transform.position, Movement.position + offset, movementSmoothness);
    }

    public void ChangeFOV(float FOV)
    {
        if (cam == null) return;
        targetFOV = FOV;
    }
}
