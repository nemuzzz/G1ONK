using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using DG.Tweening;
using UnityEngine.SceneManagement;

[Serializable]
public enum ArrowDirection
{
    Left,
    Right,
    Up,
    Down
}

[Serializable]
public struct ArrowUIDefinition
{
    [SerializeField] public ArrowDirection ArrowDirection;
    [SerializeField] public Texture2D ArrowButton;
    [SerializeField] public Texture2D ArrowButton_Failed;
    [SerializeField] public Texture2D ArrowButton_Correct;
}

public class CombatManager : MonoBehaviour
{
    [Serializable]
    enum ArrowDirection : uint
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3,
        MAX
    }
    
    static readonly List<KeyCode> detectKeyCodes = new() {
        KeyCode.A, KeyCode.LeftArrow, KeyCode.D, KeyCode.RightArrow, KeyCode.W,
        KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow
    };

    [SerializeField] GameObject EnemyPrefab;
    [SerializeField] float EnemySpawnInterval = 3.5f;
    [SerializeField] UIDocument CombatUIPanel;
    [SerializeField] Texture2D IndicatorTexture;
    
    [Header("敌人图标设置")]
    [SerializeField] private Texture2D EnemyIconTexture; // 可替换的敌人图标
    [SerializeField] private string EnemyIconElementName = "EnemyIcon"; // 敌人图标元素名称
    
    [SerializeField] VisualTreeAsset ArrowTemplate;
    [SerializeField] ArrowUIDefinition ArrowDefinition_Left;
    [SerializeField] ArrowUIDefinition ArrowDefinition_Right;
    [SerializeField] ArrowUIDefinition ArrowDefinition_Up;
    [SerializeField] ArrowUIDefinition ArrowDefinition_Down;

    List<ArrowDirection> CurrentDirections = new();
    List<ArrowDirection> InputDirections = new();


    [field:Header("Runtime Properties")]
    public int BulletCnt = 0;
    public int ArrowCnt = 6;
    public int RemainEnemyCnt = 5;
    public int PlayerHealth = 3; // 添加玩家生命值变量
    
    private ScrollView ArrowListScrollView;
    private Label BulletCntText;
    private Label RemainEnemyCntText;
    private VisualElement Indicator;
    private int OriginEnemyCnt = 0;
    private int SpawnedEnemyCnt = 0;
    private int totalBulletsFired = 0; // 总共发射的子弹数量
    private int wrongInputCount = 0; // 错误输入次数

    // 生命值UI元素
    [Header("Health UI Settings")]
    [SerializeField] private VisualElement HealthContainer; // 生命值图标的容器
    [SerializeField] private Texture2D HealthTexture; // 正常生命值图标
    [SerializeField] private Texture2D LostHealthTexture; // 失去生命值图标
    [SerializeField] private float HealthIconSpacing = 5f; // 生命值图标间距
    [SerializeField] private Vector2 HealthIconSize = new Vector2(30, 30); // 生命值图标大小
    [SerializeField] private Vector2 HealthIconStartPosition = new Vector2(10, 10); // 第一个生命值图标的位置
    private VisualElement[] HealthIcons;
    [SerializeField] private VisualTreeAsset GameOverPanel; // 游戏结束面板
    [SerializeField] private VisualTreeAsset VictoryPanel; // 胜利面板
    [SerializeField] private Texture2D EarnedCoinTexture; // 获得的金币图片
    [SerializeField] private Texture2D NotEarnedCoinTexture; // 未获得的金币图片
    [SerializeField] private string WorldSceneName = "Scene_Free"; // 世界场景名称
    [SerializeField] private Vector2 WorldSpawnPosition = new Vector2(0, 0); // 世界场景出生点
    
    [Header("圆形关卡传送设置")]
    [SerializeField] private int targetCircularLayer = 0; // 目标层级
    [SerializeField] private float targetRotationAngle = 0f; // 目标旋转角度
    
    // 暂停按钮相关
    public GameObject PauseButtonGameObject; // 在Unity编辑器中分配
    private bool isPauseButtonInitialized = false;
    
    private bool isGameOver = false;
    
    private bool bShouldResponInput = true;
    
    // UI GameObjects
    [Header("UI GameObjects")]
    [SerializeField] private GameObject victoryUIObject; // 胜利UI对象
    [SerializeField] private GameObject gameOverUIObject; // 游戏结束UI对象
    
    // 获取传送参数的公共方法
    public string GetWorldSceneName() { return WorldSceneName; }
    public Vector2 GetWorldSpawnPosition() { return WorldSpawnPosition; }
    public int GetTargetCircularLayer() { return targetCircularLayer; }
    public float GetTargetRotationAngle() { return targetRotationAngle; }
    
    void Start()
    {
        var rootElement = CombatUIPanel.rootVisualElement;
        ArrowListScrollView = rootElement.Q<ScrollView>("ArrowListScrollView");
        BulletCntText = rootElement.Q<Label>("BulletCntText");
        RemainEnemyCntText = rootElement.Q<Label>("RemainEnemyCntText");

        // 更新敌人图标
        UpdateEnemyIcon();

        // 注册暂停按钮点击事件
        var pauseButton = rootElement.Q<Button>("PauseButton");
        if (pauseButton != null)
        {
            Debug.Log("找到暂停按钮，正在注册点击事件...");
            // 移除可能已有的点击事件
            pauseButton.clicked -= OnPauseButtonClicked;
            pauseButton.clicked += OnPauseButtonClicked;
            
            // 添加按钮点击音效
            pauseButton.clicked += () => {
                Debug.Log("暂停按钮被点击，播放按钮点击音效");
                CombatAudioManager.Instance.PlayButtonClickSound();
            };
            
            Debug.Log("注册暂停按钮点击事件成功");
        }
        else
        {
            Debug.LogError("未找到暂停按钮！");
        }

        OriginEnemyCnt = RemainEnemyCnt;
        
        SetBulletCnt(BulletCnt);
        SetRemainEnemyCnt(RemainEnemyCnt);
        
        // 初始化生命值UI
        InitializeHealthUI();
        
        RefreshArrowListUI(ArrowCnt);
        SpawnEnemy();
        InvokeRepeating(nameof(SpawnEnemy), EnemySpawnInterval, EnemySpawnInterval);
        
        // 注册按钮点击音效
        if (CombatAudioManager.Instance != null)
        {
            CombatAudioManager.Instance.RegisterButtonClickSound(CombatUIPanel);
        }
    }
    
    // 暂停按钮点击事件
    private void OnPauseButtonClicked()
    {
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    void Update()
    {
        if (!bShouldResponInput)
            return;
        
        // 检测暂停按钮的键盘快捷键（按下P键）
        CheckPauseButtonInput();

        ArrowDirection InputDirection = ArrowDirection.MAX;
        List<KeyCode> InputKeyCodes = GetInputKeyCode();
        if (InputKeyCodes.Count > 0)
        {
            if (InputKeyCodes.Count > 1)
            {
                OnInputWrong();
            }
            InputDirection = ConvertKeyCodeToDirection(InputKeyCodes[0]);
            InputDirections.Add(InputDirection);
            
            // 播放Arrow输入音效
            if (CombatAudioManager.Instance != null)
            {
                CombatAudioManager.Instance.PlayArrowInputSound();
            }
            
            CheckInputDirections();
        }
    }

    void OnInputWrong()
    {
        print("Wrong!");
        wrongInputCount++; // 增加错误输入次数
        Debug.Log($"错误输入次数增加: {wrongInputCount}");
        
        // 播放输入错误音效
        if (CombatAudioManager.Instance != null)
        {
            CombatAudioManager.Instance.PlayWrongInputSound();
        }
        
        StartCoroutine(OnWrongAnimationCoroutine());
    }

    IEnumerator OnWrongAnimationCoroutine()
    {
        bShouldResponInput = false;
        var targetElement = ArrowListScrollView;
    
        if (targetElement == null)
        {
            Debug.LogError("Target element not found!");
            yield break;
        }

        // 定义抖动参数
        float[] offsets = { 10f, -10f, 8f, -8f, 5f, -5f, 0f };
        float duration = 0.1f;
        var originalPos = targetElement.transform.position;
    
        Sequence shakeSequence = DOTween.Sequence();
    
        foreach (float offset in offsets)
        {
            shakeSequence.Append(DOTween.To(
                () => targetElement.transform.position,
                pos => targetElement.transform.position = pos,
                originalPos + new Vector3(offset, 0, 0),
                duration));
        }
    
        yield return shakeSequence.WaitForCompletion();
        RefreshArrowListUI(ArrowCnt);
        bShouldResponInput = true;
    }

    void OnInputCorrect()
    {
        print("Correct!");
        totalBulletsFired++; // 增加子弹生产数量
        Debug.Log($"子弹生产数量增加: {totalBulletsFired}");
        
        // 播放子弹添加音效
        if (CombatAudioManager.Instance != null)
        {
            CombatAudioManager.Instance.PlayBulletAddedSound();
        }
        
        SetBulletCnt(BulletCnt + 1);
        RefreshArrowListUI(ArrowCnt);
    }

    public void SetBulletCnt(int Cnt)
    {
        BulletCnt = Cnt;
        BulletCntText.text = $"{BulletCnt:D2}";
    }

    public void ReduceRemainEnemyCnt(int Reduce)
    {
        SetRemainEnemyCnt(RemainEnemyCnt - Reduce);
    }

    public void SetRemainEnemyCnt(int Cnt)
    {
        RemainEnemyCnt = Cnt;
        RemainEnemyCntText.text = $"{RemainEnemyCnt:D2}";

        if (RemainEnemyCnt <= 0 && !isGameOver)
        {
            // 游戏结束
            GameOver(true);
        }
    }

    // 减少玩家生命值
    public void ReducePlayerHealth(int amount = 1)
    {
        if (isGameOver) return;
        
        PlayerHealth -= amount;
        UpdateHealthUI();
        
        if (PlayerHealth <= 0)
        {
            // 游戏结束
            GameOver(false);
        }
    }
    
    // 游戏结束
    public void GameOver(bool isWin)
    {
        if (isGameOver) return;
        
        isGameOver = true;
        print(isWin ? "GameOver You Win!!" : "GameOver You Lose!!");
        
        // 播放游戏结束音效
        if (CombatAudioManager.Instance != null)
        {
            if (isWin)
            {
                CombatAudioManager.Instance.PlayVictorySound();
            }
            else
            {
                CombatAudioManager.Instance.PlayGameOverSound();
            }
        }
        
        // 停止所有敌人
        StopAllEnemies();
        
        // 显示游戏结束面板
        if (isWin)
        {
            ShowVictoryPanel();
        }
        else
        {
            ShowGameOverPanel(false);
        }
    }
    
    // 停止所有敌人
    private void StopAllEnemies()
    {
        // 取消敌人生成
        CancelInvoke(nameof(SpawnEnemy));
        
        // 停止所有敌人
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            enemy.StopMoving();
        }
    }
    
    // 显示游戏结束面板
    private void ShowGameOverPanel(bool isWin)
    {
        // 取消敌人生成
        CancelInvoke(nameof(SpawnEnemy));
        
        // 停止玩家输入
        bShouldResponInput = false;
        
        // 显示游戏结束面板
        if (gameOverUIObject != null)
        {
            GameOverUIManager gameOverUI = gameOverUIObject.GetComponent<GameOverUIManager>();
            if (gameOverUI != null)
            {
                gameOverUI.InitializeGameOverUI(isWin);
                return;
            }
        }
        
        var rootElement = CombatUIPanel.rootVisualElement;
        
        // 先清除可能已存在的面板
        var existingPanel = rootElement.Q("GameOverPanel");
        if (existingPanel != null)
        {
            rootElement.Remove(existingPanel);
        }
        
        if (GameOverPanel != null)
        {
            var gameOverElement = GameOverPanel.Instantiate();
            
            // 确保面板位于最上层
            gameOverElement.style.position = Position.Absolute;
            gameOverElement.style.top = 0;
            gameOverElement.style.bottom = 0;
            gameOverElement.style.left = 0;
            gameOverElement.style.right = 0;
            gameOverElement.style.alignItems = Align.Center;
            gameOverElement.style.justifyContent = Justify.Center;
            
            rootElement.Add(gameOverElement);
            
            var resultText = gameOverElement.Q<Label>("ResultText");
            if (resultText != null) 
            {
                resultText.text = isWin ? "你成功了" : "你输掉了";
            }
            
            // 添加重试按钮事件
            var retryButton = gameOverElement.Q<Button>("RetryButton");
            if (retryButton != null)
            {
                retryButton.clicked += () => {
                    // 重试
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                };
            }
            
            // 添加返回世界按钮事件
            var returnButton = gameOverElement.Q<Button>("ReturnButton");
            if (returnButton != null)
            {
                returnButton.clicked += () => {
                    // 返回世界场景
                    TransToWorldScene();
                };
            }
        }
        else
        {
            // 如果游戏结束面板没有被设置，创建一个
            var gameOverElement = new VisualElement();
            gameOverElement.name = "GameOverPanel";
            gameOverElement.style.position = Position.Absolute;
            gameOverElement.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            gameOverElement.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            gameOverElement.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.8f));
            gameOverElement.style.justifyContent = Justify.Center;
            gameOverElement.style.alignItems = Align.Center;
            
            var resultLabel = new Label();
            resultLabel.text = isWin ? "你成功了" : "你输掉了"; // 修改为固定显示
            resultLabel.style.fontSize = 48;
            resultLabel.style.color = new StyleColor(Color.white);
            resultLabel.style.marginBottom = 20;
            gameOverElement.Add(resultLabel);
            
            var retryButton = new Button();
            retryButton.text = "重试";
            retryButton.style.width = 200;
            retryButton.style.height = 60;
            retryButton.style.fontSize = 24;
            retryButton.clicked += () => {
                // 重试
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            };
            gameOverElement.Add(retryButton);
            
            var returnButton = new Button();
            returnButton.text = "返回世界";
            returnButton.style.width = 200;
            returnButton.style.height = 60;
            returnButton.style.fontSize = 24;
            returnButton.clicked += () => {
                // 返回世界场景
                TransToWorldScene();
            };
            gameOverElement.Add(returnButton);
            
            rootElement.Add(gameOverElement);
        }
    }

    // 显示胜利面板
    private void ShowVictoryPanel()
    {
        // 停止接收输入
        bShouldResponInput = false;
        
        Debug.Log($"显示胜利面板: 子弹数={totalBulletsFired}, 错误数={wrongInputCount}, 生命={PlayerHealth}");
        
        if (victoryUIObject != null)
        {
            VictoryUIManager victoryUI = victoryUIObject.GetComponent<VictoryUIManager>();
            if (victoryUI != null)
            {
                victoryUI.gameObject.SetActive(true);
                victoryUI.InitializeVictoryUI(totalBulletsFired, wrongInputCount, PlayerHealth);
                return;
            }
        }
        
        var rootElement = CombatUIPanel.rootVisualElement;
        
        // 先清除可能已存在的面板
        var existingPanel = rootElement.Q("VictoryPanel");
        if (existingPanel != null)
        {
            rootElement.Remove(existingPanel);
        }
        
        if (VictoryPanel != null)
        {
            var victoryElement = VictoryPanel.Instantiate();
            
            // 确保面板位于最上层
            victoryElement.style.position = Position.Absolute;
            victoryElement.style.top = 0;
            victoryElement.style.bottom = 0;
            victoryElement.style.left = 0;
            victoryElement.style.right = 0;
            victoryElement.style.alignItems = Align.Center;
            victoryElement.style.justifyContent = Justify.Center;
            
            rootElement.Add(victoryElement);
            
            // 设置统计数据
            var bulletsFiredText = victoryElement.Q<Label>("BulletsFiredText");
            if (bulletsFiredText != null)
            {
                bulletsFiredText.text = totalBulletsFired.ToString();
            }
            
            var wrongInputsText = victoryElement.Q<Label>("WrongInputsText");
            if (wrongInputsText != null)
            {
                wrongInputsText.text = wrongInputCount.ToString();
            }
            
            // 显示剩余生命
            var healthIconsContainer = victoryElement.Q<VisualElement>("HealthIconsContainer");
            if (healthIconsContainer != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var healthIcon = new VisualElement();
                    healthIcon.style.width = 30;
                    healthIcon.style.height = 30;
                    healthIcon.style.marginLeft = 5;
                    healthIcon.style.marginRight = 5;
                    
                    if (i < PlayerHealth)
                    {
                        healthIcon.style.backgroundImage = new StyleBackground(HealthTexture);
                    }
                    else
                    {
                        healthIcon.style.backgroundImage = new StyleBackground(LostHealthTexture);
                    }
                    
                    healthIconsContainer.Add(healthIcon);
                }
            }
            
            // 计算获得的金币
            int earnedCoins = 0;
            
            // 条件1：错误输入次数低于5次
            bool condition1 = wrongInputCount < 5;
            if (condition1) earnedCoins++;
            
            // 条件2：剩余生命为2
            bool condition2 = PlayerHealth == 2;
            if (condition2) earnedCoins++;
            
            // 条件3：剩余生命为3（满血）
            bool condition3 = PlayerHealth == 3;
            if (condition3) earnedCoins += 2; // 满血获得2个金币
            
            // 显示金币
            var coin1 = victoryElement.Q<VisualElement>("Coin1");
            var coin2 = victoryElement.Q<VisualElement>("Coin2");
            var coin3 = victoryElement.Q<VisualElement>("Coin3");
            
            if (coin1 != null && EarnedCoinTexture != null && NotEarnedCoinTexture != null)
            {
                // 条件1的金币
                coin1.style.backgroundImage = condition1 ? 
                    new StyleBackground(EarnedCoinTexture) : 
                    new StyleBackground(NotEarnedCoinTexture);
                
                // 条件2的金币
                if (condition3) // 如果是满血，显示两个金币
                {
                    coin2.style.backgroundImage = new StyleBackground(EarnedCoinTexture);
                    coin3.style.backgroundImage = new StyleBackground(EarnedCoinTexture);
                }
                else if (condition2) // 如果是2点血，显示一个金币
                {
                    coin2.style.backgroundImage = new StyleBackground(EarnedCoinTexture);
                    coin3.style.backgroundImage = new StyleBackground(NotEarnedCoinTexture);
                }
                else // 如果血量低于2，不显示金币
                {
                    coin2.style.backgroundImage = new StyleBackground(NotEarnedCoinTexture);
                    coin3.style.backgroundImage = new StyleBackground(NotEarnedCoinTexture);
                }
            }
            
            // 添加按钮事件
            var returnButton = victoryElement.Q<Button>("ReturnButton");
            if (returnButton != null)
            {
                returnButton.clicked += () => {
                    // 返回世界场景
                    TransToWorldScene();
                };
            }
            
            var retryButton = victoryElement.Q<Button>("RetryButton");
            if (retryButton != null)
            {
                retryButton.clicked += () => {
                    // 重新加载当前场景
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                };
            }
        }
        else
        {
            Debug.LogError("VictoryPanel asset is not assigned!");
        }
    }
    
    // 传送到世界场景
    private void TransToWorldScene()
    {
        // 如果要传送到圆形关卡的特定层级和角度
        if (WorldSceneName.Contains("Area1"))
        {
            // 设置圆形关卡传送参数（这里可以根据需要设置默认值或从配置中读取）
            int targetLayer = targetCircularLayer; // 目标层级
            float targetAngle = targetRotationAngle; // 目标旋转角度
            
            Debug.Log($"设置圆形关卡传送参数: 层级={targetLayer}, 角度={targetAngle}");
            CircularLevelData.SetPendingCircularTransData(targetLayer, targetAngle);
        }
        
        // 保存传送目标位置
        TransArea.SetPendingTransLocation(WorldSpawnPosition);
        
        // 销毁当前Player和相机
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }
        
        // 获取主相机
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.gameObject != null)
        {
            // 如果相机不是默认的Main Camera，就销毁它
            if (mainCamera.gameObject.name != "Main Camera")
            {
                Destroy(mainCamera.gameObject);
            }
        }
        
        // 加载世界场景
        UnityEngine.SceneManagement.SceneManager.LoadScene(WorldSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void CheckInputDirections()
    {
        int i = InputDirections.Count - 1;
        if (CurrentDirections[i] != InputDirections[i])
        {
            ArrowListUIResponeInput(false, i);
            OnInputWrong();
            return;
        }
        else
        {
            ArrowListUIResponeInput(true, i);
        }

        if (CurrentDirections.Count == InputDirections.Count)
        {
            OnInputCorrect();
        }
    }

    private void ArrowListUIResponeInput(bool bCorrect, int CurIndex)
    {
        VisualElement ArrowElement = null;
        ArrowDirection r = InputDirections[CurIndex];
        switch (r)
        {
            case ArrowDirection.Left:
                ArrowElement = CreateArrowVisualElementFromImage(bCorrect ? ArrowDefinition_Left.ArrowButton_Correct : ArrowDefinition_Left.ArrowButton_Failed);
                break;
            case ArrowDirection.Right:
                ArrowElement = CreateArrowVisualElementFromImage(bCorrect ? ArrowDefinition_Right.ArrowButton_Correct : ArrowDefinition_Right.ArrowButton_Failed);
                break;
            case ArrowDirection.Up:
                ArrowElement = CreateArrowVisualElementFromImage(bCorrect ? ArrowDefinition_Up.ArrowButton_Correct : ArrowDefinition_Up.ArrowButton_Failed);
                break;
            case ArrowDirection.Down:
                ArrowElement = CreateArrowVisualElementFromImage(bCorrect ? ArrowDefinition_Down.ArrowButton_Correct : ArrowDefinition_Down.ArrowButton_Failed);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ArrowListScrollView[CurIndex].Q<Button>("ArrowButton").style.backgroundImage = ArrowElement.Q<Button>("ArrowButton").style.backgroundImage;
        UpdateIndicatorPosition(CurIndex);
    }

 private void UpdateIndicatorPosition(int MoveTargetIndex)
{
    if (MoveTargetIndex < 0 || MoveTargetIndex > ArrowListScrollView.childCount)
        return;
    
    var targetArrow = ArrowListScrollView[MoveTargetIndex].Q<Button>("ArrowButton");
    
    // 使用layout而不是worldBound
    var targetRect = targetArrow.layout;
    
    // 将局部坐标转换为世界坐标
    Vector2 worldPos = targetArrow.LocalToWorld(new Vector2(targetRect.x, targetRect.y));
    
    print($"Indicator {MoveTargetIndex} {worldPos}");

    bool init = false;
    if (Indicator == null)
    {
        init = true;
        // 创建指示器
        Indicator = new VisualElement();
        Indicator.name = "Indicator";
        Indicator.style.position = Position.Absolute;
        Indicator.style.backgroundImage = IndicatorTexture;
        Indicator.style.width = 180;
        Indicator.style.height = 180;
        Indicator.pickingMode = PickingMode.Ignore;
        CombatUIPanel.rootVisualElement.Add(Indicator);
        Indicator.style.left = 577;
        Indicator.style.top = 906;
    }
    
    // 获取当前Indicator的位置和大小
    Vector4 currentBound = new Vector4(
        Indicator.style.left.value.value,
        Indicator.style.top.value.value,
        Indicator.style.width.value.value,
        Indicator.style.height.value.value
    );
    
    // 目标位置和大小 (确保width和height是有效的)
    Vector4 targetPosition = new Vector4(
        worldPos.x,
        worldPos.y,
        targetRect.width > 0 ? targetRect.width : currentBound.z,
        targetRect.height > 0 ? targetRect.height : currentBound.w
    );
    
    float duration = init ? 0 : 0.2f;
    
    
    // 使用DoTween进行插值动画
    DOTween.To(
        () => currentBound,
        value =>
        {
            Indicator.style.left = value.x;
            Indicator.style.top = value.y;
            Indicator.style.width = value.z;
            Indicator.style.height = value.w;
        },
        targetPosition,
        duration
    ).SetEase(Ease.OutQuad);
}

    private VisualElement CreateArrowVisualElementFromImage(Texture2D InTexture)
    {
        var t = ArrowTemplate.Instantiate();
        var button = t.Q<Button>("ArrowButton");
        button.style.backgroundImage = new StyleBackground(InTexture);
        return t;
    }

    private ArrowDirection ConvertKeyCodeToDirection(KeyCode keyCode)
    {
        if (!detectKeyCodes.Contains(keyCode))
            return ArrowDirection.MAX;

        int index = detectKeyCodes.IndexOf(keyCode);
        ArrowDirection direction = (ArrowDirection)(index / 2);
        print($"KeyCode:{keyCode}, Direction:{direction}");
        return direction;
    }

    private void SpawnEnemy()
    {
        if (OriginEnemyCnt - SpawnedEnemyCnt > 0)
        {
            var Enemy = Instantiate(EnemyPrefab, 
                GetRandomPositionAtViewportEdge(), Quaternion.identity).GetComponent<Enemy>();
            Enemy.SetCombatManager(this);
            SpawnedEnemyCnt++;
        }
        else
        {
            CancelInvoke(nameof(SpawnEnemy));
        }
    }

    private Vector2 GetRandomPositionAtViewportEdge()
    {
        int edge = Random.Range(0, 4);
        
        float randomX = Random.Range(0f, 1f);
        float randomY = Random.Range(0f, 1f);

        Vector2 viewportPosition = Vector2.zero;

        switch (edge)
        {
            case 0:
                viewportPosition = new Vector2(randomX, 1 + 0.1f);
                break;
            case 1:
                viewportPosition = new Vector2(1 + 0.1f, randomY);
                break;
            case 2:
                viewportPosition = new Vector2(randomX, -0.1f);
                break;
            case 3:
                viewportPosition = new Vector2(-0.1f, randomY);
                break;
        }
        
        return Camera.main.ViewportToWorldPoint(viewportPosition);
    }

    private List<KeyCode> GetInputKeyCode()
    {
        List<KeyCode> pressedKeys = new List<KeyCode>();
        
        foreach (KeyCode keyCode in detectKeyCodes)
        {
            if (Input.GetKeyDown(keyCode))
            {
                pressedKeys.Add(keyCode);
            }
        }

        return pressedKeys;
    }

    private void RefreshArrowListUI(int len)
    {
        ArrowListScrollView.Clear();
        CurrentDirections.Clear();
        InputDirections.Clear();
        
        for (int i = 0; i < len; ++i)
        {
            VisualElement ArrowElement = null;
            ArrowDirection r = (ArrowDirection)Random.Range(0, 4);
            switch (r)
            {
                case ArrowDirection.Left:
                    ArrowElement = CreateArrowVisualElementFromImage(ArrowDefinition_Left.ArrowButton);
                    break;
                case ArrowDirection.Right:
                    ArrowElement = CreateArrowVisualElementFromImage(ArrowDefinition_Right.ArrowButton);
                    break;
                case ArrowDirection.Up:
                    ArrowElement = CreateArrowVisualElementFromImage(ArrowDefinition_Up.ArrowButton);
                    break;
                case ArrowDirection.Down:
                    ArrowElement = CreateArrowVisualElementFromImage(ArrowDefinition_Down.ArrowButton);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            ArrowListScrollView.Add(ArrowElement);
            CurrentDirections.Add(r);
        }
        
        ArrowListScrollView.MarkDirtyRepaint();
        
        UpdateIndicatorPosition(0);
    }

    // 初始化生命值UI
    private void InitializeHealthUI()
    {
        if (HealthContainer == null)
        {
            HealthContainer = CombatUIPanel.rootVisualElement.Q<VisualElement>("HealthContainer");
            
            // 如果UI中没有HealthContainer，创建一个
            if (HealthContainer == null)
            {
                HealthContainer = new VisualElement();
                HealthContainer.name = "HealthContainer";
                HealthContainer.style.position = Position.Absolute;
                HealthContainer.style.width = 200;
                HealthContainer.style.height = 50;
                HealthContainer.style.top = 20;
                HealthContainer.style.left = 20;
                HealthContainer.style.flexDirection = FlexDirection.Row;
                
                CombatUIPanel.rootVisualElement.Add(HealthContainer);
            }
        }

        if (HealthTexture == null || LostHealthTexture == null)
        {
            Debug.LogError("生命值UI图标未设置！");
            return;
        }

        // 清空容器
        HealthContainer.Clear();
        HealthIcons = new VisualElement[PlayerHealth];

        // 创建生命值图标
        for (int i = 0; i < PlayerHealth; i++)
        {
            var healthIcon = new VisualElement();
            healthIcon.style.backgroundImage = new StyleBackground(HealthTexture);
            healthIcon.style.width = HealthIconSize.x;
            healthIcon.style.height = HealthIconSize.y;
            healthIcon.style.position = Position.Absolute;
            healthIcon.style.left = HealthIconStartPosition.x + (HealthIconSize.x + HealthIconSpacing) * i;
            healthIcon.style.top = HealthIconStartPosition.y;
            
            HealthContainer.Add(healthIcon);
            HealthIcons[i] = healthIcon;
        }
    }

    // 更新生命值UI
    private void UpdateHealthUI()
    {
        for (int i = 0; i < HealthIcons.Length; i++)
        {
            if (i < PlayerHealth)
            {
                // 显示正常生命值图标
                HealthIcons[i].style.backgroundImage = new StyleBackground(HealthTexture);
            }
            else
            {
                // 显示失去生命值图标
                HealthIcons[i].style.backgroundImage = new StyleBackground(LostHealthTexture);
            }
        }
    }

    // 更新敌人图标
    private void UpdateEnemyIcon()
    {
        var rootElement = CombatUIPanel.rootVisualElement;
        var enemyIcon = rootElement.Q<Button>(EnemyIconElementName);
        if (enemyIcon != null && EnemyIconTexture != null)
        {
            enemyIcon.style.backgroundImage = new StyleBackground(EnemyIconTexture);
            Debug.Log("已更新敌人图标");
        }
        else
        {
            Debug.LogWarning("未找到敌人图标元素或未设置敌人图标纹理");
        }
    }

    private void CheckPauseButtonInput()
    {
        // 检测暂停按钮的键盘快捷键（按下P键）
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("按下P键或Esc键，触发暂停功能");
            // 播放按钮点击音效
            CombatAudioManager.Instance.PlayButtonClickSound();
            OnPauseButtonClicked();
            return;
        }
        
        // 检测鼠标点击
        if (Input.GetMouseButtonDown(0)) // 左键点击
        {
            // 将鼠标位置转换为屏幕坐标
            Vector3 mousePosition = Input.mousePosition;
            
            // 暂停按钮的屏幕坐标范围（右上角）
            Rect pauseButtonRect = new Rect(Screen.width - 110, 10, 100, 100);
            
            // 检测鼠标点击是否在暂停按钮范围内
            if (pauseButtonRect.Contains(new Vector2(mousePosition.x, Screen.height - mousePosition.y)))
            {
                Debug.Log("鼠标点击在暂停按钮范围内");
                // 按钮点击音效
                CombatAudioManager.Instance.PlayButtonClickSound();
                OnPauseButtonClicked();
            }
        }
    }
}
