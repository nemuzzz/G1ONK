using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Range(1, 100)] 
    public float moveSpeed = 5f;
    [Tooltip("u79fbu52a8u5e73u6ed1u5ea6uff0cu8d8au9ad8u8d8au5e73u6ed1u4f46u54cdu5e94u8d8au6162")]
    [Range(0.01f, 0.5f)]
    public float movementSmoothing = 0.05f;
    
    [Header("Animation Settings")]
    public string idleAnim = "Idle";
    public string moveUpAnim = "Move_Up";
    public string moveDownAnim = "Move_Down";
    public string moveLeftAnim = "Move_Left";
    public string moveRightAnim = "Move_Right";
    [Tooltip("u52a8u753bu5207u6362u7684u6700u5c0fu8f93u5165u9608u503cuff0cu9632u6b62u5faeu5c0fu8f93u5165u5bfcu81f4u52a8u753bu9891u7e41u5207u6362")]
    public float animationThreshold = 0.1f;
    [Tooltip("u52a8u753bu5207u6362u7684u6700u5c0fu65f6u95f4u95f4u9694uff08u79d2uff09")]
    public float animationChangeDelay = 0.1f;
    
    [Header("Sound Settings")]
    [Tooltip("u811au6b65u58f0u95f4u9694u65f6u95f4uff08u79d2uff09")]
    public float footstepInterval = 0.3f;

    private Vector2 _moveInput;
    private Vector2 _currentVelocity; // u7528u4e8eu901fu5ea6u5e73u6ed1u8fc7u6e21
    private Animator _animator;
    private Rigidbody2D _rb;
    private bool _canUpdateAnimation = true;
    private bool _canMove = true;  
    private string _currentAnimationState;
    private float _lastAnimationChangeTime;
    private float _lastFootstepTime;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _rb.gravityScale = 0;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate; // u6dfbu52a0u63d2u503cu4ee5u51cfu5c11u6296u52a8
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // u4f7fu7528u8fdeu7eedu78b0u649eu68c0u6d4b
        _currentAnimationState = idleAnim;
        _lastAnimationChangeTime = 0f;
        _lastFootstepTime = 0f;
        _currentVelocity = Vector2.zero;
        
        // 确保Player是根游戏对象，然后才能使用DontDestroyOnLoad
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // u5728 Start u4e2du8ba2u9605u4e8bu4ef6uff0cu786eu4fdd DialogueManager u5df2u7ecfu521du59cbu5316
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onDialogueStart.AddListener(DisableMovement);
            DialogueManager.Instance.onDialogueEnd.AddListener(EnableMovement);
        }
        else
        {
            Debug.LogWarning("DialogueManager not found! Player movement during dialogues may not work correctly.");
        }
        
        // u521du59cbu64adu653eIdleu52a8u753b
        PlayAnimation(idleAnim);
    }

    void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onDialogueStart.RemoveListener(DisableMovement);
            DialogueManager.Instance.onDialogueEnd.RemoveListener(EnableMovement);
        }
    }

    public void DisableMovement()
    {
        _canMove = false;
        _moveInput = Vector2.zero;
        _rb.velocity = Vector2.zero;
        _currentVelocity = Vector2.zero;
        PlayAnimation(idleAnim);
    }

    public void EnableMovement()
    {
        Debug.Log("u542fu7528u73a9u5bb6u79fbu52a8");  // u8c03u8bd5u65e5u5fd7
        _canMove = true;
        // u6e05u9664u4e4bu524du7684u8f93u5165u72b6u6001
        _moveInput = Vector2.zero;
        _rb.velocity = Vector2.zero;
        _currentVelocity = Vector2.zero;
        // u91cdu7f6eu52a8u753bu72b6u6001
        PlayAnimation(idleAnim);
    }

    public void OnXMove(InputAction.CallbackContext context)
    {
        if (!_canMove)
        {
            _moveInput.x = 0;
            return;
        }
        _moveInput.x = context.ReadValue<float>();
    }

    public void OnYMove(InputAction.CallbackContext context)
    {
        if (!_canMove)
        {
            _moveInput.y = 0;
            return;
        }
        _moveInput.y = context.ReadValue<float>();
    }

    public void SetAnimationUpdateEnabled(bool enabled)
    {
        if (enabled && !_canUpdateAnimation)
        {
            PlayAnimation(idleAnim);
        }
        _canUpdateAnimation = enabled;
    }

    // u4f7fu7528FixedUpdateu6765u5904u7406u7269u7406u79fbu52a8uff0cu786eu4fddu5e73u6ed1u4e00u81f4
    private void FixedUpdate()
    {
        if (!_canMove)
        {
            _rb.velocity = Vector2.zero;
            _currentVelocity = Vector2.zero;
            return;
        }

        // u4feeu6539u901fu5ea6u8ba1u7b97u65b9u5f0fuff0cu4f7fu7528u56fau5b9au503cu800cu4e0du53d7u5e27u7387u5f71u54cd
        // u5982u679cu8f93u5165u5927u5c0fu5927u4e8e1uff08u5bf9u89d2u7ebfu79fbu52a8uff09uff0cu8fdbu884cu5f52u4e00u5316u4ee5u4fddu6301u4e00u81f4u7684u901fu5ea6
        Vector2 normalizedInput = _moveInput.magnitude > 1 ? _moveInput.normalized : _moveInput;
        Vector2 targetVelocity = moveSpeed * normalizedInput;
        
        // u4f7fu7528SmoothDampu5e73u6ed1u8fc7u6e21u5f53u524du901fu5ea6u5230u76eeu6807u901fu5ea6
        _rb.velocity = Vector2.SmoothDamp(_rb.velocity, targetVelocity, ref _currentVelocity, movementSmoothing);
        
        // u66f4u65b0u52a8u753b
        if (_canUpdateAnimation)
        {
            UpdateAnimation();
        }
        
        // 
        if (_rb.velocity.magnitude > 0.1f)
        {
            if (Time.time > _lastFootstepTime + footstepInterval)
            {
                PlayFootstepSound();
                _lastFootstepTime = Time.time;
            }
        }
    }

    private void UpdateAnimation()
    {
        if (!_canUpdateAnimation) return;  
        
        // u5982u679cu8f93u5165u503cu5c0fu4e8eu9608u503cuff0cu89c6u4e3au9759u6b62
        if (_moveInput.magnitude < animationThreshold)
        {
            PlayAnimation(idleAnim);
            return;
        }
        
        // u6839u636eu79fbu52a8u65b9u5411u51b3u5b9au52a8u753b
        string targetAnim;
        if (Mathf.Abs(_moveInput.y) > Mathf.Abs(_moveInput.x))
        {
            targetAnim = _moveInput.y > 0 ? moveUpAnim : moveDownAnim;
        }
        else
        {
            targetAnim = _moveInput.x > 0 ? moveRightAnim : moveLeftAnim;
        }
        
        PlayAnimation(targetAnim);
    }
    
    // u64adu653eu52a8u753buff0cu6dfbu52a0u72b6u6001u68c0u67e5u548cu65f6u95f4u95f4u9694u9650u5236
    private void PlayAnimation(string animationName)
    {
        // u5982u679cu8981u64adu653eu7684u52a8u753bu4e0eu5f53u524du52a8u753bu76f8u540cuff0cu4e0du505au4efbu4f55u5904u7406
        if (_currentAnimationState == animationName) return;
        
        // u68c0u67e5u662fu5426u8fbeu5230u52a8u753bu5207u6362u7684u6700u5c0fu65f6u95f4u95f4u9694
        if (Time.time < _lastAnimationChangeTime + animationChangeDelay) return;
        
        // u64adu653eu65b0u52a8u753b
        _animator.Play(animationName);
        _currentAnimationState = animationName;
        _lastAnimationChangeTime = Time.time;
    }
    
    // 
    private void PlayFootstepSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFootstep();
        }
    }
}