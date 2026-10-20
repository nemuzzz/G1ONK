#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransArea : MonoBehaviour
{
    [Header("同场景传送目标点")]
    public Transform TransTarget;
    
#if UNITY_EDITOR
    [Header("传送目标场景")]
    public SceneAsset TransTargetScene;
#endif
    
    [HideInInspector, SerializeField]
    private string TransTargetSceneName;
    
    [Header("跨场景传送目标点位置")]
    public Vector2 TransTargetLocation;
    
    [Header("圆形关卡传送配置")]
    public bool isCircularLevelTrans = false;
    [Tooltip("目标圆形关卡层级，0为最外层")]
    public int targetCircularLayer = 0;
    [Tooltip("目标圆形关卡旋转角度（0-360度）")]
    public float targetRotationAngle = 0f;
    
    public BoxCollider2D DetectArea;

    public GameObject InteractIconPrefab;

    private GameObject InteractIconInstance;
    private static Vector2 pendingTransLocation;
    private static bool hasPendingTrans = false;
    
#if UNITY_EDITOR
    public void OnAfterDeserialize ( ) => FillTransTargetSceneName ( );
    public void OnBeforeSerialize ( ) => FillTransTargetSceneName ( );
    public void OnValidate ( ) => FillTransTargetSceneName ( );

    private void FillTransTargetSceneName ( )
    {
        if (TransTargetScene)
            TransTargetSceneName = TransTargetScene.name;
    }
#endif

    // 添加静态方法以设置传送位置
    public static void SetPendingTransLocation(Vector2 location)
    {
        pendingTransLocation = location;
        hasPendingTrans = true;
    }

    private void Awake()
    {
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 取消订阅场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetEnableInteractIcon(bool bEnable)
    {
        if (!InteractIconPrefab)
        {
            return;
        }

        if (bEnable)
        {
            InteractIconInstance = Instantiate(InteractIconPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            Destroy(InteractIconInstance);
        }
    }

    public bool TryTransToTarget(Transform PlayerTrans)
    {
        if (TransTargetSceneName.Length == 0)
        {
            return false;
        }

        if (TransTargetSceneName == SceneManager.GetActiveScene().name && TransTarget)
        {
            // 同场景传送
            PlayerTrans.position = TransTarget.position;
            return true;
        }
        else
        {
            // 跨场景传送
            // 保存目标位置供新场景使用
            pendingTransLocation = TransTargetLocation;
            hasPendingTrans = true;

            // 如果是圆形关卡传送，设置层级和旋转角度
            if (isCircularLevelTrans && TransTargetSceneName.Contains("Area1"))
            {
                Debug.Log($"设置圆形关卡传送参数: 层级={targetCircularLayer}, 旋转角度={targetRotationAngle}");
                CircularLevelData.SetPendingCircularTransData(targetCircularLayer, targetRotationAngle);
            }

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

            // 加载新场景
            SceneManager.LoadScene(TransTargetSceneName, LoadSceneMode.Single);
            return true;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (hasPendingTrans)
        {
            StartCoroutine(SetupNewScene());
        }
    }

    private IEnumerator SetupNewScene()
    {
        // 等待两帧，确保所有对象都已实例化
        yield return null;
        yield return null;

        // 获取新场景中的Player
        GameObject newPlayer = GameObject.FindGameObjectWithTag("Player");
        if (newPlayer != null)
        {
            Debug.Log($"找到新场景的Player: {newPlayer.name}");
            
            // 设置Player位置
            newPlayer.transform.position = pendingTransLocation;
            
            // 检查当前场景是否是Menu场景
            string currentSceneName = SceneManager.GetActiveScene().name;
            bool isMenuScene = currentSceneName.Contains("Menu");
            
            if (isMenuScene)
            {
                // 如果是Menu场景，使用场景中的摄像机配置
                Debug.Log("检测到Menu场景，使用场景中的摄像机配置");
            }
            else
            {
                // 获取新场景中的相机
                Camera newCamera = Camera.main;
                if (newCamera != null)
                {
                    // 确保相机立即激活
                    newCamera.gameObject.SetActive(true);
                    
                    // 确保相机旋转角度正确（修复相机歪斜问题）
                    newCamera.transform.rotation = Quaternion.identity;
                    
                    // 尝试获取任何可能的相机控制脚本
                    MonoBehaviour[] cameraScripts = newCamera.GetComponents<MonoBehaviour>();
                    foreach (var script in cameraScripts)
                    {
                        try
                        {
                            // 通过反射查找target或Target属性
                            var targetProperty = script.GetType().GetProperty("target") ?? 
                                              script.GetType().GetProperty("Target");
                            
                            if (targetProperty != null && 
                                (targetProperty.PropertyType == typeof(Transform) || 
                                 targetProperty.PropertyType == typeof(GameObject)))
                            {
                                // 找到了可能的相机跟随脚本，设置目标
                                targetProperty.SetValue(script, newPlayer.transform);
                                Debug.Log($"已设置相机脚本 {script.GetType().Name} 的跟随目标");
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"设置相机脚本属性时异常: {e.Message}");
                        }
                    }
                    
                    // 尝试将相机位置设置到玩家位置
                    newCamera.transform.position = new Vector3(newPlayer.transform.position.x, newPlayer.transform.position.y, -10);
                }
                else
                {
                    Debug.LogWarning("新场景中没有找到主相机！尝试创建一个临时相机");
                    // 如果没有找到主相机，创建一个临时相机
                    GameObject tempCameraObj = new GameObject("TempCamera");
                    Camera tempCamera = tempCameraObj.AddComponent<Camera>();
                    tempCamera.tag = "MainCamera";
                    tempCameraObj.transform.position = newPlayer.transform.position + new Vector3(0, 0, -10);
                    tempCameraObj.transform.rotation = Quaternion.identity; // 确保临时相机旋转正确
                }
            }
        }
        else
        {
            Debug.LogWarning("新场景中没有找到Player标签的物体！");
        }

        // 重置标志
        hasPendingTrans = false;
    }
    
    void Start()
    {
        if (DetectArea == null)
        {
            DetectArea = GetComponent<BoxCollider2D>();
            if (DetectArea == null)
            {
                Debug.Log($"{gameObject.name} cannot find child box component.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // show interact ui
            other.gameObject.GetComponent<PlayerTransComp>()?.SetReadyToTrans(true, this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // disable interact ui
            other.gameObject.GetComponent<PlayerTransComp>()?.SetReadyToTrans(false, this);
        }
    }
}
