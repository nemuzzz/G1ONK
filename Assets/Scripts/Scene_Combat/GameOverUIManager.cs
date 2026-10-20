using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
// 使用完整命名空间引用，避免命名冲突
using UIButton = UnityEngine.UIElements.Button;
using UILabel = UnityEngine.UIElements.Label;
using UnityEngine.UIElements;

public class GameOverUIManager : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject gameOverPanel;
    public UnityEngine.UI.Text resultText; // 原生UI的Text组件
    
    // UI Toolkit相关变量
    private UIDocument uiDocument;
    private UIButton retryButton; // UI Toolkit的Button组件
    private UIButton returnButton; // UI Toolkit的Button组件，名称从backButton改为returnButton
    private UILabel uiResultText; // UI Toolkit的Label组件
    
    [Header("传送设置")]
    public string worldSceneName = "Scene_Free"; // 世界场景名
    public Vector2 worldSpawnPosition = new Vector2(0, 0); // 世界场景生成位置
    public int targetCircularLayer = 0; // 目标环形层级（环形关卡）
    public float targetRotationAngle = 0f; // 目标旋转角度（环形关卡）
    
    private CombatManager combatManager; // 引用CombatManager来获取传送参数
    
    // 在Awake中绑定按钮事件和获取CombatManager
    private void Awake()
    {
        // 获取CombatManager实例
        combatManager = FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            // 如果找到CombatManager，使用其参数
            worldSceneName = combatManager.GetWorldSceneName();
            worldSpawnPosition = combatManager.GetWorldSpawnPosition();
            targetCircularLayer = combatManager.GetTargetCircularLayer();
            targetRotationAngle = combatManager.GetTargetRotationAngle();
            
            Debug.Log($"从CombatManager获取传送参数: 场景名={worldSceneName}, 位置={worldSpawnPosition}, 层级={targetCircularLayer}, 角度={targetRotationAngle}");
        }
        
        // 获取UIDocument组件
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            // 如果没有找到UIDocument组件，尝试在子对象中找到
            uiDocument = GetComponentInChildren<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("没有找到UIDocument组件！请确保将其添加到GameOverUIManager对象上。");
                return;
            }
        }
    }
    
    // 在OnEnable中绑定按钮事件
    private void OnEnable()
    {
        Debug.Log("GameOverUIManager.OnEnable被调用");
        
        if (uiDocument != null)
        {
            Debug.Log($"UIDocument组件存在，Source Asset: {uiDocument.visualTreeAsset?.name}");
            
            if (uiDocument.rootVisualElement != null)
            {
                Debug.Log("rootVisualElement不为空");
                var root = uiDocument.rootVisualElement;
                
                // 列出所有可用的UI元素
                Debug.Log("可用UI元素:");
                foreach (var element in root.Children())
                {
                    Debug.Log($"- {element.name} (类型: {element.GetType().Name})");
                    
                    // 如果是容器，列出其子元素
                    if (element.childCount > 0)
                    {
                        foreach (var child in element.Children())
                        {
                            Debug.Log($"  - {child.name} (类型: {child.GetType().Name})");
                        }
                    }
                }
                
                // 获取UI Toolkit的按钮和文本组件
                uiResultText = root.Q<UILabel>("ResultText");
                retryButton = root.Q<UIButton>("RetryButton");
                returnButton = root.Q<UIButton>("ReturnButton"); // 修改为ReturnButton以匹配UXML
                
                Debug.Log($"ResultText查找结果: {(uiResultText != null ? "成功" : "失败")}");
                Debug.Log($"RetryButton查找结果: {(retryButton != null ? "成功" : "失败")}");
                Debug.Log($"ReturnButton查找结果: {(returnButton != null ? "成功" : "失败")}");
                
                // 绑定按钮事件
                if (retryButton != null)
                {
                    retryButton.clicked += OnRetryClicked;
                    Debug.Log("绑定重试按钮事件成功！");
                }
                else
                {
                    Debug.LogError("没有找到RetryButton组件！");
                }
                
                if (returnButton != null)
                {
                    returnButton.clicked += OnReturnToWorldClicked;
                    Debug.Log("绑定返回世界按钮事件成功！");
                }
                else
                {
                    Debug.LogError("没有找到ReturnButton组件！");
                }
            }
            else
            {
                Debug.LogError("rootVisualElement为空！");
            }
        }
        else
        {
            Debug.LogError("UIDocument组件为空！");
        }
    }
    
    // 初始化游戏结束UI
    public void InitializeGameOverUI(bool isWin)
    {
        // 设置标题文本
        if (uiResultText != null)
        {
            // 保持UXML中的文本不变
            // uiResultText.text = isWin ? "胜利！" : "失败！";
            Debug.Log($"保持标题文本：{uiResultText.text}");
        }
        else
        {
            Debug.LogError("ResultText组件为空！");
        }
        
        // 显示游戏结束面板
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else if (uiDocument != null)
        {
            uiDocument.enabled = true;
            Debug.Log("显示UI Toolkit面板！");
        }
    }
    
    // 重试按钮点击事件
    public void OnRetryClicked()
    {
        Debug.Log("点击了重试按钮！");
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // 返回世界按钮点击事件
    public void OnReturnToWorldClicked()
    {
        Debug.Log("点击了返回世界按钮！");
        // 传送到世界场景
        TransToWorldScene();
    }
    
    // 传送到世界场景
    private void TransToWorldScene()
    {
        Debug.Log("正在执行TransToWorldScene方法！");
        
        // 如果需要传送到环形关卡的特定层级和角度
        if (worldSceneName.Contains("Area1"))
        {
            Debug.Log($"设置环形关卡传送参数：层级={targetCircularLayer}, 角度={targetRotationAngle}");
            CircularLevelData.SetPendingCircularTransData(targetCircularLayer, targetRotationAngle);
        }
        
        // 保存传送目标位置
        TransArea.SetPendingTransLocation(worldSpawnPosition);
        Debug.Log($"设置传送目标位置：{worldSpawnPosition}");
        
        // 销毁当前玩家和相机
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null)
        {
            Debug.Log("销毁当前玩家！");
            Destroy(currentPlayer);
        }
        
        // 获取主相机
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.gameObject != null)
        {
            // 如果相机不是Main Camera，销毁它
            if (mainCamera.gameObject.name != "Main Camera")
            {
                Debug.Log("销毁非Main Camera相机！");
                Destroy(mainCamera.gameObject);
            }
        }
        
        // 加载世界场景
        Debug.Log($"加载世界场景：{worldSceneName}");
        SceneManager.LoadScene(worldSceneName, LoadSceneMode.Single);
    }
}
