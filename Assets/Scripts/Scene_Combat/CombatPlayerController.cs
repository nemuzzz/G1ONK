using System.Collections.Generic;
using UnityEngine;

public class CombatPlayerController : MonoBehaviour
{
    [SerializeField] CombatManager Manager;
    [SerializeField] SpriteRenderer SpriteRder;
    [SerializeField] GameObject BulletPrefab;

    [SerializeField] List<Sprite> Sprites;

    public int Health = 3;
    private float invincibleTime = 0f;
    private float invincibleDuration = 1f; // 无敌时间1秒
    private bool isInvincible = false;
    private bool isGameOver = false;
    
    private int prev_Index = 0;
    
    void Start()
    {
        // 确保玩家对象有Player标签
        gameObject.tag = "Player";
    }
    
    void Update()
    {
        // 如果游戏结束，不处理输入
        if (isGameOver) return;
        
        // 处理无敌时间
        if (isInvincible)
        {
            invincibleTime -= Time.deltaTime;
            if (invincibleTime <= 0)
            {
                isInvincible = false;
                // 恢复正常显示
                SpriteRder.color = new Color(1f, 1f, 1f, 1f);
            }
        }
        
        Vector3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 Direction = (MousePosition - transform.position).normalized;

        float origin_angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        float u_angle = (origin_angle + 360) % 360;

        int index = Mathf.RoundToInt(u_angle / 45) % 8;
        if (prev_Index != index)
        {
            prev_Index = index;
            SpriteRder.sprite = Sprites[index];
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            if (Manager.BulletCnt > 0)
            {
                Instantiate(BulletPrefab, transform.position,
                    Quaternion.AngleAxis(origin_angle - 90, Vector3.forward));
                Manager.SetBulletCnt(Manager.BulletCnt - 1);
            }
        }
    }
    
    // 当Health属性被修改时调用
    public void OnHealthChanged()
    {
        if (!isInvincible)
        {
            // 播放受击音效
            if (CombatAudioManager.Instance != null)
            {
                CombatAudioManager.Instance.PlayPlayerHitSound();
            }
            
            // 通知CombatManager更新UI
            Manager.ReducePlayerHealth();
            
            // 设置无敌时间
            isInvincible = true;
            invincibleTime = invincibleDuration;
            
            // 闪烁效果
            StartCoroutine(FlashEffect());
            
            // 如果生命值降到0，设置游戏结束标志并通知CombatManager
            if (Health <= 0)
            {
                isGameOver = true;
                // 确保CombatManager也知道游戏结束
                if (Manager != null)
                {
                    Manager.GameOver(false);
                }
            }
        }
    }
    
    // 闪烁效果协程
    private System.Collections.IEnumerator FlashEffect()
    {
        for (int i = 0; i < 5; i++)
        {
            SpriteRder.color = new Color(1f, 1f, 1f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            SpriteRder.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
