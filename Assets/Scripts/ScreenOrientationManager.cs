// ScreenOrientationManager.cs
// 画面の向きを管理するクラス
// 画面を縦向きに固定し、自動回転を無効化する

using UnityEngine;

public class ScreenOrientationManager : MonoBehaviour
{
    private void Awake()
    {
        // 画面を縦向きに固定
        Screen.orientation = ScreenOrientation.Portrait;
        
        // 画面の自動回転を無効化
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
    }
} 