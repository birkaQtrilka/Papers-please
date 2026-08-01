using UnityEngine;

public class HoverManager : MonoBehaviour
{
    [SerializeField] Vector2 amplitudeMinMax = new Vector2(0.5f, 1f);
    [SerializeField] Vector2 frequencyMinMax = new Vector2(0.5f, 1f);
    [SerializeField] int seed = 0;

#if UNITY_EDITOR
    [SerializeField] bool randomizeOnValidate = true;
    private void OnValidate()
    {
        if (randomizeOnValidate)
        {
            randomizeOnValidate = false;
            RandomizeData();
        }
    }

#endif

    Transform[] hoverItems;
    Vector3[] originalPositions;
    [SerializeField] float[] amplitudes;
    [SerializeField] float[] frequencies;

    private void Awake()
    {
        hoverItems = GetComponentsInChildren<Transform>();
        originalPositions = new Vector3[hoverItems.Length];
        if(amplitudes == null || amplitudes.Length != hoverItems.Length || frequencies == null || frequencies.Length != hoverItems.Length)
        {
            RandomizeData();
        }
        for (int i = 0; i < hoverItems.Length; i++)
        {
            originalPositions[i] = hoverItems[i].position;
        }
    }

    void Update()
    {

        for (int i = 0; i < hoverItems.Length; i++)
        {
             hoverItems[i].position = originalPositions[i] + amplitudes[i] * Mathf.Sin(Time.time * frequencies[i] + i) * Vector3.up;
        }
    }

    public void RandomizeData()
    {
        if(hoverItems == null || hoverItems.Length == 0)
            hoverItems = GetComponentsInChildren<Transform>();
        Random.InitState(seed);
        amplitudes = new float[hoverItems.Length];
        frequencies = new float[hoverItems.Length];
        for (int i = 0; i < hoverItems.Length; i++)
        {
            amplitudes[i] = Random.Range(amplitudeMinMax.x, amplitudeMinMax.y);
            frequencies[i] = Random.Range(frequencyMinMax.x, frequencyMinMax.y);
        }
    }
}
