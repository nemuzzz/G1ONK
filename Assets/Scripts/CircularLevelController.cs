using System.Collections;
using UnityEngine;
using DG.Tweening;

public class CircularLevelController : MonoBehaviour
{
    [System.Serializable]
    public class LayerConfig
    {
        public Transform layerTransform;     // 圆环层级的Transform引用
        public float radius;                 // 圆环半径
        public float rotationSpeed = 60f;    // 旋转速度
        public Collider2D upStairsTrigger;   // 通往上层的楼梯触发器
        public Collider2D downStairsTrigger; // 通往下层的楼梯触发器
        public Transform[] npcsInLayer;      // 该层级中的NPC列表
        [HideInInspector]
        public Quaternion[] initialNPCRotations; // 存储NPC的初始旋转
    }

    [Header("玩家设置")]
    public Transform player;                 // 玩家物体引用
    public Collider2D playerCollider;        // 玩家的碰撞器
    public float playerMoveSpeed = 5f;       // 玩家在最内层的移动速度

    [Header("层级配置")]
    public LayerConfig[] layerConfigs = new LayerConfig[4];  // 4个层级：外层+3个圆环
    
    [Header("移动设置")]
    public float layerTransitionDuration = 0.5f;    // 层级切换动画时长
    public Ease transitionEase = Ease.OutQuad;      // 过渡动画曲线
    
    private int currentLayer = 0;            // 当前所在层级（0是最外层）
    private float currentAngle = 0f;         // 当前角度
    private bool isTransitioning = false;    // 是否正在切换层级
    private Vector3 playerLocalPosition;     // 玩家在圆环上的本地位置
    private bool canRotate = true;           // 是否可以旋转场景
    
    private void Start()
    {
        // 确保所有必要组件都已设置
        if (player == null || playerCollider == null)
        {
            Debug.LogError("玩家或玩家碰撞器引用未设置！");
            enabled = false;
            return;
        }

        // 初始化每层的NPC
        foreach (var config in layerConfigs)
        {
            if (config.npcsInLayer != null)
            {
                // 初始化数组来存储NPC的初始旋转
                if (config.initialNPCRotations == null || config.initialNPCRotations.Length != config.npcsInLayer.Length)
                {
                    config.initialNPCRotations = new Quaternion[config.npcsInLayer.Length];
                }

                for (int i = 0; i < config.npcsInLayer.Length; i++)
                {
                    Transform npc = config.npcsInLayer[i];
                    if (npc != null)
                    {
                        // 保存NPC的初始旋转
                        config.initialNPCRotations[i] = npc.rotation;
                        
                        // 设置父物体关系
                        if (npc.parent != config.layerTransform)
                        {
                            npc.SetParent(config.layerTransform, true);
                        }
                        
                        // 保持初始旋转
                        npc.rotation = config.initialNPCRotations[i];
                    }
                }
            }
        }

        // 检查是否有待处理的传送数据
        if (CircularLevelData.HasPendingCircularTrans())
        {
            // 获取待处理的层级和角度
            int targetLayer = CircularLevelData.GetPendingTargetLayer();
            float targetAngle = CircularLevelData.GetPendingRotationAngle();
            
            // 确保目标层级有效
            if (targetLayer >= 0 && targetLayer < layerConfigs.Length)
            {
                Debug.Log($"应用传送数据: 层级={targetLayer}, 角度={targetAngle}");
                
                // 设置当前层级
                currentLayer = targetLayer;
                
                // 设置旋转角度
                currentAngle = targetAngle;
                
                // 应用旋转到所有相关层级
                for (int i = currentLayer; i < layerConfigs.Length; i++)
                {
                    layerConfigs[i].layerTransform.rotation = Quaternion.Euler(0, 0, -currentAngle);
                }
                
                // 更新玩家位置
                UpdatePlayerPosition();
            }
            else
            {
                Debug.LogWarning($"无效的目标层级: {targetLayer}");
            }
            
            // 清除待处理的传送数据
            CircularLevelData.ClearPendingCircularTrans();
        }
        else
        {
            // 初始化玩家位置
            UpdatePlayerPosition();
        }
        
        // 订阅对话事件
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onDialogueStart.AddListener(OnDialogueStart);
            DialogueManager.Instance.onDialogueEnd.AddListener(OnDialogueEnd);
        }
        else
        {
            Debug.LogWarning("DialogueManager未找到！对话期间圆形关卡旋转控制可能无法正常工作。");
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅对话事件
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onDialogueStart.RemoveListener(OnDialogueStart);
            DialogueManager.Instance.onDialogueEnd.RemoveListener(OnDialogueEnd);
        }
    }
    
    // 对话开始时停止场景旋转
    private void OnDialogueStart()
    {
        canRotate = false;
        Debug.Log("对话开始，停止圆形关卡旋转");
    }
    
    // 对话结束时恢复场景旋转
    private void OnDialogueEnd()
    {
        canRotate = true;
        Debug.Log("对话结束，恢复圆形关卡旋转");
    }

    private void Update()
    {
        if (isTransitioning) return;

        float horizontalInput = Input.GetAxis("Horizontal");
        
        if (currentLayer == layerConfigs.Length - 1)
        {
            // 在最内层时，直接移动玩家
            HandleInnerLayerMovement(horizontalInput);
        }
        else if (canRotate) // 只有在可以旋转时才旋转场景
        {
            // 在其他层时，旋转当前层及内层
            RotateLayersFromCurrent(-horizontalInput);
        }
        
        // 检查楼梯触发器
        CheckStairsTransition();
    }
    
    private void HandleInnerLayerMovement(float horizontalInput)
    {
        // 限制玩家在圆环上移动
        playerLocalPosition = playerLocalPosition.normalized * layerConfigs[currentLayer].radius;
    }
    
    private void RotateLayersFromCurrent(float rotationAmount)
    {
        float deltaRotation = rotationAmount * layerConfigs[currentLayer].rotationSpeed * Time.deltaTime;
        currentAngle += deltaRotation;
        currentAngle = NormalizeAngle(currentAngle);
        
        // 从当前层开始旋转所有内层
        for (int i = currentLayer; i < layerConfigs.Length; i++)
        {
            LayerConfig config = layerConfigs[i];
            
            // 旋转整个层级，NPC作为子物体会自动跟随旋转
            config.layerTransform.rotation = Quaternion.Euler(0, 0, -currentAngle);
        }
        
        // 更新玩家位置
        UpdatePlayerPosition();
    }
    
    private void UpdatePlayerPosition()
    {
        if (currentLayer == layerConfigs.Length - 1)
        {
            // 在最内层时，保持玩家的本地位置
            return;
        }

        // 计算玩家在当前圆环上的位置
        float angle = currentAngle * Mathf.Deg2Rad;
        Vector3 position = new Vector3(0, layerConfigs[currentLayer].radius, 0);
        
        player.position = position;
    }
    
    private void CheckStairsTransition()
    {
        LayerConfig currentConfig = layerConfigs[currentLayer];
        
        // 检查是否可以上楼
        if (currentConfig.upStairsTrigger && 
            playerCollider.IsTouching(currentConfig.upStairsTrigger))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                TransitionToLayer(currentLayer + 1);
            }
        }
        
        // 检查是否可以下楼
        if (currentConfig.downStairsTrigger && 
            playerCollider.IsTouching(currentConfig.downStairsTrigger))
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                TransitionToLayer(currentLayer - 1);
            }
        }
    }
    
    private void TransitionToLayer(int targetLayer)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToLayerCoroutine(targetLayer));
    }

    private IEnumerator TransitionToLayerCoroutine(int targetLayer)
    {
        isTransitioning = true;
        Vector3 targetPos = new Vector3(0, layerConfigs[targetLayer].radius, 0);
        
        // 获取组件并设置动画
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            // 禁用玩家自动动画更新
            playerMovement.SetAnimationUpdateEnabled(false);
            
            // 播放过渡动画
            string animName = targetLayer < currentLayer ? playerMovement.moveUpAnim : playerMovement.moveDownAnim;
            playerMovement.GetComponent<Animator>()?.Play(animName);
        }
        
        // 等待过渡完成
        yield return player.DOMove(targetPos, layerTransitionDuration)
            .SetEase(transitionEase)
            .WaitForCompletion();
            
        // 更新状态
        currentLayer = targetLayer;
        isTransitioning = false;
        UpdatePlayerPosition();
        
        // 恢复玩家自动动画更新
        if (playerMovement != null)
        {
            playerMovement.SetAnimationUpdateEnabled(true);
        }
    }
    
    private float NormalizeAngle(float angle)
    {
        while (angle > 360f) angle -= 360f;
        while (angle < 0f) angle += 360f;
        return angle;
    }
    
    private void OnDrawGizmos()
    {
        // 在编辑器中绘制圆环辅助线
        Gizmos.color = Color.yellow;
        foreach (var config in layerConfigs)
        {
            if (config != null)
            {
                DrawCircle(Vector3.zero, config.radius, 32);
            }
        }
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float nextAngle = angle + angleStep;
            Vector3 pos = center + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius
            );
            Vector3 nextPos = center + new Vector3(
                Mathf.Cos(nextAngle * Mathf.Deg2Rad) * radius,
                Mathf.Sin(nextAngle * Mathf.Deg2Rad) * radius
            );
            
            Gizmos.DrawLine(pos, nextPos);
            angle = nextAngle;
        }
    }
}
