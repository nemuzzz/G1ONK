using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary; // 添加命名空间
using System.Runtime.Serialization; // 添加命名空间

[System.Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public string levelName;
    public int bulletsFired;
    public int wrongInputs;
    public int remainingHealth;
    public int score;
    
    public LeaderboardEntry(string name, string level, int bullets, int errors, int health)
    {
        playerName = name;
        levelName = level;
        bulletsFired = bullets;
        wrongInputs = errors;
        remainingHealth = health;
        
        // 计算分数：每剩余生命+10分，每生产子弹+2分，每错误输入-1分，0错误+5分
        score = (health * 10) + (bullets * 2) - errors;
        if (errors == 0)
        {
            score += 5;
        }
    }
}

public class VictoryUIManager : MonoBehaviour
{
    [Header("UI引用")]
    public UIDocument victoryUIDocument;
    
    [Header("资源")]
    public Texture2D healthSprite;
    public Texture2D lostHealthSprite;
    
    [Header("关卡图标设置")]
    public Texture2D[] levelIcons; // 不同关卡的图标
    public string[] levelNames;    // 对应的关卡名称
    
    [Header("场景设置")]
    private CombatManager combatManager;
    
    // 排行榜数据
    private List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
    private string leaderboardFilePath;
    
    // 当前玩家数据
    private int totalBulletsFired;
    private int wrongInputCount;
    private int playerHealth;
    private string currentLevelName;
    private bool scoreSubmitted = false;
    
    // UI元素
    private VisualElement root;
    private ScrollView leaderboardScroll;
    private VisualElement leaderboardContent;
    private TextField playerNameInput;
    private UnityEngine.UIElements.Button submitScoreButton;
    private UnityEngine.UIElements.Button returnButton;
    private UnityEngine.UIElements.Button retryButton;
    private Label currentBulletsLabel;
    private Label currentErrorsLabel;
    private Label currentScoreLabel;
    private VisualElement currentLevelIcon;
    private VisualElement currentHealthIcons;
    
    private void Awake()
    {
        // 获取CombatManager实例
        combatManager = FindObjectOfType<CombatManager>();
        
        // 设置排行榜文件路径
        leaderboardFilePath = Path.Combine(Application.persistentDataPath, "leaderboard.json");
        
        // 加载排行榜数据
        LoadLeaderboard();
        
        // 默认隐藏胜利UI
        if (victoryUIDocument != null)
        {
            VisualElement root = victoryUIDocument.rootVisualElement;
            if (root != null)
            {
                VisualElement victoryPanel = root.Q("VictoryPanel");
                if (victoryPanel != null)
                {
                    victoryPanel.style.display = DisplayStyle.None;
                    Debug.Log("初始化时隐藏胜利面板");
                }
            }
        }
    }
    
    private void OnEnable()
    {
        // 获取UI元素
        if (victoryUIDocument != null)
        {
            root = victoryUIDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("无法获取UI根元素!");
                return;
            }
            
            Debug.Log("获取UI元素引用");
            
            // 获取排行榜相关元素
            leaderboardScroll = root.Q<ScrollView>("LeaderboardScroll");
            leaderboardContent = root.Q<VisualElement>("LeaderboardContent");
            
            // 获取当前成绩相关元素
            playerNameInput = root.Q<TextField>("PlayerNameInput");
            submitScoreButton = root.Q<UnityEngine.UIElements.Button>("SubmitScoreButton");
            currentBulletsLabel = root.Q<Label>("CurrentBullets");
            currentErrorsLabel = root.Q<Label>("CurrentErrors");
            currentScoreLabel = root.Q<Label>("CurrentScore");
            currentLevelIcon = root.Q<VisualElement>("CurrentLevelIcon");
            currentHealthIcons = root.Q<VisualElement>("CurrentHealthIcons");
            
            // 获取按钮
            returnButton = root.Q<UnityEngine.UIElements.Button>("ReturnButton");
            retryButton = root.Q<UnityEngine.UIElements.Button>("RetryButton");
            
            // 添加按钮事件
            if (submitScoreButton != null)
            {
                submitScoreButton.clicked += OnSubmitScore;
            }
            
            if (returnButton != null)
            {
                returnButton.clicked += OnReturnToWorldClicked;
            }
            
            if (retryButton != null)
            {
                retryButton.clicked += OnRetryClicked;
            }
            
            // 检查UI元素是否正确获取
            CheckUIElements();
            
            // 默认隐藏胜利面板
            VisualElement victoryPanel = root.Q("VictoryPanel");
            if (victoryPanel != null)
            {
                victoryPanel.style.display = DisplayStyle.None;
                Debug.Log("OnEnable时隐藏胜利面板");
            }
        }
    }
    
    // 检查UI元素是否正确获取
    private void CheckUIElements()
    {
        Debug.Log("检查UI元素:");
        Debug.Log($"leaderboardScroll: {(leaderboardScroll != null ? "已获取" : "未获取")}");
        Debug.Log($"leaderboardContent: {(leaderboardContent != null ? "已获取" : "未获取")}");
        Debug.Log($"playerNameInput: {(playerNameInput != null ? "已获取" : "未获取")}");
        Debug.Log($"submitScoreButton: {(submitScoreButton != null ? "已获取" : "未获取")}");
        Debug.Log($"currentBulletsLabel: {(currentBulletsLabel != null ? "已获取" : "未获取")}");
        Debug.Log($"currentErrorsLabel: {(currentErrorsLabel != null ? "已获取" : "未获取")}");
        Debug.Log($"currentScoreLabel: {(currentScoreLabel != null ? "已获取" : "未获取")}");
        Debug.Log($"currentLevelIcon: {(currentLevelIcon != null ? "已获取" : "未获取")}");
        Debug.Log($"currentHealthIcons: {(currentHealthIcons != null ? "已获取" : "未获取")}");
        Debug.Log($"returnButton: {(returnButton != null ? "已获取" : "未获取")}");
        Debug.Log($"retryButton: {(retryButton != null ? "已获取" : "未获取")}");
    }
    
    // 初始化胜利UI
    public void InitializeVictoryUI(int bulletsFired, int wrongInputs, int health)
    {
        Debug.Log($"初始化胜利UI: 子弹数={bulletsFired}, 错误数={wrongInputs}, 生命={health}");
        
        // 保存数据
        totalBulletsFired = bulletsFired;
        wrongInputCount = wrongInputs;
        playerHealth = health;
        currentLevelName = SceneManager.GetActiveScene().name;
        scoreSubmitted = false;
        
        // 确保UI元素已获取
        if (root == null)
        {
            OnEnable();
        }
        
        // 显示胜利面板
        VisualElement victoryPanel = root.Q("VictoryPanel");
        if (victoryPanel != null)
        {
            victoryPanel.style.display = DisplayStyle.Flex;
            Debug.Log("显示胜利面板");
        }
        
        // 立即更新UI
        UpdateCurrentScoreUI();
        UpdateLeaderboardUI();
    }
    
    // 更新当前成绩UI
    private void UpdateCurrentScoreUI()
    {
        if (root == null) 
        {
            Debug.LogError("根元素为空");
            return;
        }
        
        Debug.Log($"更新当前成绩UI: 子弹数={totalBulletsFired}, 错误数={wrongInputCount}, 生命={playerHealth}");
        
        // 设置当前成绩
        if (currentBulletsLabel != null)
        {
            currentBulletsLabel.text = totalBulletsFired.ToString();
            Debug.Log($"设置子弹数文本: {totalBulletsFired}");
        }
        else
        {
            Debug.LogError("子弹数标签为空");
        }
        
        if (currentErrorsLabel != null)
        {
            currentErrorsLabel.text = wrongInputCount.ToString();
            Debug.Log($"设置错误数文本: {wrongInputCount}");
        }
        else
        {
            Debug.LogError("错误数标签为空");
        }
        
        // 计算并显示分数
        int currentScore = CalculateScore(totalBulletsFired, wrongInputCount, playerHealth);
        if (currentScoreLabel != null)
        {
            currentScoreLabel.text = currentScore.ToString();
            Debug.Log($"设置分数文本: {currentScore}");
        }
        else
        {
            Debug.LogError("分数标签为空");
        }
        
        // 显示关卡图标
        if (currentLevelIcon != null)
        {
            int levelIndex = GetLevelIndex(currentLevelName);
            if (levelIndex >= 0 && levelIndex < levelIcons.Length)
            {
                currentLevelIcon.style.backgroundImage = new StyleBackground(levelIcons[levelIndex]);
                Debug.Log($"设置关卡图标: {currentLevelName}, 索引={levelIndex}");
            }
        }
        else
        {
            Debug.LogError("关卡图标元素为空");
        }
        
        // 显示生命图标
        if (currentHealthIcons != null)
        {
            currentHealthIcons.Clear();
            Debug.Log($"设置生命图标: {playerHealth}/3");
            
            for (int i = 0; i < 3; i++)
            {
                var healthIcon = new VisualElement();
                healthIcon.style.width = 20;
                healthIcon.style.height = 20;
                healthIcon.style.marginLeft = 2;
                healthIcon.style.marginRight = 2;
                
                if (i < playerHealth)
                {
                    healthIcon.style.backgroundImage = new StyleBackground(healthSprite);
                }
                else
                {
                    healthIcon.style.backgroundImage = new StyleBackground(lostHealthSprite);
                }
                
                currentHealthIcons.Add(healthIcon);
            }
        }
        else
        {
            Debug.LogError("生命图标容器为空");
        }
    }
    
    // 更新排行榜UI
    private void UpdateLeaderboardUI()
    {
        if (leaderboardContent == null) 
        {
            Debug.LogError("排行榜内容元素为空");
            return;
        }
        
        // 清空排行榜内容
        leaderboardContent.Clear();
        
        // 添加排行榜条目
        for (int i = 0; i < leaderboardEntries.Count; i++)
        {
            var entry = leaderboardEntries[i];
            var entryElement = CreateLeaderboardEntryElement(i + 1, entry);
            
            // 为前三名添加特殊样式
            if (i == 0) entryElement.AddToClassList("rank-1");
            else if (i == 1) entryElement.AddToClassList("rank-2");
            else if (i == 2) entryElement.AddToClassList("rank-3");
            
            leaderboardContent.Add(entryElement);
        }
        
        Debug.Log($"更新排行榜UI: 显示{leaderboardEntries.Count}条记录");
    }
    
    // 创建排行榜条目元素
    private VisualElement CreateLeaderboardEntryElement(int rank, LeaderboardEntry entry)
    {
        var entryElement = new VisualElement();
        entryElement.AddToClassList("leaderboard-row");
        entryElement.AddToClassList("leaderboard-item");
        
        // 排名
        var rankLabel = new Label(rank.ToString());
        rankLabel.AddToClassList("score-cell");
        rankLabel.style.fontSize = 23; // 设置字体大小
        entryElement.Add(rankLabel);
        
        // 玩家名称
        var nameLabel = new Label(entry.playerName);
        nameLabel.AddToClassList("score-cell");
        nameLabel.style.fontSize = 23; // 设置字体大小
        entryElement.Add(nameLabel);
        
        // 关卡图标
        var levelIcon = new VisualElement();
        levelIcon.AddToClassList("score-cell");
        levelIcon.AddToClassList("level-icon");
        int levelIndex = GetLevelIndex(entry.levelName);
        if (levelIndex >= 0 && levelIndex < levelIcons.Length)
        {
            levelIcon.style.backgroundImage = new StyleBackground(levelIcons[levelIndex]);
        }
        entryElement.Add(levelIcon);
        
        // 子弹数
        var bulletsLabel = new Label(entry.bulletsFired.ToString());
        bulletsLabel.AddToClassList("score-cell");
        bulletsLabel.style.fontSize = 23; // 设置字体大小
        entryElement.Add(bulletsLabel);
        
        // 错误数
        var errorsLabel = new Label(entry.wrongInputs.ToString());
        errorsLabel.AddToClassList("score-cell");
        errorsLabel.style.fontSize = 23; // 设置字体大小
        entryElement.Add(errorsLabel);
        
        // 生命图标
        var healthContainer = new VisualElement();
        healthContainer.AddToClassList("score-cell");
        healthContainer.AddToClassList("health-icons-container");
        for (int i = 0; i < 3; i++)
        {
            var healthIcon = new VisualElement();
            healthIcon.style.width = 25;
            healthIcon.style.height = 25;
            healthIcon.style.marginLeft = 2;
            healthIcon.style.marginRight = 2;
            
            if (i < entry.remainingHealth)
            {
                healthIcon.style.backgroundImage = new StyleBackground(healthSprite);
            }
            else
            {
                healthIcon.style.backgroundImage = new StyleBackground(lostHealthSprite);
            }
            
            healthContainer.Add(healthIcon);
        }
        entryElement.Add(healthContainer);
        
        // 总分
        var scoreLabel = new Label(entry.score.ToString());
        scoreLabel.AddToClassList("score-cell");
        scoreLabel.style.fontSize = 23; // 设置字体大小
        entryElement.Add(scoreLabel);
        
        return entryElement;
    }
    
    // 提交分数
    private void OnSubmitScore()
    {
        if (scoreSubmitted)
        {
            Debug.Log("分数已提交，不允许重复提交");
            return;
        }
        
        string playerName = playerNameInput.value;
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("玩家名称不能为空");
            return;
        }
        
        // 过滤敏感词
        string filteredName = SensitiveWordFilter.Instance.FilterSensitiveWords(playerName);
        if (filteredName != playerName)
        {
            Debug.Log($"检测到敏感词，过滤后名称: {filteredName}");
            playerName = filteredName;
            playerNameInput.value = filteredName; // 更新输入框显示
        }
        
        // 创建新的排行榜条目
        LeaderboardEntry newEntry = new LeaderboardEntry(
            playerName,
            currentLevelName,
            totalBulletsFired,
            wrongInputCount,
            playerHealth
        );
        
        Debug.Log($"提交分数: 玩家={playerName}, 子弹数={totalBulletsFired}, 错误数={wrongInputCount}, 生命={playerHealth}, 分数={newEntry.score}");
        
        // 添加到排行榜并排序
        AddEntryToLeaderboard(newEntry);
        
        // 保存排行榜
        SaveLeaderboard();
        
        // 更新UI
        UpdateLeaderboardUI();
        
        // 禁用提交按钮
        submitScoreButton.SetEnabled(false);
        playerNameInput.SetEnabled(false);
        scoreSubmitted = true;
    }
    
    // 添加条目到排行榜
    private void AddEntryToLeaderboard(LeaderboardEntry newEntry)
    {
        // 检查是否已存在相同名字的玩家
        int existingIndex = leaderboardEntries.FindIndex(e => e.playerName == newEntry.playerName);
        
        if (existingIndex >= 0)
        {
            // 如果存在相同名字的玩家，比较分数
            if (newEntry.score > leaderboardEntries[existingIndex].score)
            {
                // 如果新分数更高，替换旧记录
                leaderboardEntries[existingIndex] = newEntry;
                Debug.Log($"更新玩家 {newEntry.playerName} 的最高分: {newEntry.score}");
            }
            else
            {
                Debug.Log($"玩家 {newEntry.playerName} 的新分数 {newEntry.score} 未超过现有最高分 {leaderboardEntries[existingIndex].score}，不更新排行榜");
            }
        }
        else
        {
            // 如果是新玩家，直接添加
            leaderboardEntries.Add(newEntry);
            Debug.Log($"添加新玩家 {newEntry.playerName} 到排行榜，分数: {newEntry.score}");
        }
        
        // 按分数降序排序
        leaderboardEntries = leaderboardEntries.OrderByDescending(e => e.score).ToList();
        
        // 限制排行榜最多显示50条记录
        if (leaderboardEntries.Count > 50)
        {
            leaderboardEntries.RemoveAt(50);
        }
    }
    
    // 计算分数
    private int CalculateScore(int bullets, int errors, int health)
    {
        int score = (health * 10) + (bullets * 2) - errors;
        if (errors == 0)
        {
            score += 5;
        }
        return score;
    }
    
    // 获取关卡索引
    private int GetLevelIndex(string levelName)
    {
        for (int i = 0; i < levelNames.Length; i++)
        {
            if (levelNames[i] == levelName)
            {
                return i;
            }
        }
        return 0; // 默认返回第一个
    }
    
    // 加载排行榜数据
    private void LoadLeaderboard()
    {
        if (File.Exists(leaderboardFilePath))
        {
            try
            {
                string json = File.ReadAllText(leaderboardFilePath);
                LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(json);
                if (data != null && data.entries != null)
                {
                    leaderboardEntries = data.entries;
                }
                else
                {
                    Debug.LogWarning("排行榜数据为空或格式错误");
                    leaderboardEntries = new List<LeaderboardEntry>();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("加载排行榜失败: " + e.Message);
                leaderboardEntries = new List<LeaderboardEntry>();
            }
        }
        else
        {
            Debug.Log("排行榜文件不存在，创建新排行榜");
            leaderboardEntries = new List<LeaderboardEntry>();
        }
    }
    
    // 保存排行榜数据
    private void SaveLeaderboard()
    {
        try
        {
            LeaderboardData data = new LeaderboardData { entries = leaderboardEntries };
            string json = JsonUtility.ToJson(data);
            File.WriteAllText(leaderboardFilePath, json);
            Debug.Log("排行榜数据保存成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError("保存排行榜失败: " + e.Message);
        }
    }
    
    // 返回世界按钮点击事件
    public void OnReturnToWorldClicked()
    {
        // 使用CombatManager中的方法返回世界
        if (combatManager != null)
        {
            Debug.Log("点击了返回世界按钮，正在调用CombatManager.TransToWorldScene方法");
            combatManager.SendMessage("TransToWorldScene", null, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogError("无法找到CombatManager实例！");
        }
    }
    
    // 重试按钮点击事件
    public void OnRetryClicked()
    {
        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

// 用于JSON序列化的辅助类
[System.Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries;
}
