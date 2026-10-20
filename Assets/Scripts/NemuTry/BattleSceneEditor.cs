#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class BattleSceneEditor : EditorWindow
{
    private string battleSceneName = "BattleScene";
    private Object battleSceneAsset;
    private bool useTransition = true;

    [MenuItem("Tools/Battle Scene Manager")]
    public static void ShowWindow()
    {
        GetWindow<BattleSceneEditor>("战斗场景管理器");
    }

    void OnGUI()
    {
        GUILayout.Label("战斗场景设置", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // 场景名称设置
        battleSceneName = EditorGUILayout.TextField("场景名称", battleSceneName);
        
        // 场景资源选择
        battleSceneAsset = EditorGUILayout.ObjectField("战斗场景", battleSceneAsset, typeof(SceneAsset), false);
        
        // 过渡效果选项
        useTransition = EditorGUILayout.Toggle("使用过渡效果", useTransition);

        EditorGUILayout.Space();

        // 创建场景按钮
        if (GUILayout.Button("创建新战斗场景"))
        {
            CreateBattleScene();
        }

        // 打开场景按钮
        if (GUILayout.Button("打开战斗场景"))
        {
            OpenBattleScene();
        }
    }

    void CreateBattleScene()
    {
        // 创建新场景
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        
        // 保存场景
        string scenePath = $"Assets/Scenes/{battleSceneName}.unity";
        bool saved = EditorSceneManager.SaveScene(scene, scenePath);
        
        if (saved)
        {
            Debug.Log($"战斗场景已创建: {scenePath}");
            
            // 将场景添加到构建设置
            AddSceneToBuildSettings(scenePath);
        }
        else
        {
            Debug.LogError("创建场景失败！");
        }
    }

    void OpenBattleScene()
    {
        if (battleSceneAsset != null)
        {
            string scenePath = AssetDatabase.GetAssetPath(battleSceneAsset);
            EditorSceneManager.OpenScene(scenePath);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", "请先选择战斗场景！", "确定");
        }
    }

    void AddSceneToBuildSettings(string scenePath)
    {
        // 获取当前构建设置
        var scenes = EditorBuildSettings.scenes;
        
        // 检查场景是否已经在构建设置中
        bool sceneExists = false;
        foreach (var scene in scenes)
        {
            if (scene.path == scenePath)
            {
                sceneExists = true;
                break;
            }
        }
        
        // 如果场景不在构建设置中，添加它
        if (!sceneExists)
        {
            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            System.Array.Copy(scenes, newScenes, scenes.Length);
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
#endif
