// AdjustCameraSize.cs
// カメラのサイズを調整する

using UnityEngine;

public class AdjustCameraSize : MonoBehaviour // MonoBehaviour を継承
{
    void Start()
    {
        float targetAspect = 1080f / 1920f;
        float screenAspect = (float)Screen.width / Screen.height;
        Camera.main.orthographicSize *= targetAspect / screenAspect;
    }
}
