using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[ImageEffectAllowedInSceneView] // 添加这个特性以允许在Scene视图中使用
public class GrayScaleEffector : MonoBehaviour
{
    public Material grayscaleMaterial;
    [Range(0, 1)] public float intensity = 1.0f;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (grayscaleMaterial != null)
        {
            grayscaleMaterial.SetFloat("_Intensity", intensity);
            Graphics.Blit(source, destination, grayscaleMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    // 添加这个方法来在Editor模式下更新效果
    #if UNITY_EDITOR
    void OnValidate()
    {
        // 强制立即重绘
        UnityEditor.EditorApplication.delayCall += () => 
        {
            if (this == null) return;
            var cam = GetComponent<Camera>();
            if (cam != null) cam.Render();
        };
    }
    #endif
}
