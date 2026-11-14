using UnityEngine;

public abstract class SpecialPaperEffectSO : ScriptableObject
{
    [SerializeField] private string _displayName;
    [Tooltip("Editable paper description. Effects can substitute value tokens such as {paperCount}, {positiveMultiplier}, {negativeMultiplier}, {remainingTimePercent} and {bonus}.")]
    [TextArea(2, 5)] [SerializeField] private string _description;

    public event System.Action Changed;
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public string Description => FormatDescription(_description ?? string.Empty);
    public virtual string Rules => string.Empty;
    public abstract bool IsValid { get; }
    public abstract ISpecialPaperEffect CreateRuntimeEffect();

    protected virtual string FormatDescription(string description) => description;
    protected static string FormatValue(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    public void NotifyChanged() => Changed?.Invoke();
    protected virtual void OnValidate() => NotifyChanged();

    protected static bool IsFinitePositive(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
