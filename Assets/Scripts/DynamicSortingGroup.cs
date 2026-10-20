using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 使用SortingGroup组件实现基于Y坐标的动态排序
/// 将此脚本添加到具有多个子物体的父物体上
/// </summary>
public class DynamicSortingGroup : MonoBehaviour
{
    [Tooltip("排序顺序的基础值")]
    public int baseSortingOrder = 0;
    
    [Tooltip("Y坐标到排序顺序的乘数，通常为负值")]
    public float yToOrderMultiplier = -100f;
    
    [Tooltip("Y坐标的偏移量，用于微调排序")]
    public float yOffset = 0f;
    
    [Tooltip("是否在Update中持续更新排序顺序")]
    public bool updateContinuously = true;
    
    // SortingGroup组件引用
    private SortingGroup sortingGroup;
    
    private void Awake()
    {
        // 获取或添加SortingGroup组件
        sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null)
        {
            sortingGroup = gameObject.AddComponent<SortingGroup>();
            Debug.Log($"已自动添加SortingGroup组件到 {gameObject.name}");
        }
        
        // 初始化排序顺序
        UpdateSortingOrder();
    }
    
    private void Update()
    {
        if (updateContinuously)
        {
            UpdateSortingOrder();
        }
    }
    
    /// <summary>
    /// 更新排序顺序
    /// </summary>
    public void UpdateSortingOrder()
    {
        if (sortingGroup != null)
        {
            // 计算基于Y坐标的排序顺序
            // Y坐标越小（越靠下）的物体排序值越大，就会显示在前面
            float adjustedY = transform.position.y + yOffset;
            int calculatedOrder = Mathf.RoundToInt(adjustedY * yToOrderMultiplier) + baseSortingOrder;
            
            // 设置排序顺序
            sortingGroup.sortingOrder = calculatedOrder;
        }
    }
    
    /// <summary>
    /// 手动设置基础排序顺序
    /// </summary>
    public void SetBaseSortingOrder(int newBaseSortingOrder)
    {
        baseSortingOrder = newBaseSortingOrder;
        UpdateSortingOrder();
    }
    
    /// <summary>
    /// 在编辑器中可视化排序区域
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 绘制一条线，表示当前Y坐标
        Gizmos.color = Color.yellow;
        Vector3 position = transform.position;
        Gizmos.DrawLine(
            new Vector3(position.x - 1f, position.y + yOffset, position.z),
            new Vector3(position.x + 1f, position.y + yOffset, position.z)
        );
        
        // 显示当前排序顺序
        int calculatedOrder = Mathf.RoundToInt((position.y + yOffset) * yToOrderMultiplier) + baseSortingOrder;
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            new Vector3(position.x + 1.2f, position.y + yOffset, position.z),
            $"排序顺序: {calculatedOrder}"
        );
#endif
    }
}
