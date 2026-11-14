using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.Events;


public class SceneTransitionManager : MonoBehaviour
{
    [Header("Solid Vignette Overlay")]
    [Tooltip("Draw the vignette as a UI gradient so the color is fully opaque (URP's vignette only multiplies the scene).")]
    public bool useSolidOverlay = true;

    [Tooltip("Full-screen stretched Image on a canvas rendered above everything. Raycast Target off.")]
    public Image overlayImage;

    [Tooltip("Where the gradient starts (0 = center, 1 = corners).")]
    [Range(0f, 0.95f)]
    public float overlayInnerRadius = 0.4f;
    [Header("References")]
    [Tooltip("CanvasGroup on your main game UI Canvas.")]
    public CanvasGroup uiCanvasGroup;

    [Tooltip("The Global Volume that contains a Vignette override.")]
    public Volume globalVolume;
    public bool fadeOnStart = true;

    [Header("Timing")]
    [Tooltip("How long the fade-out phase takes (seconds).")]
    public float fadeOutDuration = 0.5f;

    [Tooltip("How long to hold at fully faded before fading back in (seconds).")]
    public float holdDuration = 0.2f;

    [Tooltip("How long the fade-in phase takes (seconds).")]
    public float fadeInDuration = 0.5f;

    [Header("Vignette")]
    [Tooltip("Vignette intensity at the peak of the transition (0–1).")]
    [Range(0f, 1f)]
    public float vignetteMaxIntensity = 0.6f;

    [Tooltip("Easing curve applied to both the UI and vignette fades.")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Color")]
    [Tooltip("Default color used for the vignette and the UI image during transitions.")]
    public Color transitionColor = Color.black;

    private Vignette _vignette;
    private bool _isTransitioning = false;

    public UnityEvent OnEnd;

    void Start()
    {
        SetupOverlay();
        if (globalVolume != null)
        {
            globalVolume.profile = Instantiate(globalVolume.profile);

            if (globalVolume.profile.TryGet(out _vignette))
            {
                // Make sure our values actually get applied by the volume system
                _vignette.intensity.overrideState = true;
                _vignette.color.overrideState = true;
                _vignette.center.overrideState = true;
                _vignette.rounded.overrideState = true;

                _vignette.intensity.value = 0f;
            }
        }

        if (fadeOnStart) FadeOut(() => OnEnd?.Invoke());
    }

    private void SetupOverlay()
    {
        if (!useSolidOverlay || overlayImage == null) return;

        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.7071f; // 1 at the corners
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(overlayInnerRadius, 1f, d));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        overlayImage.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        overlayImage.type = Image.Type.Simple;
        overlayImage.raycastTarget = false;
        overlayImage.color = new Color(1, 1, 1, 0);
    }

    public void FadeOut(System.Action onMidpoint = null, Color? color = null)
    {
        if (_isTransitioning) StopAllCoroutines();
        StartCoroutine(FadeOutRoutine(onMidpoint, color));
    }

    public void FadeIn(System.Action onMidpoint = null, Color? color = null)
    {
        if (_isTransitioning) StopAllCoroutines();
        StartCoroutine(FadeInRoutine(onMidpoint, color));
    }


    // color is now optional; falls back to transitionColor when null
    public IEnumerator FadeOutInCoroutine(Color? color, float duration, Action onEnd, float vigneteIntensity = .5f, float uiIntensity = .2f, AnimationCurve curve = null)
    {
        _isTransitioning = true;

        yield return StartCoroutine(FadePhase(
            uiStart: 0,
            uiEnd: uiIntensity,
            vignetteStart: 0,
            vignetteEnd: vigneteIntensity,
            duration: duration / 2,
            color: color,
            curve: curve
        ));

        yield return StartCoroutine(FadePhase(
            uiStart: uiIntensity,
            uiEnd: 0,
            vignetteStart: vigneteIntensity,
            vignetteEnd: 0f,
            duration: duration / 2,
            color: color,
            curve: curve
        ));

        onEnd?.Invoke();
        _isTransitioning = false;
    }

    // UnityEvent-friendly (no Color? parameter), uses the inspector color
    public void TriggerFadeOut() => FadeOut();

    private IEnumerator FadeOutRoutine(System.Action onMidpoint, Color? color)
    {
        _isTransitioning = true;

        yield return StartCoroutine(FadePhase(
            uiStart: 1,
            uiEnd: 0,
            vignetteStart: vignetteMaxIntensity,
            vignetteEnd: 0f,
            duration: fadeOutDuration,
            color: color
        ));
        onMidpoint?.Invoke();

        _isTransitioning = false;
    }

    public void TriggerFadeIn() => FadeIn();

    private IEnumerator FadeInRoutine(System.Action onMidpoint, Color? color)
    {
        _isTransitioning = true;

        yield return StartCoroutine(FadePhase(
            uiStart: 0,
            uiEnd: 1,
            vignetteStart: 0f,
            vignetteEnd: vignetteMaxIntensity,
            duration: fadeInDuration,
            color: color
        ));
        onMidpoint?.Invoke();
        _isTransitioning = false;
    }

    private IEnumerator FadePhase(
        float uiStart, float uiEnd,
        float vignetteStart, float vignetteEnd,
        float duration, Color? color = null, AnimationCurve curve = null)
    {
        Color activeColor = color ?? transitionColor;
        float elapsed = 0f;

        bool overlayActive = useSolidOverlay && overlayImage != null;

        if (overlayActive)
        {
            overlayImage.color = new Color(activeColor.r, activeColor.g, activeColor.b, overlayImage.color.a);
        }
        else if (_vignette != null)
        {
            _vignette.center.value = new Vector2(0.5f, 0.5f);
            _vignette.intensity.value = vignetteStart;
            _vignette.color.value = activeColor;
            _vignette.rounded.value = true;
        }

        if (uiCanvasGroup != null && uiCanvasGroup.TryGetComponent<Image>(out Image img))
        {
            img.color = activeColor;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = curve != null ? curve.Evaluate(t) : fadeCurve.Evaluate(t);

            SetCanvasAlpha(Mathf.Lerp(uiStart, uiEnd, curved));
            SetVignetteIntensity(Mathf.Lerp(vignetteStart, vignetteEnd, curved));

            yield return null;
        }

        // Snap to exact target values.
        SetCanvasAlpha(uiEnd);
        SetVignetteIntensity(vignetteEnd);
    }


    private void SetCanvasAlpha(float alpha)
    {
        if (uiCanvasGroup != null)
            uiCanvasGroup.alpha = alpha;
    }

    private void SetVignetteIntensity(float intensity)
    {
        if (useSolidOverlay && overlayImage != null)
        {
            Color c = overlayImage.color;
            c.a = Mathf.Clamp01(intensity);
            overlayImage.color = c;
            return;
        }

        if (_vignette != null)
            _vignette.intensity.value = intensity;
    }
}