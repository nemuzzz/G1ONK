using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GameExitAreaVisualFeedback : MonoBehaviour
{
    [Header("视觉反馈设置")]
    public Sprite normalSprite;       // 正常状态的精灵
    public Sprite highlightedSprite;  // 高亮状态的精灵
    
    private SpriteRenderer spriteRenderer;
    private bool playerInRange = false;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 初始化默认状态
        if (normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }
    
    // 由GameExitArea调用的方法，设置玩家是否在范围内
    public void SetPlayerInRange(bool inRange)
    {
        playerInRange = inRange;
        
        // 根据玩家是否在范围内切换精灵
        if (playerInRange)
        {
            // 切换为高亮图片
            if (highlightedSprite != null)
            {
                spriteRenderer.sprite = highlightedSprite;
            }
        }
        else
        {
            // 恢复原图
            if (normalSprite != null)
            {
                spriteRenderer.sprite = normalSprite;
            }
        }
    }
}
