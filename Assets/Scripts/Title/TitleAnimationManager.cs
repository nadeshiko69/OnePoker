using UnityEngine;
using System.Collections;
using System;

// 移動させるボタンの情報をまとめるクラス
[System.Serializable]
public class ButtonAnimationInfo
{
    public RectTransform targetButton;
    public Vector2 targetPosition;
    public float moveDuration = 5f;
}

public class TitleAnimationManager : MonoBehaviour
{
    [Header("アニメーションさせるボタンの情報")]
    [SerializeField] private ButtonAnimationInfo[] buttonAnimationInfos;

    /// <summary>
    /// 指定されたボタンを目標位置に移動させる
    /// </summary>
    public void MoveButton(RectTransform buttonToMove, Action onComplete = null)
    {
        foreach (var info in buttonAnimationInfos)
        {
            if (info.targetButton == buttonToMove)
            {
                StartCoroutine(MoveButtonCoroutine(info.targetButton, info.targetPosition, info.moveDuration, onComplete));
                return;
            }
        }
        Debug.LogWarning("指定されたボタンの情報が見つかりませんでした: " + buttonToMove.name);
    }

    private IEnumerator MoveButtonCoroutine(RectTransform rectTransform, Vector2 endPosition, float duration, Action onComplete)
    {
        float time = 0;
        Vector2 startPosition = rectTransform.anchoredPosition;

        while (time < duration)
        {
            // 時間の経過に合わせて位置を線形補間
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        // 最終的な位置を正確に設定
        rectTransform.anchoredPosition = endPosition;
        onComplete?.Invoke();
    }
} 