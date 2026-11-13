using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PopEffect", menuName = "Scriptable Objects/PopEffect")]
public class PopEffect : ScriptableObject
{
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private float scaleMultiplier = 0.2f;

    [Tooltip("Path relative to the object passed by the event. " +
             "Use '..' for parent and names for children, e.g. '..', '../..', '../Icon', 'Visual/Glow'. " +
             "Leave empty to use the object itself.")]
    [SerializeField] private string relativePath = "..";

    // Call this from the UnityEvent
    public void StartPop(GameObject source)
    {
        if (source == null) return;

        Transform resolved = ResolveTarget(source.transform);
        Debug.Log(resolved);
        if (resolved == null) return;

        GameObject target = resolved.gameObject;

        if (!target.TryGetComponent(out CoroutineRunner runner))
            runner = target.AddComponent<CoroutineRunner>();

        var before = runner.coroutines.FirstOrDefault(x => (string)x.Data[0] == "pop");
        if(before != null)
        {
            runner.Stop(before);
            target.transform.localScale = (Vector3)before.Data[1];
        }

        runner.Run(Pop(target), new object[] {"pop", target.transform.localScale});
    }

    private Transform ResolveTarget(Transform start)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return start;

        Transform current = start;
        string[] parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            if (part == ".")
                continue;

            current = part == ".." ? current.parent : current.Find(part);

            if (current == null)
            {
                Debug.LogWarning($"PopEffect: couldn't resolve path '{relativePath}' from '{start.name}' (failed at '{part}').", this);
                return null;
            }
        }

        return current;
    }

    private IEnumerator Pop(GameObject target)
    {
        Transform t = target.transform;
        Vector3 originalScale = t.localScale;
        Vector3 targetScale = originalScale * scaleMultiplier;

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            t.localScale = Vector3.Lerp(originalScale, targetScale, time / duration);
            yield return null;
        }
        t.localScale = targetScale;

        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            t.localScale = Vector3.Lerp(targetScale, originalScale, time / duration);
            yield return null;
        }
        t.localScale = originalScale;
    }

}