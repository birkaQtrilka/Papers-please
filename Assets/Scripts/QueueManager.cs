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


    void Start()
    {
        ResetTimer();
        StartCoroutine(StartNpcSpawn(2));
        countdownTimer.TimerCompleted.AddListener(CallOffNPCs);
        stampManager.InkStamped.AddListener(CheckScore);
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
            walker.LastPoint++;
            npc.StartCoroutine(walker.WalkLoop());

            stampManager.PaperTrashed.RemoveListener(self);
            stampManager.StampFinished.RemoveListener(self);
        }

        stampManager.PaperTrashed.AddListener(self);
        stampManager.StampFinished.AddListener(self);
    }

    void OnWalkerStopedAtDesk(CurveFollower walker, NPC npc)
    {
        walker.RemoveSubscriptions();
        bool isGameOverNPC = npc.gameObject.name == gameOverNpc.gameObject.name + "(Clone)";
        if (_walkOut && !isGameOverNPC)
        {
            GoToExit(npc);
            return;
        }

        CurveFollower toWatchFollow = new (paperFollowData, npc.PaperObject);
        StartCoroutine(toWatchFollow.WalkLoop(watchSpot, watchLerp));
        paperAS.Play();
        toWatchFollow.OnFinishedWalking += () => {
            
            npc.PaperObject.SetParent(watchSpot);

            _lastStampData = new StampData(false, npc.DocData, npc.PaperObject, npc.PaperModel.transform);
            if (!isGameOverNPC && npc.gameObject.name != gameWinNpc.gameObject.name + "(Clone)")
                npc.PaperModel.gameObject.layer = LayerMask.NameToLayer("NonPixelated");

            toWatchFollow.RemoveSubscriptions();
            if (_walkOut)
            {
                if(!isGameOverNPC)
                {
                    stampManager.TrashPaper(_lastStampData);

                }
                npc.PaperModel.gameObject.layer = LayerMask.NameToLayer("Default");
            }
            

        };
    }

    public void OnPaperEvaluated(bool approved)
    {
        _lastStampData = new StampData(approved, _lastStampData.DocData, _lastStampData.PaperObject, _lastStampData.PaperModel);
        _lastStampData.PaperObject.SetParent(null);
        _lastStampData.PaperModel.gameObject.layer = LayerMask.NameToLayer("Default");

        NPC instance = _npcInstances[0];
        GoToExit(instance);

        PaperEvaluated?.Invoke(_lastStampData);

        if(_npcInstances.Count == 0)
        {
            ResetTimer();

            if (documents.Count == 0)
            {
                StartGameWin();
            }
            else
            {
                StartCoroutine(AddNpcToQueue(SpawnNPC()));
            }
        }
    }

    void StartGameWin()
    {
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
        hudFade.TriggerFadeOut();
        bomb.Throw(bombThrowVelocity);

        _walkOut = true;
        documents.Clear();
        if(_npcInstances.Count != 0)
        {
            NPC deskNPC = _npcInstances[0];
            StampData stampData = new StampData(false, deskNPC.DocData, deskNPC.PaperObject, deskNPC.PaperModel.transform);
            stampManager.TrashPaper(stampData);
            stampData.PaperModel.gameObject.layer = LayerMask.NameToLayer("Default");

            GoToExit(deskNPC);
        }
        NPC instance = Instantiate(gameOverNpc);
        instance.transform.position = npcFollowData.path.startGS;
        StartCoroutine(AddNpcToQueue(instance));
    }

    void GoToExit(NPC instance)
    {
        CurveFollowData copy = npcFollowData;
        copy.path = leaveCurve;
        CurveFollower backFollow = new(copy, instance.transform);
        instance.StopAllCoroutines();
        instance.StartCoroutine(backFollow.WalkLoop());
        backFollow.OnFinishedWalking += () => {
            backFollow.RemoveSubscriptions();
            Destroy(instance.gameObject);
        };
        if(_npcInstances.Count > 0)
            _npcInstances.RemoveAt(0);
    }

    private void Update()
    {
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
