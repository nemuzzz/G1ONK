using UnityEngine;

public class BGMPlayerController : MonoBehaviour
{
    void Awake()
    {
        // 确保只有一个BGMPlayer实例在跨厂家时存在
        if(FindObjectsOfType<BGMPlayerController>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }    
        // 标记该游戏对象在加载新场景时不被销毁
        DontDestroyOnLoad(gameObject);
    }

    // 其他方法和属性...
    
}
