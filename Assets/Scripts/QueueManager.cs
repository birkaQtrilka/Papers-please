using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class QueueManager : MonoBehaviour
{
    [SerializeField] GameObject[] npcVariants;
    [SerializeField] List<DocDataSO> documents;
    [SerializeField] NPC npcPrefab;
    [SerializeField] NPC gameOverNpc;
    [SerializeField] NPC gameWinNpc;

    [SerializeField] Animator doorAnimator;

    [SerializeField] float doorOpenWait = 1f;
    [SerializeField] float doorCloseWait = 1f;
    [SerializeField] float doorCloseSoundWait = .2f;
    [SerializeField] StampManager stampManager;
    [SerializeField] CountdownTimer countdownTimer;
    [SerializeField] SceneTransitionManager hudFade;

    public UnityEvent<StampData> PaperEvaluated;

    [SerializeField] Transform watchSpot;
    [SerializeField] float watchLerp;
    [SerializeField] CurveFollowData npcFollowData;
    [SerializeField] CurveFollowData paperFollowData;
    [SerializeField] float npc_wobble_speed = 5;
    [SerializeField] float npc_wobble_amplitude = .3f;
    [SerializeField] Curve leaveCurve;
    [Header("Auto Spawner")]
    [SerializeField] Vector2 _timerMinMax;
    [SerializeField] AudioSource openAS;
    [SerializeField] AudioSource closeAS;
    [SerializeField] AudioSource paperAS;
    [SerializeField] Bomb bomb;
    [SerializeField] Vector3 bombThrowVelocity;
    [SerializeField] bool throwBomb;


    float _timer;
    bool _walkOut;
    readonly List<NPC> _npcInstances = new List<NPC>();

    StampData _lastStampData;
    bool _paperReady;
    bool _pendingPaper;
    bool _roundEnded;
    readonly List<UnityAction> _queueAdvanceListeners = new List<UnityAction>();
    public bool RoundEnded => _roundEnded;

    void Start()
    {
        ResetTimer();
        StartCoroutine(StartNpcSpawn(2));
        countdownTimer.TimerCompleted.AddListener(CallOffNPCs);
        stampManager.InkStamped.AddListener(CheckScore);
        stampManager.StampFinished.AddListener(OnStampFinished);
    }

    void OnStampFinished()
    {
        _pendingPaper = false;
        if (_roundEnded) return;
        CheckScore();
        if (!_roundEnded && documents.Count == 0 && _npcInstances.Count == 0)
            StartGameWin();
    }

    void ClearQueueListeners()
    {
        foreach (var listener in _queueAdvanceListeners)
        {
            stampManager.PaperTrashed.RemoveListener(listener);
            stampManager.StampFinished.RemoveListener(listener);
        }
        _queueAdvanceListeners.Clear();
    }

    void OnDestroy()
    {
        if (countdownTimer != null) countdownTimer.TimerCompleted.RemoveListener(CallOffNPCs);
        if (stampManager != null)
        {
            stampManager.InkStamped.RemoveListener(CheckScore);
            stampManager.StampFinished.RemoveListener(OnStampFinished);
            ClearQueueListeners();
        }
    }

    void CheckScore()
    {
        if(stampManager.Score < 0)
        {
            CallOffNPCs();
        }
    }

    IEnumerator StartNpcSpawn(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        ResetTimer();

        StartCoroutine(AddNpcToQueue(SpawnNPC()));
    }

    IEnumerator AddNpcToQueue(NPC instance)
    {
        if(instance == null)
        {
            yield break;
        }

        CurveFollower curveFollower = new(npcFollowData, instance.transform);
        WobbleCurveFollow wobble = new(curveFollower, npc_wobble_speed, npc_wobble_amplitude);

        curveFollower.LastPoint = npcFollowData.path.points.Count - _npcInstances.Count;
        curveFollower.OnFinishedWalking += () => {
            if(_npcInstances.Count > 1)
            {
                OnWalkerStopedInQueue(curveFollower, instance);
            }
            else
            {
                OnWalkerStopedAtDesk(curveFollower, instance);
            }
        };

        doorAnimator.SetBool("IsOpen", true);
        openAS.Play();
        yield return new WaitForSeconds(doorOpenWait);
        instance.StopAllCoroutines();
        instance.StartCoroutine(curveFollower.WalkLoop());

        yield return new WaitForSeconds(doorCloseWait);
        doorAnimator.SetBool("IsOpen", false);
        yield return new WaitForSeconds(doorCloseSoundWait);
        closeAS.Play();
    }

    NPC SpawnNPC()
    {
        if (_roundEnded) return null;
        if (documents.Count == 0)
        {
            Debug.LogError("No documents assigned to QueueManager!");
            return null;
        }

        NPC instance = Instantiate(npcPrefab);
        instance.SetModel(npcVariants[UnityEngine.Random.Range(0, npcVariants.Length)]);
        instance.SetDocData(documents[^1]);
        documents.RemoveAt(documents.Count - 1);

        InitNPC(instance);
        return instance;
    }

    void InitNPC(NPC instance)
    {
        _npcInstances.Add(instance);

        instance.transform.position = npcFollowData.path.startGS;
    }

    void OnWalkerStopedInQueue(CurveFollower walker, NPC npc)
    {
        void self()
        {
            stampManager.PaperTrashed.RemoveListener(self);
            stampManager.StampFinished.RemoveListener(self);
            _queueAdvanceListeners.Remove(self);
            if (_roundEnded || npc == null) return;
            walker.LastPoint++;
            npc.StartCoroutine(walker.WalkLoop());
        }

        _queueAdvanceListeners.Add(self);
        stampManager.PaperTrashed.AddListener(self);
        stampManager.StampFinished.AddListener(self);
    }

    void OnWalkerStopedAtDesk(CurveFollower walker, NPC npc)
    {
        walker.RemoveSubscriptions();
        bool isGameOverNPC = npc.gameObject.name == gameOverNpc.gameObject.name + "(Clone)";
        bool isResultNPC = isGameOverNPC || npc.gameObject.name == gameWinNpc.gameObject.name + "(Clone)";
        if (_roundEnded && !isResultNPC)
        {
            GoToExit(npc);
            return;
        }

        CurveFollower toWatchFollow = new (paperFollowData, npc.PaperObject);
        //WobbleCurveFollow wobble = new(toWatchFollow); 
        StartCoroutine(toWatchFollow.WalkLoop(watchSpot, watchLerp));
        paperAS.Play();
        toWatchFollow.OnFinishedWalking += () => {
            toWatchFollow.RemoveSubscriptions();
            
            npc.PaperObject.SetParent(watchSpot);
            if (isResultNPC) return;
            if (_roundEnded)
            {
                npc.PaperModel.gameObject.layer = LayerMask.NameToLayer("Default");
                return;
            }
            _lastStampData = new StampData(false, npc.DocData, npc.PaperObject, npc.PaperModel.transform);
            npc.PaperModel.gameObject.layer = LayerMask.NameToLayer("NonPixelated");
            _paperReady = true;

        };
    }

    public void OnPaperEvaluated(bool approved)
    {
        if (_roundEnded || !_paperReady || _pendingPaper || stampManager.IsStamping || _npcInstances.Count == 0 ||
            _lastStampData.DocData == null || _lastStampData.PaperObject == null) return;
        _paperReady = false;
        _pendingPaper = true;
        _lastStampData = new StampData(approved, _lastStampData.DocData, _lastStampData.PaperObject, _lastStampData.PaperModel);
        _lastStampData.PaperObject.SetParent(null);
        _lastStampData.PaperModel.gameObject.layer = LayerMask.NameToLayer("Default");

        NPC instance = _npcInstances[0];
        GoToExit(instance);

        PaperEvaluated?.Invoke(_lastStampData);

        if (!_roundEnded && _npcInstances.Count == 0 && documents.Count > 0)
        {
            ResetTimer();
            StartCoroutine(AddNpcToQueue(SpawnNPC()));
        }
    }

    void CloseRound()
    {
        _roundEnded = true;
        _paperReady = false;
        countdownTimer.StopTimer();
        stampManager.EndRound();
        ClearQueueListeners();
        StopAllCoroutines();
        doorAnimator.SetBool("IsOpen", false);
    }

    void StartGameWin()
    {
        if (_roundEnded || _pendingPaper || stampManager.IsStamping) return;
        CloseRound();
        NPC instance = Instantiate(gameWinNpc);
        instance.SetModel(npcVariants[Random.Range(0, npcVariants.Length)]);

        InitNPC(instance);
        instance.transform.Find("GameOverPaper/ScoreText").GetComponent<TextMeshPro>().text = $"Score: {stampManager.Score}";
        hudFade.TriggerFadeOut();

        StartCoroutine(AddNpcToQueue(instance));
    }

    [ContextMenu("Call Off NPCs")]
    public void CallOffNPCs()
    {
        if (_roundEnded) return;
        bool trashReadyPaper = _paperReady && _lastStampData.PaperObject != null;
        CloseRound();
        hudFade.TriggerFadeOut();
        bomb.Throw(bombThrowVelocity);

        _walkOut = true;
        documents.Clear();
        if (trashReadyPaper)
        {
            _lastStampData.PaperModel.gameObject.layer = LayerMask.NameToLayer("Default");
            stampManager.TrashPaper(_lastStampData);
        }
        foreach (var npc in _npcInstances.ToArray())
        {
            if (npc != null) GoToExit(npc);
        }
        NPC instance = Instantiate(gameOverNpc);
        InitNPC(instance);
        StartCoroutine(AddNpcToQueue(instance));
    }

    void GoToExit(NPC instance)
    {
        CurveFollowData copy = npcFollowData;
        copy.path = leaveCurve;
        CurveFollower backFollow = new(copy, instance.transform);
        WobbleCurveFollow wobble = new(backFollow, npc_wobble_speed, npc_wobble_amplitude);
        instance.StopAllCoroutines();
        instance.StartCoroutine(backFollow.WalkLoop());
        backFollow.OnFinishedWalking += () => {
            backFollow.RemoveSubscriptions();
            Destroy(instance.gameObject);
        };
        _npcInstances.Remove(instance);
    }

    private void Update()
    {
        if (_roundEnded) return;
        if (throwBomb)
        {
            throwBomb = false;
            bomb.Throw(bombThrowVelocity);
        }

        if (documents.Count == 0) return;

        _timer -= Time.deltaTime;
        if(_timer <= 0)
        {
            ResetTimer();
            StartCoroutine(AddNpcToQueue(SpawnNPC()));
        }
    }

    [ContextMenu("SpawnRandomNPC")]
    public void SpawnRandomNPC()
    {
         StartCoroutine(AddNpcToQueue(SpawnNPC()));
    }

    public void ResetTimer()
    {
        _timer = Random.Range(_timerMinMax.x, _timerMinMax.y);
    }
    
}
