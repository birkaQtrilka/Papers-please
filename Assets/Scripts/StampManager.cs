using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class StampManager : MonoBehaviour
{
    public UnityEvent StampFinished;
    public UnityEvent PaperTrashed;
    public UnityEvent InkPressed;
    public UnityEvent InkStamped;
    public UnityEvent ScoreChanged;

    [SerializeField] SceneTransitionManager fadeEffector;
    [SerializeField] float fadeTime = .2f;
    [SerializeField] float fadeVignete = .2f;
    [SerializeField] float fadeUI= 0f;
    [SerializeField] AnimationCurve fadeCurve;
    [SerializeField] ScoreUI scoreUI;

    [SerializeField] Transform toStampPaperPosition;

    [SerializeField] Transform stamper;
    [SerializeField] Transform raisedPosition;
    [SerializeField] Transform approveInk;
    [SerializeField] Transform disapproveInk;
    [SerializeField] Transform stampedRaisedPosition;
    [SerializeField] Transform folderSpot;

    [SerializeField] CurveFollowData paperFollowData;
    [SerializeField] CurveFollowData paperFollowData2;
    [SerializeField] CurveFollowData d3;
    [SerializeField] float finalpositionLerpTime;
    [SerializeField] float stampTime;
    [SerializeField] float inkRaiseTime = .2f;
    [SerializeField] float inkPressTime = .1f;
    [SerializeField] float stampRaiseTime;
    [SerializeField] float stampRaiseHoldTime;
    [SerializeField] float paperHoldTime = .2f;
    [SerializeField] float trashPaperTime = .9f;

    [SerializeField] Curve watchToDeskPath;
    [SerializeField] Curve stampToPaperPath;
    [SerializeField] Curve paperToStampPath;
    [SerializeField] Curve deskToFolderPath;
    [SerializeField] AudioSource paperAS;
    [SerializeField] AudioSource wrongAS;
    [SerializeField] AudioSource correctAS;
    public int Score = 0;
    

    GameObject _lastPaper;
    private void Start()
    {
        scoreUI.UpdateScore(Score);
    }

    public void TrashPaper(StampData stampData)
    {
        stampData.PaperObject.SetParent(null);
        CurveFollower paperPutFollower = new(paperFollowData, stampData.PaperObject);
        StartCoroutine(paperPutFollower.LerpToTransform(folderSpot, trashPaperTime));
        paperAS.Play();
        //DestroyLastPaper(stampData.PaperObject.gameObject);
        PaperTrashed?.Invoke();
    }

    public void StampPaper(StampData stampData)
    {
        stampData.PaperObject.SetParent(null);
        paperFollowData.path = watchToDeskPath;
        CurveFollower paperPutFollower = new (paperFollowData, stampData.PaperObject);
        StartCoroutine(paperPutFollower.WalkLoop(toStampPaperPosition, finalpositionLerpTime));

        paperPutFollower.OnFinishedWalking += () => {
            StartCoroutine(
                MoveStamperAnimation(stampData.Approved, stampData.DocData.shouldApprove, stampData.PaperObject)
            );

            paperPutFollower.RemoveSubscriptions();
        };
    }

    IEnumerator MoveStamperAnimation(bool approved, bool shouldApprove, Transform docObject)
    {
        DeactivateApproveColliders(docObject);

        paperFollowData.path = stampToPaperPath;
        CurveFollower paperDeskFollower = new (paperFollowData, stamper);
        yield return StartCoroutine(paperDeskFollower.LerpToTransform(raisedPosition, inkRaiseTime));

        Transform inkToActivate = approved ? approveInk : disapproveInk;
        yield return StartCoroutine(paperDeskFollower.LerpToTransform(inkToActivate, inkPressTime));

        InkPressed?.Invoke();

        Transform stampPaperPosition = approved ? docObject.Find("Approve") : docObject.Find("Disapprove");
        CurveFollower stampFollower = new(paperFollowData2, stamper, stampToPaperPath);

        yield return StartCoroutine(stampFollower.WalkLoop(stampPaperPosition, stampTime));


        if (!(shouldApprove ^ approved))
        {
            correctAS.Play();
            Score += 100;
            scoreUI.PositiveEffect();
            fadeEffector.StartCoroutine(fadeEffector. FadeOutInCoroutine(Color.green, fadeTime, null, fadeVignete, fadeUI, fadeCurve));
        }
        else
        {
            wrongAS.Play();
            Score -= 50;
            scoreUI.NegativeEffect();
            fadeEffector.StartCoroutine(fadeEffector.FadeOutInCoroutine(Color.red, fadeTime, null, fadeVignete, fadeUI, fadeCurve));
        }

        scoreUI.UpdateScore(Score);
        var inkRenderer = stampPaperPosition.Find("Stamp");
        inkRenderer.gameObject.SetActive(true);


        InkStamped?.Invoke();
        StartCoroutine(AfterStamp(inkToActivate));
        yield return new WaitForSeconds(paperHoldTime);
        CurveFollower deskToFolder = new (paperFollowData, docObject, deskToFolderPath);
        paperAS.Play();
        yield return StartCoroutine(deskToFolder.WalkLoop(folderSpot, finalpositionLerpTime));
        
        DestroyLastPaper(docObject.gameObject);

        StampFinished.Invoke();
    }

    void DestroyLastPaper(GameObject newPaper)
    {
        if (_lastPaper != null)
        {
            Destroy(_lastPaper);
        }
        _lastPaper = newPaper;
    }

    // make adapter util
    // custom property drawer that holds scriptable object reference
    IEnumerator AfterStamp(Transform backSpot)
    {
        CurveFollower stampBack = new(d3, stamper, paperToStampPath, false);
        yield return StartCoroutine(stampBack.LerpToTransform(stampedRaisedPosition, stampRaiseTime));
        yield return new WaitForSeconds(stampRaiseHoldTime);
        yield return StartCoroutine(stampBack.WalkLoop(backSpot, finalpositionLerpTime));
    }

    void DeactivateApproveColliders(Transform paper)
    {
        paper.Find("Approve").GetComponent<Collider>().enabled = false;
        paper.Find("Disapprove").GetComponent<Collider>().enabled = false;
    }
}
