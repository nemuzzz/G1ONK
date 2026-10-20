using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TransAreaVisualFeedback : MonoBehaviour
{
    [Header("视觉反馈设置")]
    public Sprite normalSprite;
    public Sprite highlightedSprite;
    public GameObject fPromptPrefab; // 漂浮的F提示预制体
    public float floatHeight = 1.5f; // 漂浮高度
    
    private SpriteRenderer spriteRenderer;
    private GameObject currentFPrompt;
    private bool playerInRange = false;
    private TransArea transArea; // 引用原有的传送脚本

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transArea = GetComponent<TransArea>();
        
        // 初始化默认状态
        if (normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
    }

    void Update()
    {
        // 更新F提示位置
        if (playerInRange && currentFPrompt != null)
        {
            Vector3 targetPosition = transform.position + Vector3.up * floatHeight;
            currentFPrompt.transform.position = targetPosition;
            
            // 添加简单的浮动动画
            float floatOffset = Mathf.Sin(Time.time * 2f) * 0.1f;
            currentFPrompt.transform.position += Vector3.up * floatOffset;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            // 切换为高亮图片
            if (highlightedSprite != null)
            {
                spriteRenderer.sprite = highlightedSprite;
            }
            
            // 显示F提示
            if (fPromptPrefab != null && currentFPrompt == null)
            {
                currentFPrompt = Instantiate(fPromptPrefab, 
                    transform.position + Vector3.up * floatHeight, 
                    Quaternion.identity);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            // 恢复原图
            if (normalSprite != null)
            {
                spriteRenderer.sprite = normalSprite;
            }
            
            // 隐藏F提示
            if (currentFPrompt != null)
            {
                Destroy(currentFPrompt);
                currentFPrompt = null;
            }
        }
    }
}
