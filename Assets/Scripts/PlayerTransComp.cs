using System;
using UnityEngine;

public class PlayerTransComp : MonoBehaviour
{
    public bool bReadyToTrans;
    
    public TransArea CurrentTransAreaComp;

    public void SetReadyToTrans(bool bReady, TransArea InTransArea)
    {
        // 如果是新进入传送区域，播放提示音效
        if (bReady && !bReadyToTrans && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayInteractionPromptSound();
        }
        
        bReadyToTrans = bReady;
        CurrentTransAreaComp = bReadyToTrans ? InTransArea : null;
    }

    private void Update()
    {
        if (!bReadyToTrans)
            return;

        if (Input.GetKeyDown(KeyCode.F))
            TryTransPlayerToTarget();
    }

    void TryTransPlayerToTarget()
    {
        bool canTrans = bReadyToTrans;
        if (canTrans) 
        {
            // 播放传送音效
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTeleportSound();
            }
            
            CurrentTransAreaComp?.TryTransToTarget(transform);
        }
    }
}