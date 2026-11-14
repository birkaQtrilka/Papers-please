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

    [SerializeField] Stamper stamper;
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

    [Header("Special Papers")]
    [SerializeField] private SpecialPaperEffectController _specialPaperEffects;
    [SerializeField] private int _correctPoints = 100;
    [SerializeField] private int _wrongPoints = -50;
    private bool _roundEnded;
    public bool IsStamping { get; private set; }
    public bool RoundEnded => _roundEnded;

    public void EndRound()
    {
        if (_roundEnded) return;
        _roundEnded = true;
        if (_specialPaperEffects != null) _specialPaperEffects.EndRound();
    }

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
        if (_roundEnded || IsStamping || stampData.DocData == null || stampData.PaperObject == null) return;
        IsStamping = true;
        DeactivateApproveColliders(stampData.PaperObject);
        stampData.PaperObject.SetParent(null);
        paperFollowData.path = watchToDeskPath;
        CurveFollower paperPutFollower = new (paperFollowData, stampData.PaperObject);
        StartCoroutine(paperPutFollower.WalkLoop(toStampPaperPosition, finalpositionLerpTime));

        paperPutFollower.OnFinishedWalking += () => {
            StartCoroutine(
                MoveStamperAnimation(stampData)
            );

            paperPutFollower.RemoveSubscriptions();
        };
    }

    IEnumerator MoveStamperAnimation(StampData stampData)
    {
        bool approved = stampData.Approved;
        Transform docObject = stampData.PaperObject;

        paperFollowData.path = stampToPaperPath;
        CurveFollower paperDeskFollower = new (paperFollowData, stamper.transform);
        yield return StartCoroutine(paperDeskFollower.LerpToTransform(raisedPosition, inkRaiseTime));

        Transform inkToActivate = approved ? approveInk : disapproveInk;
        yield return StartCoroutine(paperDeskFollower.LerpToTransform(inkToActivate, inkPressTime));

        stamper.PlayStampEffect(approved);

        InkPressed?.Invoke();

        Transform stampPaperPosition = approved ? docObject.Find("Approve") : docObject.Find("Disapprove");
        CurveFollower stampFollower = new(paperFollowData2, stamper.transform, stampToPaperPath);

        yield return StartCoroutine(stampFollower.WalkLoop(stampPaperPosition, stampTime));


        ResolvePaper(stampData);
        scoreUI.UpdateScore(Score);
        var inkRenderer = stampPaperPosition.Find("Stamp");
        inkRenderer.gameObject.SetActive(true);
        stamper.PlayStampEffect(approved);


        InkStamped?.Invoke();
        StartCoroutine(AfterStamp(inkToActivate));
        yield return new WaitForSeconds(paperHoldTime);
        CurveFollower deskToFolder = new (paperFollowData, docObject, deskToFolderPath);
        
        paperAS.Play();
        yield return StartCoroutine(deskToFolder.WalkLoop(folderSpot, finalpositionLerpTime));
        var rb = docObject.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        DestroyLastPaper(docObject.gameObject);

        IsStamping = false;
        StampFinished?.Invoke();
    }

    private void ResolvePaper(StampData stampData)
    {
        if (_roundEnded || stampData.DocData == null) return;
        if (stampData.DocData.IsSpecial)
        {
            if (stampData.Approved)
            {
                if (_specialPaperEffects != null) _specialPaperEffects.Accept(stampData.DocData);
                else Debug.LogWarning("Special paper skipped: no effect controller assigned.", this);
            }
            return;
        }

        bool correct = stampData.Approved == stampData.DocData.shouldApprove;
        int points = correct ? _correctPoints : _wrongPoints;
        if (_specialPaperEffects != null) points = _specialPaperEffects.ResolveNormalPaper(correct, points);
        int previousScore = Score;
        Score = SpecialPaperEffectController.ClampScore((double)Score + points);
        if (correct)
        {
            correctAS.Play();
            scoreUI.PositiveEffect();
            fadeEffector.StartCoroutine(fadeEffector.FadeOutInCoroutine(Color.green, fadeTime, null, fadeVignete, fadeUI, fadeCurve));
        }
        else
        {
            wrongAS.Play();
            scoreUI.NegativeEffect();
            fadeEffector.StartCoroutine(fadeEffector.FadeOutInCoroutine(Color.red, fadeTime, null, fadeVignete, fadeUI, fadeCurve));
        }
        if (Score != previousScore) ScoreChanged?.Invoke();
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
        CurveFollower stampBack = new(d3, stamper.transform, paperToStampPath, false);
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
