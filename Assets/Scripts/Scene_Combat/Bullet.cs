using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] float Speed = 300;
    [SerializeField] float LifeTime = 2;
    
    private void Start()
    {
        // 播放发射音效
        if (CombatAudioManager.Instance != null)
        {
            CombatAudioManager.Instance.PlayShootSound();
        }
        
        Destroy(gameObject, LifeTime);
    }

    private void FixedUpdate()
    {
        transform.Translate(Speed * Time.fixedDeltaTime * Vector3.up, Space.Self);
    }
}
