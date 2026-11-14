using UnityEngine;

[CreateAssetMenu(fileName = "TimeTradeBonus", menuName = "Special Papers/Time Trade Bonus")]
public sealed class TimeTradeBonusEffectSO : SpecialPaperEffectSO
{
    [Range(0.01f, 1f)] [SerializeField] private float _remainingTimeFraction = 0.5f;
    [Min(0)] [SerializeField] private int _correctPaperBonus = 100;

    public float RemainingTimeFraction => _remainingTimeFraction;
    public int CorrectPaperBonus => _correctPaperBonus;
    public override string Rules => $"Keep {FormatValue(_remainingTimeFraction * 100f)}% of remaining time\n+{_correctPaperBonus} per correct normal paper\nUntil round ends";
    protected override string FormatDescription(string description) => description
        .Replace("{remainingTimePercent}", FormatValue(_remainingTimeFraction * 100f))
        .Replace("{bonus}", _correctPaperBonus.ToString());
    public override bool IsValid => IsFinitePositive(_remainingTimeFraction) && _remainingTimeFraction <= 1f && _correctPaperBonus >= 0;
    public override ISpecialPaperEffect CreateRuntimeEffect() => new RuntimeEffect(_remainingTimeFraction, _correctPaperBonus);

    private sealed class RuntimeEffect : ISpecialPaperEffect
    {
        private readonly float _fraction;
        private readonly int _bonus;
        private bool _expired;
        public bool IsExpired => _expired;
        public string Status => $"+{_bonus} per correct paper until round end";

        public RuntimeEffect(float fraction, int bonus)
        {
            _fraction = fraction;
            _bonus = bonus;
        }

        public void Activate(SpecialPaperEffectContext context)
        {
            if (!context.ScaleRemainingTime(_fraction))
            {
                _expired = true;
                Debug.LogWarning("Time-trade effect skipped: no running countdown timer.");
            }
        }
        public void ContributeScore(bool correct, ref double bonus, ref double multiplier)
        {
            if (correct) bonus += _bonus;
        }
        public void OnNormalPaperResolved(bool correct) { }
        public void EndRound() { _expired = true; }
    }
}
