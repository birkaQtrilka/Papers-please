using UnityEngine;

public readonly struct StampData
{
    public readonly bool Approved;
    public readonly DocDataSO DocData;
    public readonly Transform PaperObject;
    public readonly Transform PaperModel;

    public StampData(bool approved, DocDataSO docData, Transform paperObject, Transform paperModel)
    {
        Approved = approved;
        DocData = docData;
        PaperObject = paperObject;
        PaperModel = paperModel;
    }
}
