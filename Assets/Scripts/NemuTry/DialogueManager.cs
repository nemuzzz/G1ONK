using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    
    [Header("对话UI预制体")]
    public GameObject dialoguePanelPrefab;  // 对话框预制体
    public GameObject optionsPanelPrefab;   // 选项框预制体
    public float dialogueBoxHeight = 2f;
    
    private GameObject currentDialoguePanel;  // 当前激活的对话框
    private GameObject currentOptionsPanel;   // 当前激活的选项框
    private Text speakerNameText;
    private Text dialogueText;
    private Button acceptButton;
    private Button declineButton;
    
    private Transform currentNPC;
    private DialogueData currentDialogue;
    private int currentLineIndex;
    private bool isLastLine;
    private bool showingOptions;
    private bool isTyping;
    public bool IsInDialogue { get; private set; }
    public Transform CurrentNPC => currentNPC;

    [Header("战斗场景设置")]
    [HideInInspector] // 隐藏在检查器中，用于NPC配置
    public string battleSceneName = "";
    public bool loadWithTransition = true;
    
    // 对话事件
    public UnityEvent onDialogueStart = new UnityEvent();
    public UnityEvent onDialogueEnd = new UnityEvent();
    public UnityEvent onOptionAccepted = new UnityEvent();
    public UnityEvent onOptionDeclined = new UnityEvent();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartDialogue(DialogueData dialogue, Transform npcTransform)
    {
        if (dialogue == null || npcTransform == null)
        {
            Debug.LogError($"对话数据或NPC为空！NPC: {npcTransform?.name}");
            return;
        }

        // 结束当前对话（如果有）
        if (IsInDialogue)
        {
            EndDialogue();
        }

        // 创建对话UI
        CreateDialogueUI(npcTransform);

        // 设置新对话
        currentDialogue = dialogue;
        currentNPC = npcTransform;
        currentLineIndex = 0;
        isLastLine = (dialogue.dialogueLines.Count == 1);
        showingOptions = false;
        IsInDialogue = true;

        Debug.Log($"开始与NPC {currentNPC.name} 的对话");
        onDialogueStart.Invoke();

        if (dialogue.dialogueLines.Count > 0)
        {
            DisplayCurrentLine();
        }
        else
        {
            Debug.LogWarning("对话数据中没有对话行！");
            EndDialogue();
        }
    }

    private void CreateDialogueUI(Transform npcTransform)
    {
        // 创建对话框
        if (dialoguePanelPrefab != null)
        {
            currentDialoguePanel = Instantiate(dialoguePanelPrefab);
            currentDialoguePanel.transform.SetParent(npcTransform);
            currentDialoguePanel.transform.localPosition = Vector3.up * dialogueBoxHeight;
            currentDialoguePanel.transform.localRotation = Quaternion.identity;

            // 获取UI组件引用
            speakerNameText = currentDialoguePanel.GetComponentInChildren<Text>(true);
            dialogueText = currentDialoguePanel.GetComponentsInChildren<Text>(true)[1];
        }

        // 创建选项框
        if (optionsPanelPrefab != null)
        {
            currentOptionsPanel = Instantiate(optionsPanelPrefab);
            currentOptionsPanel.transform.SetParent(npcTransform);
            currentOptionsPanel.transform.localPosition = Vector3.up * dialogueBoxHeight;
            currentOptionsPanel.transform.localRotation = Quaternion.identity;
            currentOptionsPanel.SetActive(false);

            // 获取按钮引用
            var buttons = currentOptionsPanel.GetComponentsInChildren<Button>(true);
            if (buttons.Length >= 2)
            {
                acceptButton = buttons[0];
                declineButton = buttons[1];
                acceptButton.onClick.AddListener(OnAcceptBattle);
                declineButton.onClick.AddListener(OnDeclineBattle);
            }
        }
    }

    private void EndDialogue()
    {
        if (!IsInDialogue) return;

        Debug.Log("结束对话");
        
        // 清理UI
        if (currentDialoguePanel != null)
            Destroy(currentDialoguePanel);
        if (currentOptionsPanel != null)
            Destroy(currentOptionsPanel);

        // 清理状态
        currentDialogue = null;
        currentNPC = null;
        isLastLine = false;
        showingOptions = false;
        speakerNameText = null;
        dialogueText = null;
        acceptButton = null;
        declineButton = null;
        
        // 最后设置对话状态并触发事件
        IsInDialogue = false;
        onDialogueEnd.Invoke();
    }

    private void ShowOptions()
    {
        showingOptions = true;
        if (currentDialoguePanel != null)
            currentDialoguePanel.SetActive(false);
        if (currentOptionsPanel != null)
            currentOptionsPanel.SetActive(true);
    }

    private void HideOptions()
    {
        showingOptions = false;
        if (currentOptionsPanel != null)
            currentOptionsPanel.SetActive(false);
    }

    private void DisplayCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.dialogueLines.Count)
        {
            Debug.LogWarning("没有更多对话内容，显示选项");
            ShowOptions();
            return;
        }
        
        var line = currentDialogue.dialogueLines[currentLineIndex];
        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;
        if (dialogueText != null)
            dialogueText.text = line.content;
        
        Debug.Log($"显示对话: {line.speakerName}: {line.content}");
    }

    void Update()
    {
        // 检查是否有正在进行的对话
        if (IsInDialogue && currentDialogue != null && currentNPC != null)
        {
            // 检查输入
            if (showingOptions)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    OnAcceptBattle();
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    OnDeclineBattle();
                }
            }
            else if (!isTyping)
            {
                if (isLastLine)
                {
                    if (Input.GetKeyDown(KeyCode.Space) || 
                        Input.GetKeyDown(KeyCode.Return) || 
                        Input.GetMouseButtonDown(0))
                    {
                        ShowOptions();
                    }
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.Space) || 
                        Input.GetKeyDown(KeyCode.Return) || 
                        Input.GetMouseButtonDown(0))
                    {
                        NextLine();
                    }
                }
            }
        }
    }

    private void NextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < currentDialogue.dialogueLines.Count)
        {
            isLastLine = (currentLineIndex == currentDialogue.dialogueLines.Count - 1);
            DisplayCurrentLine();
        }
    }

    public void OnAcceptBattle()
    {
        if (!IsInDialogue) return;
        Debug.Log("接受战斗");
        HideOptions();
        onOptionAccepted.Invoke();
        EndDialogue();
        
        // 检查战斗场景路径是否为空
        if (string.IsNullOrEmpty(battleSceneName))
        {
            Debug.LogError("战斗场景路径为空，请在NPC配置中设置正确的战斗场景路径。");
            return;
        }
        
        // 检查场景是否存在
        bool sceneExists = false;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneName == battleSceneName)
            {
                sceneExists = true;
                break;
            }
        }
        
        if (!sceneExists)
        {
            Debug.LogError($"战斗场景 '{battleSceneName}' 不存在或未添加到构建设置中，请在File->Build Settings中添加该场景。");
            return;
        }
        
        // 销毁当前Player和相机，防止它们出现在战斗场景中
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
        
        // 加载场景
        if (loadWithTransition)
        {
            SceneManager.LoadScene(battleSceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(battleSceneName);
        }
    }

    public void OnDeclineBattle()
    {
        if (!IsInDialogue) return;
        Debug.Log("拒绝战斗");
        HideOptions();
        onOptionDeclined.Invoke();
        EndDialogue();
    }
}