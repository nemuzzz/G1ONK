using UnityEngine;

public class GameExitArea : MonoBehaviour
{
    [Header("碰撞检测区域")]
    public BoxCollider2D DetectArea;
    
    [Header("交互按键")]
    public KeyCode interactKey = KeyCode.F;
    
    private bool playerInRange = false;
    private GameExitAreaVisualFeedback visualFeedback;
    
    void Start()
    {
        if (DetectArea == null)
        {
            DetectArea = GetComponent<BoxCollider2D>();
            if (DetectArea == null)
            {
                Debug.Log($"{gameObject.name} 找不到BoxCollider2D组件。");
            }
        }
        
        // 获取视觉反馈组件
        visualFeedback = GetComponent<GameExitAreaVisualFeedback>();
    }
    
    void Update()
    {
        // 检查玩家是否在范围内并按下交互键
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            ExitGame();
        }
    }
    
    // 退出游戏的方法
    private void ExitGame()
    {
        Debug.Log("退出游戏...");
        
        // 在编辑器中
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // 在独立版本中
        #else
        Application.Quit();
        #endif
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerInRange = true;
            
            // 通知玩家可以交互
            if (visualFeedback != null)
            {
                visualFeedback.SetPlayerInRange(true);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
            
            // 通知玩家离开交互范围
            if (visualFeedback != null)
            {
                visualFeedback.SetPlayerInRange(false);
            }
        }
    }
}
