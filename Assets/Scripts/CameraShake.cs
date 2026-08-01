using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] float frequency = 20f;
    [SerializeField] AnimationCurve recoverySpeed;

    [Header("Influence")]
    [SerializeField] float maxPositionShake = 0.3f;
    [SerializeField] float maxRotationShake = 3f;
    [SerializeField] float _seed;

    [SerializeField] float _tr;
    [SerializeField] float _ti;

    float _trauma;
    Vector3 _initialLocalPos;
    Quaternion _initialLocalRot;
    float _time;
    float _totalTime;
    float _totalTrauma;

#if UNITY_EDITOR
    [SerializeField] bool _addShake;
    private void OnValidate()
    {
        if (_addShake)
        {
            _addShake = false;
            AddShake(_tr, _ti);
        }
    }
#endif

    void Awake()
    {
        _initialLocalPos = transform.localPosition;
        _initialLocalRot = transform.localRotation;
    }

    public void AddShake(float amount)
    {
        _time = Mathf.Max(_time, 0);
        _trauma += amount;
        _time += .1f;
        _totalTime = _time;
        _totalTrauma = _trauma;
    }

    public void AddShake(float amount, float time/*, float maxR, float maxP*/)
    {
        _time = Mathf.Max(_time, 0);
        _time += time;
        _trauma += amount;
        _totalTime = _time;
        _totalTrauma = _trauma;
        //maxPositionShake = maxP;
        //maxRotationShake = maxR;
    }

    void Update()
    {
        if (_time <= 0f)
        {
            transform.SetLocalPositionAndRotation(_initialLocalPos, _initialLocalRot);
            return;
        }

        float shake = _trauma * _trauma;
        float t = _time * frequency;
        // -1 to 1
        float px = (Mathf.PerlinNoise(_seed + 0f, t) - 0.5f) * 2f;
        float py = (Mathf.PerlinNoise(_seed + 1f, t) - 0.5f) * 2f;
        float rz = (Mathf.PerlinNoise(_seed + 2f, t) - 0.5f) * 2f;

        
        transform.SetLocalPositionAndRotation(_initialLocalPos + new Vector3(
            px * maxPositionShake * shake,
            py * maxPositionShake * shake,
            0f
        ), _initialLocalRot * Quaternion.Euler(
            0f, 0f, rz * maxRotationShake * shake
        ));
        if(_totalTime == 0) _trauma = 0;
        else _trauma = _totalTrauma * recoverySpeed.Evaluate(_time/_totalTime);
        _time -= Time.deltaTime;

        _trauma = Mathf.Max(_trauma, 0);
        _time = Mathf.Max(_time, 0);
    }
}