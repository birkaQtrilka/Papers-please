using UnityEngine;

[CreateAssetMenu(fileName = "NextPapersMultiplier", menuName = "Special Papers/Next Papers Multiplier")]
public sealed class NextPapersMultiplierEffectSO : SpecialPaperEffectSO
{
    [Min(1)] [SerializeField] private int _paperCount = 3;
    [UnityEngine.Serialization.FormerlySerializedAs("_multiplier")]
    [Min(0.01f)] [SerializeField] private float _multiplierPositive = 2f;
    [Min(0.01f)] [SerializeField] private float _multiplierNegative = 3f;

    public int PaperCount => _paperCount;
    public float Multiplier => _multiplierPositive;
    public float PositiveMultiplier => _multiplierPositive;
    public float NegativeMultiplier => _multiplierNegative;
    public override string Rules => $"Next {_paperCount} normal papers\nCorrect points x{FormatValue(_multiplierPositive)}\nWrong penalty x{FormatValue(_multiplierNegative)}";
    protected override string FormatDescription(string description) => description
        .Replace("{paperCount}", _paperCount.ToString())
        .Replace("{positiveMultiplier}", FormatValue(_multiplierPositive))
        .Replace("{negativeMultiplier}", FormatValue(_multiplierNegative));
    public override bool IsValid => _paperCount >= 1 && IsFinitePositive(_multiplierPositive) && IsFinitePositive(_multiplierNegative);
    public override ISpecialPaperEffect CreateRuntimeEffect() => new RuntimeEffect(_paperCount, _multiplierPositive, _multiplierNegative);

    private sealed class RuntimeEffect : ISpecialPaperEffect
    {
        private int _remaining;
        private float _multiplierPositive = 2f;
        private float _multiplierNegative = 3f;
        public bool IsExpired => _remaining <= 0;
        public string Status => $"Points x{_multiplierPositive} on success - x{_multiplierNegative} on fail: {_remaining} papers remaining";

        public RuntimeEffect(int paperCount, float multiplierPos, float multiplierNeg)
        {
            _remaining = paperCount;
            _multiplierPositive = multiplierPos;
            _multiplierNegative = multiplierNeg;
        }

        public void Activate(SpecialPaperEffectContext context) { }
        public void ContributeScore(bool correct, ref double bonus, ref double multiplier)
        {
            multiplier *= correct ? _multiplierPositive : _multiplierNegative;
        }
        public void OnNormalPaperResolved(bool correct) { _remaining--; }
        public void EndRound() { _remaining = 0; }
    }
}
