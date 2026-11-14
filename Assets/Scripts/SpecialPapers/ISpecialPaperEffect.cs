using System;

/// <summary>One accepted effect instance. Never store this mutable state in a definition asset.</summary>
public interface ISpecialPaperEffect
{
    bool IsExpired { get; }
    string Status { get; }
    void Activate(SpecialPaperEffectContext context);
    void ContributeScore(bool correct, ref double bonus, ref double multiplier);
    void OnNormalPaperResolved(bool correct);
    void EndRound();
}

public sealed class SpecialPaperEffectContext
{
    private readonly Func<float, bool> _scaleRemainingTime;

    public SpecialPaperEffectContext(Func<float, bool> scaleRemainingTime)
    {
        _scaleRemainingTime = scaleRemainingTime;
    }

    public bool ScaleRemainingTime(float factor)
    {
        return _scaleRemainingTime != null && _scaleRemainingTime(factor);
    }
}
