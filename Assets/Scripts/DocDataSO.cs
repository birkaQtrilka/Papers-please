using UnityEngine;

[CreateAssetMenu(fileName = "DocData", menuName = "Scriptable Objects/DocData")]
public class DocDataSO : ScriptableObject
{
    public Texture2D DocTexture;
    public bool shouldApprove;

    [Header("Special Paper")]
    [Tooltip("Empty means a normal scored document. Approve accepts all listed effects; Disapprove skips them, with no direct points.")]
    [SerializeField] private SpecialPaperEffectSO[] _specialEffects;

    public bool IsSpecial => _specialEffects != null && _specialEffects.Length > 0;
    public System.Collections.Generic.IReadOnlyList<SpecialPaperEffectSO> SpecialEffects =>
        _specialEffects ?? System.Array.Empty<SpecialPaperEffectSO>();
}
