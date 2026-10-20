using UnityEngine;

/// <summary>
/// 透视缩放器 - 根据Y坐标调整对象的缩放，实现近大远小的透视效果
/// </summary>
public class PerspectiveScaler : MonoBehaviour
{
    [Header("透视缩放设置")]
    [Tooltip("Y坐标最小值（最近处）")]
    [SerializeField] private float minY = -5f;
    
    [Tooltip("Y坐标最大值（最远处）")]
    [SerializeField] private float maxY = 5f;
    
    [Tooltip("最近处的缩放比例")]
    [SerializeField] private float maxScale = 1.5f;
    
    [Tooltip("最远处的缩放比例")]
    [SerializeField] private float minScale = 0.5f;
    
    [Tooltip("缩放曲线，控制缩放变化的平滑度")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Tooltip("是否在编辑器中实时更新（仅用于调试）")]
    [SerializeField] private bool updateInEditor = false;
    
    // 原始缩放值
    private Vector3 originalScale;
    
    private void Start()
    {
        // 保存原始缩放值
        originalScale = transform.localScale;
    }
    
    private void Update()
    {
        // 根据Y坐标更新缩放
        UpdateScale();
    }
    
    // 根据Y坐标更新缩放
    private void UpdateScale()
    {
        // 获取当前Y坐标
        float currentY = transform.position.y;
        
        // 计算缩放比例（0-1之间）
        float t = Mathf.InverseLerp(minY, maxY, currentY);
        
        // 应用缩放曲线
        float curveValue = scaleCurve.Evaluate(t);
        
        // 计算最终缩放值
        float finalScale = Mathf.Lerp(maxScale, minScale, curveValue);
        
        // 应用缩放
        transform.localScale = originalScale * finalScale;
    }
    
#if UNITY_EDITOR
    // 在编辑器中实时更新（用于调试）
    private void OnDrawGizmos()
    {
        if (updateInEditor && Application.isEditor && !Application.isPlaying)
        {
            // 如果originalScale未初始化，则初始化它
            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
            }
            
            // 更新缩放
            UpdateScale();
        }
    }
#endif
}
