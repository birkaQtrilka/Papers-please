using UnityEngine;

public class NPC : MonoBehaviour
{
    public Transform ModelPosition;
    public MeshRenderer PaperModel;
    public Transform PaperObject;
    [SerializeField] private SpecialPaperView _specialPaperView;
    DocDataSO _docData;
    public DocDataSO DocData => _docData;
    public void SetModel(GameObject modelPrefab)
    {
        if (ModelPosition.childCount > 0)
        {
            Destroy(ModelPosition.GetChild(0).gameObject);
        }
    
        GameObject modelInstance = Instantiate(modelPrefab, ModelPosition);
        //modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

    }

    public void SetDocData(DocDataSO docData)
    {
        if(docData == null)
        {
            Debug.LogError("DocDataSO is null!");
            return;
        }

        _docData = docData;
        if (_specialPaperView != null) _specialPaperView.Bind(docData);

        if (PaperModel != null)
        {
            PaperModel.material.mainTexture = docData.DocTexture;
        }
    }
}
