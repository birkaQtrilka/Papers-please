using TMPro;
using UnityEngine;

public class HoverEffectUI : MonoBehaviour
{
    [SerializeField] float paddingAddition = 1;
    [SerializeField] Color hoverColor = Color.white;
    [SerializeField] float completeTime = .5f;
    [SerializeField] AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    RectTransform rectTransform;
    TextMeshProUGUI textMeshPro;

    float currentTime = 0;
    Color originalColor;
    float originalPaddingLeft;
    Rect origRect;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textMeshPro = GetComponent<TextMeshProUGUI>();
        originalColor = textMeshPro.color;
        originalPaddingLeft = rectTransform.offsetMin.x;
        origRect = rectTransform.rect;
    }

    void FixedUpdate()
    {
        if(RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
        {
            currentTime += Time.fixedDeltaTime;
        }
        else
        {
            currentTime -= Time.fixedDeltaTime;
        }

        currentTime = Mathf.Clamp(currentTime, 0, completeTime);
        ApplyEffect(animationCurve.Evaluate( currentTime / completeTime));
    }

    void ApplyEffect(float t)
    {
        float padding = Mathf.Lerp(originalPaddingLeft, originalPaddingLeft + paddingAddition, t);
        rectTransform.offsetMin = new Vector2(padding, rectTransform.offsetMin.y);
        textMeshPro.color = Color.Lerp(originalColor, hoverColor, t);
    }
}
