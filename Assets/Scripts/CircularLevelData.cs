using UnityEngine;

/// <summary>
/// 静态类，用于存储圆形关卡的传送数据
/// 确保这些数据在场景加载时不会被重置
/// </summary>
public static class CircularLevelData
{
    // 传送目标层级
    private static int pendingTargetLayer = -1;
    
    // 传送目标旋转角度
    private static float pendingRotationAngle = 0f;
    
    // 是否有待处理的传送
    private static bool hasPendingCircularTrans = false;
    
    /// <summary>
    /// 设置圆形关卡的传送参数
    /// </summary>
    /// <param name="targetLayer">目标层级</param>
    /// <param name="rotationAngle">目标旋转角度</param>
    public static void SetPendingCircularTransData(int targetLayer, float rotationAngle)
    {
        pendingTargetLayer = targetLayer;
        pendingRotationAngle = rotationAngle;
        hasPendingCircularTrans = true;
        
        Debug.Log($"设置圆形关卡传送数据: 层级={targetLayer}, 角度={rotationAngle}");
    }
    
    /// <summary>
    /// 获取是否有待处理的圆形关卡传送
    /// </summary>
    public static bool HasPendingCircularTrans()
    {
        return hasPendingCircularTrans;
    }
    
    /// <summary>
    /// 获取待处理的目标层级
    /// </summary>
    public static int GetPendingTargetLayer()
    {
        return pendingTargetLayer;
    }
    
    /// <summary>
    /// 获取待处理的旋转角度
    /// </summary>
    public static float GetPendingRotationAngle()
    {
        return pendingRotationAngle;
    }
    
    /// <summary>
    /// 清除待处理的传送数据
    /// </summary>
    public static void ClearPendingCircularTrans()
    {
        hasPendingCircularTrans = false;
        pendingTargetLayer = -1;
        pendingRotationAngle = 0f;
        
        Debug.Log("已清除圆形关卡传送数据");
    }
}
