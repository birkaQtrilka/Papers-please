using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreTextMesh;

    [Header("Effect Settings")]
    [SerializeField] float effectDuration = 0.15f;
    [SerializeField] float holdDuration = 0.1f;
    [SerializeField] float positiveFontSizeMultiplier = 1.4f;
    [SerializeField] float negativeFontSizeMultiplier = 0.7f;
    [SerializeField] Color positiveColor = Color.green;
    [SerializeField] Color negativeColor = Color.red;

    [Header("Elastic Settings")]
    [SerializeField] float elasticAmplitude = 1.2f;
    [SerializeField] float elasticPeriod = 0.4f;

    private float originalFontSize;
    private Coroutine activeEffect;

    private void Awake()
    {
        originalFontSize = scoreTextMesh.fontSize;
    }

    public void UpdateScore(int score)
    {
        scoreTextMesh.text = score.ToString();
    }

    public void PositiveEffect()
    {
        if (activeEffect != null) StopCoroutine(activeEffect);
        activeEffect = StartCoroutine(FlashEffect(positiveColor, originalFontSize * positiveFontSizeMultiplier));
    }

    public void NegativeEffect()
    {
        if (activeEffect != null) StopCoroutine(activeEffect);
        activeEffect = StartCoroutine(FlashEffect(negativeColor, originalFontSize * negativeFontSizeMultiplier));
    }

    private float ElasticOut(float t)
    {
        if (t == 0f || t == 1f) return t;
        return elasticAmplitude * Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - elasticPeriod / 4f) * (2f * Mathf.PI) / elasticPeriod) + 1f;
    }

    private IEnumerator FlashEffect(Color flashColor, float targetFontSize)
    {
        float elapsed = 0f;

        while (elapsed < effectDuration)
        {
            float t = ElasticOut(elapsed / effectDuration);
            scoreTextMesh.color = Color.Lerp(Color.white, flashColor, t);
            scoreTextMesh.fontSize = Mathf.Lerp(originalFontSize, targetFontSize, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        scoreTextMesh.color = flashColor;
        scoreTextMesh.fontSize = targetFontSize;

        yield return new WaitForSeconds(holdDuration);

        elapsed = 0f;
        while (elapsed < effectDuration)
        {
            float t = ElasticOut(elapsed / effectDuration);
            scoreTextMesh.color = Color.Lerp(flashColor, Color.white, t);
            scoreTextMesh.fontSize = Mathf.Lerp(targetFontSize, originalFontSize, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        scoreTextMesh.color = Color.white;
        scoreTextMesh.fontSize = originalFontSize;
        activeEffect = null;
    }
}