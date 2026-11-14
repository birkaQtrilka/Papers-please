using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class SpecialPaperView : MonoBehaviour
{
    [SerializeField] private MeshRenderer _paperRenderer;
    [SerializeField] private GameObject _content;
    [SerializeField] private TMPro.TextMeshPro _title;
    [SerializeField] private TMPro.TextMeshPro _body;

    private readonly List<SpecialPaperEffectSO> _definitions = new List<SpecialPaperEffectSO>();
    private Transform[] _contentTransforms;
    private int _lastLayer = -1;
    public string DisplayedTitle => _title.text;
    public string DisplayedBody => _body.text;

    private void Awake()
    {
        _contentTransforms = _content.GetComponentsInChildren<Transform>(true);
    }

    public void Bind(DocDataSO document)
    {
        Unsubscribe();
        bool special = document != null && document.IsSpecial;
        _content.SetActive(special);
        if (!special) return;
        foreach (var definition in document.SpecialEffects)
        {
            if (definition != null && !_definitions.Contains(definition))
            {
                _definitions.Add(definition);
                definition.Changed += Refresh;
            }
        }
        Refresh();
        SynchronizeLayer();
    }

    public void Refresh()
    {
        var title = new StringBuilder();
        var body = new StringBuilder();
        foreach (var definition in _definitions)
        {
            if (title.Length > 0) title.Append(" + ");
            title.Append(definition.DisplayName);
            if (body.Length > 0) body.Append("\n\n");
            if (!string.IsNullOrWhiteSpace(definition.Description))
                body.Append(definition.Description).Append("\n\n");
            body.Append(definition.IsValid ? definition.Rules : "Invalid effect configuration");
        }
        _title.text = title.Length > 0 ? title.ToString() : "Special Paper";
        _body.text = body.ToString();
    }

    // QueueManager changes only the paper renderer layer when entering/leaving inspection.
    private void LateUpdate() => SynchronizeLayer();
    private void SynchronizeLayer()
    {
        int layer = _paperRenderer.gameObject.layer;
        if (_lastLayer == layer) return;
        _lastLayer = layer;
        foreach (var child in _contentTransforms) child.gameObject.layer = layer;
    }

    private void Unsubscribe()
    {
        foreach (var definition in _definitions)
            if (definition != null) definition.Changed -= Refresh;
        _definitions.Clear();
    }
    private void OnDestroy() => Unsubscribe();
}
