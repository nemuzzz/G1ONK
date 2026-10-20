using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float Speed = 3;
    [SerializeField] Rigidbody2D rb;
    
    [Header("击退设置")]
    [SerializeField] float KnockbackDistance = 2f; // 击退距离
    [SerializeField] float KnockbackDuration = 0.5f; // 击退持续时间
    [SerializeField] AnimationCurve KnockbackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 击退动画曲线
    
    private CombatManager Manager;
    private bool isStopped = false;
    private bool isKnockedBack = false;
    private Vector2 originalPosition;
    private Vector2 knockbackDirection;

    public void SetCombatManager(CombatManager manager)
    {
        Manager = manager;
    }
    
    private void FixedUpdate()
    {
        if (isStopped || isKnockedBack) return;
        
        Vector2 Direction = (Vector3.zero - transform.position).normalized;
        rb.velocity = Speed * Direction;
    }
    
    // 停止敌人移动
    public void StopMoving()
    {
        isStopped = true;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }
    
    // 击退敌人
    private IEnumerator KnockbackCoroutine(Vector2 direction)
    {
        isKnockedBack = true;
        originalPosition = transform.position;
        knockbackDirection = direction.normalized;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < KnockbackDuration)
        {
            float t = elapsedTime / KnockbackDuration;
            float curveValue = KnockbackCurve.Evaluate(t);
            
            // 计算击退位置
            Vector2 knockbackPosition = originalPosition + knockbackDirection * KnockbackDistance * curveValue;
            transform.position = knockbackPosition;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        isKnockedBack = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            // 播放敌人死亡音效
            if (CombatAudioManager.Instance != null)
            {
                CombatAudioManager.Instance.PlayEnemyDeathSound();
            }
            
            Destroy(other.gameObject);
            Destroy(gameObject);
            Manager.ReduceRemainEnemyCnt(1);
        }
        else if (other.gameObject.CompareTag("Player"))
        {
            // 计算击退方向（从玩家指向敌人的方向）
            Vector2 knockbackDir = (transform.position - other.transform.position).normalized;
            
            // 开始击退
            StartCoroutine(KnockbackCoroutine(knockbackDir));
            
            // 对玩家造成伤害
            var player = other.gameObject.GetComponent<CombatPlayerController>();
            if (player != null)
            {
                player.Health -= 1;
                player.OnHealthChanged();
            }
        }
    }
}
