using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SpecialPaperEffectController : MonoBehaviour
{
    [SerializeField] private CountdownTimer _countdownTimer;
    private readonly List<ISpecialPaperEffect> _activeEffects = new List<ISpecialPaperEffect>();
    private readonly List<ISpecialPaperEffect> _retiredEffects = new List<ISpecialPaperEffect>();
    private bool _roundEnded;
    private bool _dispatching;

    public bool RoundEnded => _roundEnded;
    public int ActiveEffectCount => _activeEffects.Count;
    public IReadOnlyList<ISpecialPaperEffect> ActiveEffects => _activeEffects.AsReadOnly();

    public void Accept(DocDataSO document)
    {
        if (_roundEnded || _dispatching || document == null || !document.IsSpecial) return;
        var context = new SpecialPaperEffectContext(factor =>
            !_roundEnded && _countdownTimer != null && _countdownTimer.ScaleRemainingTime(factor));
        _dispatching = true;
        try
        {
            foreach (var definition in document.SpecialEffects)
            {
                if (_roundEnded) break;
                if (definition == null || !definition.IsValid)
                {
                    Debug.LogWarning($"Invalid special-paper effect on {document.name}; skipped.", document);
                    continue;
                }
                ISpecialPaperEffect instance = definition.CreateRuntimeEffect();
                if (instance == null)
                {
                    Debug.LogWarning($"Effect {definition.name} returned no runtime instance.", definition);
                    continue;
                }
                // Register before activation: timer completion can re-enter EndRound.
                _activeEffects.Add(instance);
                instance.Activate(context);
            }
        }
        finally
        {
            _dispatching = false;
            RemoveExpired();
        }
    }

    /// <summary>Bonuses first, then multipliers; rounds midpoint away from zero. Consumes one normal paper use.</summary>
    public int ResolveNormalPaper(bool correct, int basePoints)
    {
        if (_roundEnded || _dispatching) return 0;
        double bonus = 0d;
        double multiplier = 1d;
        _dispatching = true;
        try
        {
            foreach (var effect in _activeEffects)
            {
                if (!effect.IsExpired) effect.ContributeScore(correct, ref bonus, ref multiplier);
            }
            foreach (var effect in _activeEffects)
            {
                if (!effect.IsExpired) effect.OnNormalPaperResolved(correct);
            }
        }
        finally
        {
            _dispatching = false;
            RemoveExpired();
        }
        return _roundEnded ? 0 : ClampScore((basePoints + bonus) * multiplier);
    }

    public static int ClampScore(double score)
    {
        if (double.IsNaN(score)) return 0;
        if (score >= int.MaxValue) return int.MaxValue;
        if (score <= int.MinValue) return int.MinValue;
        return (int)Math.Round(score, MidpointRounding.AwayFromZero);
    }

    public void EndRound()
    {
        if (_roundEnded) return;
        _roundEnded = true;
        // Do not mutate a collection while an effect callback is dispatching.
        if (!_dispatching) RemoveExpired();
    }

    private void RemoveExpired()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            if (_roundEnded || effect.IsExpired)
            {
                _activeEffects.RemoveAt(i);
                _retiredEffects.Add(effect);
            }
        }
        // Release first, then dispatch cleanup so re-entry cannot invalidate iteration.
        var retired = _retiredEffects.ToArray();
        _retiredEffects.Clear();
        foreach (var effect in retired) effect.EndRound();
    }

    private void OnDestroy() { EndRound(); }
}
