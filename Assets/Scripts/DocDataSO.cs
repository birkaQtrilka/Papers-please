using UnityEngine;

[CreateAssetMenu(fileName = "DocData", menuName = "Scriptable Objects/DocData")]
public class DocDataSO : ScriptableObject
{
    public Texture2D DocTexture;
    public bool shouldApprove;
}
