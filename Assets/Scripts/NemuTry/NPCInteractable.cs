// NPCInteractable.cs
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BoxCollider2D))]
public class NPCInteractable : MonoBehaviour
{
    [Header("视觉提示")]
    public GameObject fPromptPrefab;
    public float promptHeight = 1.5f;
    
    [Header("对话配置")]
    public DialogueData dialogueData;
    
    [Header("战斗场景配置")]
#if UNITY_EDITOR
    public SceneAsset battleScene; // 可在编辑器中拖拽场景资源
#endif
    [SerializeField] private string battleSceneName; // 场景名称，在运行时使用
    
    private GameObject currentPrompt;
    private bool playerInRange = false;
    
#if UNITY_EDITOR
    // 当在编辑器中修改battleScene时，自动更新battleSceneName
    private void OnValidate()
    {
        if (battleScene != null)
        {
            battleSceneName = battleScene.name;
        }
    }
#endif
    
    void Start()
    {
        // 订阅对话结束事件
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onDialogueEnd.AddListener(OnDialogueEnd);
        }
        
        // 订阅场景卸载事件
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onDialogueEnd.RemoveListener(OnDialogueEnd);
        }
        
        // 取消订阅场景事件
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        
        // 确保提示被清理
        HidePrompt();
    }
    
    void OnDisable()
    {
        // 在禁用时确保提示被清理
        HidePrompt();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // 场景卸载时清理提示
        HidePrompt();
    }
    
    void Update()
    {
        if (DialogueManager.Instance == null) return;
        
        // 只在玩家在范围内且不在对话中时检测按键
        if (playerInRange && (!DialogueManager.Instance.IsInDialogue || DialogueManager.Instance.CurrentNPC == transform))
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (dialogueData != null)
                {
                    Debug.Log($"触发NPC {gameObject.name} 的对话");
                    // 设置战斗场景名称
                    if (!string.IsNullOrEmpty(battleSceneName))
                    {
                        DialogueManager.Instance.battleSceneName = battleSceneName;
                    }
                    DialogueManager.Instance.StartDialogue(dialogueData, transform);
                    HidePrompt();
                }
                else
                {
                    Debug.LogError($"NPC {gameObject.name} 没有设置DialogueData！");
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"玩家进入NPC {gameObject.name} 的范围");
            playerInRange = true;
            // 只在不在对话中时显示提示
            if (DialogueManager.Instance != null && 
                (!DialogueManager.Instance.IsInDialogue || 
                 DialogueManager.Instance.CurrentNPC == transform))
            {
                ShowPrompt();
                
                // 播放交互提示音效
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayInteractionPromptSound();
                }
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            HidePrompt();
        }
    }

    private void OnDialogueEnd()
    {
        // 对话结束时，如果玩家仍在范围内，显示F提示
        if (playerInRange)
        {
            ShowPrompt();
        }
    }
    
    void ShowPrompt()
    {
        if (fPromptPrefab == null)
        {
            Debug.LogError($"NPC {gameObject.name} 没有设置F提示预制体！");
            return;
        }

        if (currentPrompt != null)
        {
            return;
        }

        // 创建提示并设置为子物体
        currentPrompt = Instantiate(fPromptPrefab);
        currentPrompt.transform.SetParent(transform);
        currentPrompt.transform.localPosition = Vector3.up * promptHeight;
        currentPrompt.transform.localRotation = Quaternion.identity;
        
        // 如果提示UI需要面向相机
        if (Camera.main != null)
        {
            currentPrompt.transform.forward = Camera.main.transform.forward;
        }
        else
        {
            Debug.LogWarning("没有找到主相机，F提示可能不会正确面向玩家");
        }
    }
    
    void HidePrompt()
    {
        if (currentPrompt != null)
        {
            Destroy(currentPrompt);
            currentPrompt = null;
        }
    }
}