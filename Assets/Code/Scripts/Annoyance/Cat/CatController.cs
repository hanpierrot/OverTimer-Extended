using System;
using UnityEngine;

[RequireComponent(typeof(PointerReceiver))]
public class CatController : MonoBehaviour
{
    [Header("Roam Area")]
    [SerializeField] private Vector2 areaMin;
    [SerializeField] private Vector2 areaMax;
    [SerializeField] private float moveSpeed = 1.5f;

    [Header("Sit sub-animation")]
    [SerializeField] private float sitClipCheckInterval = 1f;
    [SerializeField, Range(0f, 1f)] private float sitClipSwitchChance = 0.3f;
    
    [Header("Sleep")]
    [SerializeField] private float sleepDurationMin = 3f;
    [SerializeField] private float sleepDurationMax = 6f;
    
    [SerializeField] private Animator animator;
    
    private float SitDurationMin => GameManager.Instance.GameConfig.catSitDurationMin;
    private float SitDurationMax => GameManager.Instance.GameConfig.catSitDurationMax;
    private float BlockChance => GameManager.Instance.GameConfig.catBlockChance;
    private float BlockDurationMin => GameManager.Instance.GameConfig.catBlockDurationMin;
    private float BlockDurationMax => GameManager.Instance.GameConfig.catBlockDurationMax;

    private readonly CatStateMachine _sm = new CatStateMachine();
    
    private static readonly string[] IdleFamily = { "Idle", "LickPawn", "Scratch" };
    private static readonly string[] LyingFamily = { "Lying", "LickPawn_Lie", "Sleep" };

    private string[] _currentFamily;
    private string _currentSitClip;
    private float _sitClipCheckTimer;

    private Vector2 _targetPos;
    private float _sitTimer;
    private string _currentAnimClip;
    private bool _clipFinishedOnce;
    
    private float _sleepLockTimer;

    private PointerReceiver _blockedTarget;
    private float _blockTimer;
    
    private PointerReceiver _receiver;
    
    private void Awake()
    {
        _receiver = GetComponent<PointerReceiver>();
        
        _sm.AddState(CatState.Wandering, new CatStateMachine.StateDefinition
        {
            Enter = EnterWandering,
            Tick = TickWandering,
        });

        _sm.AddState(CatState.SittingIdle, new CatStateMachine.StateDefinition
        {
            Enter = EnterSittingIdle,
            Tick = TickSittingIdle,
        });
        
        _sm.AddState(CatState.SittingBlocking, new CatStateMachine.StateDefinition()
        {
            Enter = EnterSittingBlocking,
            Tick = TickSittingBlocking,
            Exit = ExitSittingBlocking,
        });
        
        _sm.AddState(CatState.Dragged, new CatStateMachine.StateDefinition
        {
            Enter = EnterDragged,
        });
    }
    
    private void OnEnable()
    {
        _receiver.DragStart += HandleDragStart;
        _receiver.DragUpdate += HandleDragUpdate;
        _receiver.DragEnd += HandleDragEnd;
    }
    
    private void OnDisable()
    {
        _receiver.DragStart -= HandleDragStart;
        _receiver.DragUpdate -= HandleDragUpdate;
        _receiver.DragEnd -= HandleDragEnd;
    }
    
    private void HandleDragStart(Vector2 worldPos) => _sm.ChangeState(CatState.Dragged);

    private void HandleDragUpdate(Vector2 worldPos) => transform.position = worldPos;

    private void HandleDragEnd(Vector2 worldPos) => _sm.ChangeState(CatState.Wandering);

    private void EnterDragged()
    {
        PlayAnimation("Dragged");
    }
    
    private void Start() => _sm.ChangeState(CatState.Wandering);

    private void Update() => _sm.Tick();
    
    //Block Desk Object

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_sm.CurrentState != CatState.Wandering) return;
        
        if (!other.TryGetComponent(out PointerReceiver target)) return;
        if (!target.IsInteractable) return;
        
        if (RngService.Instance.Random.NextDouble() >= BlockChance) return;
        
        _blockedTarget = target;
        _sm.ChangeState(CatState.SittingBlocking);
    }

    private void EnterSittingBlocking()
    {
        _blockTimer = (float)(BlockDurationMin + RngService.Instance.Random.NextDouble() * (BlockDurationMax - BlockDurationMin));
        
        if (_blockedTarget != null) _blockedTarget.SetInteractable(false);
        
        PlayAnimation("Idle");
    }
    
    private void TickSittingBlocking()
    {
        _blockTimer -= Time.deltaTime;
        if (_blockTimer <= 0f) _sm.ChangeState(CatState.Wandering);
    }

    private void ExitSittingBlocking()
    {
        if (_blockedTarget != null) _blockedTarget.SetInteractable(true);
        _blockedTarget = null;
    }

    //Wandering
    
    private void EnterWandering() => PickNewTarget();

    private void PickNewTarget()
    {
        float x = (float)(areaMin.x + RngService.Instance.Random.NextDouble() * (areaMax.x - areaMin.x));
        float y = (float)(areaMin.y + RngService.Instance.Random.NextDouble() * (areaMax.y - areaMin.y));
        _targetPos = new Vector2(x, y);
    }

    private void TickWandering()
    {
        Vector2 pos = transform.position;
        Vector2 dir = _targetPos - pos;

        if (dir.sqrMagnitude <= 0.1f)
        {
            _sm.ChangeState(CatState.SittingIdle);
            return;
        }
        
        PlayWalkAnimation(dir); 
        transform.position = Vector2.MoveTowards(pos, _targetPos, moveSpeed * Time.deltaTime);
    }

    private void PlayWalkAnimation(Vector2 dir)
    {
        string clip = Mathf.Abs(dir.x) > Mathf.Abs(dir.y)
            ? (dir.x > 0 ? "Walk_Right" : "Walk_Left")
            : (dir.y > 0 ? "Walk_Up" : "Walk_Down");

        PlayAnimation(clip);
    }
    
    //Sitting Idle

    private void EnterSittingIdle()
    {
        _sitTimer = (float)(SitDurationMin + RngService.Instance.Random.NextDouble() * (SitDurationMax - SitDurationMin));
        
        _currentFamily = RngService.Instance.Random.Next(2) == 0 ? IdleFamily : LyingFamily;
        SetSitClip(_currentFamily[0]);
    }

    private void TickSittingIdle()
    {
        _sitTimer -= Time.deltaTime;
        
        if (_sitTimer <= 0f && _sleepLockTimer <= 0f)
        {
            _sm.ChangeState(CatState.Wandering);
            return;
        }
        
        TickSitClipTransition();
    }
    
    private void SetSitClip(string clip)
    {
        _currentSitClip = clip;
        _sitClipCheckTimer = 0f;
        PlayAnimation(clip);
        
        _sleepLockTimer = clip == "Sleep"
            ? (float)(sleepDurationMin + RngService.Instance.Random.NextDouble() * (sleepDurationMax - sleepDurationMin))
            : 0f;
    }

    private void TickSitClipTransition()
    {
        if (_sleepLockTimer > 0f)
        {
            _sleepLockTimer -= Time.deltaTime;
        }
        
        if (!_clipFinishedOnce) return;
        
        _sitClipCheckTimer += Time.deltaTime;
        if (_sitClipCheckTimer < sitClipCheckInterval) return;
        _sitClipCheckTimer = 0f;
        
        if (RngService.Instance.Random.NextDouble() >= sitClipSwitchChance) return;

        string next;
        do
        {
            next = _currentFamily[RngService.Instance.Random.Next(_currentFamily.Length)];
        } while(next == _currentSitClip);
        
        SetSitClip(next);
    }
    
    //Helper
    
    private void PlayAnimation(string clip)
    {
        if (_currentAnimClip == clip) return;
        _currentAnimClip = clip;
        if (animator != null) animator.Play(clip);
    }
    
    public void OnAnimationLoopComplete()
    {
        _clipFinishedOnce = true;
    }
}
