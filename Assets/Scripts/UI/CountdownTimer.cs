using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class CountdownTimer : MonoBehaviour
{
    public UnityEvent TimerCompleted;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Settings")]
    [SerializeField] private float duration = 60f;
    [SerializeField] private AnimationCurve colorCurve;
    [SerializeField] private AnimationCurve pulseCurve;

    [Header("Color")]
    [SerializeField] private Color colorFull = Color.white;
    [SerializeField] private Color colorEmpty = Color.red;

    [Header("Elastic Pulse")]
    [SerializeField] private int countdownThreshold = 10;
    [SerializeField] private float pulseScaleMultiplier = 1.4f;
    [SerializeField] private float pulseDuration = 0.35f;

    private float timeRemaining;
    private bool isRunning = false;
    private int lastWholeSecond;
    private int baseFontSize;
    private Coroutine pulseCoroutine;
    private bool hasStarted;
    private bool completionDispatched;

    public float RemainingTime => timeRemaining;
    public bool IsRunning => isRunning;

    private void Start()
    {
        baseFontSize = Mathf.RoundToInt(timerText.fontSize);
        if (!hasStarted) StartTimer(duration);
    }

    public void StartTimer(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        hasStarted = true;
        completionDispatched = false;
        timeRemaining = Mathf.Max(0f, seconds);
        lastWholeSecond = Mathf.CeilToInt(timeRemaining);
        isRunning = true;
        UpdateDisplay();
        if (timeRemaining <= 0f) CompleteTimer();
    }

    public bool ScaleRemainingTime(float factor)
    {
        if (!isRunning || float.IsNaN(factor) || float.IsInfinity(factor) || factor <= 0f || factor > 1f)
            return false;
        timeRemaining = Mathf.Max(0f, timeRemaining * factor);
        lastWholeSecond = Mathf.CeilToInt(timeRemaining);
        UpdateDisplay();
        if (timeRemaining <= 0f) CompleteTimer();
        return true;
    }

    public void StopTimer()
    {
        hasStarted = true;
        isRunning = false;
    }

    private void CompleteTimer()
    {
        if (completionDispatched) return;
        timeRemaining = 0f;
        isRunning = false;
        completionDispatched = true;
        UpdateDisplay();
        OnTimerComplete();
        StartCoroutine(SpamPulse());
    }

    private void Update()
    {
        if (!isRunning) return;

        timeRemaining -= Time.deltaTime;
        CheckSecondTick();

        if (timeRemaining <= 0f)
        {
            CompleteTimer();
            return;
        }

        UpdateDisplay();
    }

    private void CheckSecondTick()
    {
        if (timeRemaining > countdownThreshold) return;

        int currentWhole = Mathf.CeilToInt(timeRemaining);
        if (currentWhole != lastWholeSecond)
        {
            lastWholeSecond = currentWhole;
            TriggerPulse(pulseScaleMultiplier * pulseCurve.Evaluate((float)currentWhole/countdownThreshold));
        }
    }

    private void UpdateDisplay()
    {
        // Format
        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";

        float t = 1f - (timeRemaining / duration);

        float easedT = colorCurve.Evaluate(t); 
        timerText.color = Color.Lerp(colorFull, colorEmpty, easedT);
    }

    private void TriggerPulse(float pulseScaleMult, bool reverse = false)
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(ElasticPulse(pulseScaleMult, reverse));
    }

    private IEnumerator ElasticPulse(float pulseScaleMult, bool reverse = false)
    {
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / pulseDuration;
            if(reverse) p = 1f - p;
            // Elastic out: overshoot then settle
            float scale = ElasticOut(p, pulseScaleMult);
            timerText.fontSize = baseFontSize * scale;

            yield return null;
        }

        timerText.fontSize = baseFontSize;
        pulseCoroutine = null;
    }

    // Elastic out easing: starts at 1, overshoots to multiplier, returns to 1
    private float ElasticOut(float t, float scaleMultiplier)
    {
        if (t <= 0f) return 1f;
        if (t >= 1f) return 1f;

        float amplitude = scaleMultiplier - 1f;
        float p = 0.3f;
        float s = p / 4f;

        float eased = amplitude * Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        return eased + 1f; // shift so baseline is 1
    }

    private void OnTimerComplete()
    {
        timerText.text = "00:00";
        timerText.color = colorEmpty;
        timerText.fontSize = baseFontSize;

        TimerCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        duration = Mathf.Max(1f, duration);
        pulseScaleMultiplier = Mathf.Clamp(pulseScaleMultiplier, 1.1f, 3f);
        pulseDuration = Mathf.Clamp(pulseDuration, 0.1f, 1f);
    }
#endif

    IEnumerator SpamPulse()
    {
        for (int i = 0; i < 4; i++)
        {
            TriggerPulse(pulseScaleMultiplier);
            yield return new WaitForSeconds(Random.Range(0.36f, 0.4f));
            TriggerPulse(pulseScaleMultiplier, reverse: true);
            yield return new WaitForSeconds(Random.Range(0.36f, 0.4f));

        }
    }
}