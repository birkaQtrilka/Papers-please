using UnityEngine;

public class StampTest : MonoBehaviour
{
    [SerializeField] StampManager stampManager;
    [SerializeField] DocDataSO testDocData;
    [SerializeField] bool approved;
    [SerializeField] Transform paperObject;

    public void Start()
    {
        DoTest();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DoTest();
        }
    }

    private void DoTest()
    {
        StampData testStampData = new(approved, testDocData, paperObject, paperObject);
        stampManager.StampPaper(testStampData);
    }
}

