using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultViewManager : MonoBehaviour
{
    public GameObject cellPrefab; // セルのプレハブ
    public RectTransform resultView; // 表全体のコンテンツ
    public RectTransform columnTitle; // 固定列 (行タイトル)

    private int rows = 3; // 行数
    private int cols = 16; // 列数

    void Start()
    {
        GenerateTable();
    }

    void GenerateTable()
    {
        // 既存の子オブジェクトを削除
        foreach (Transform child in resultView)
        {
            Destroy(child.gameObject);
        }
        
        for (int i = 0; i < rows; i++)
        {
            // 行の作成
            GameObject row = new GameObject($"Row{i}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(resultView, false);
            
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 1);
            rowRect.anchorMax = new Vector2(0, 1);
            rowRect.pivot = new Vector2(0, 1);
            rowRect.sizeDelta = new Vector2(resultView.rect.width, 30); // 幅を resultView に合わせる

            // HorizontalLayoutGroup 設定
            HorizontalLayoutGroup layoutGroup = row.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;
            layoutGroup.spacing = 5;

            // 各セルを追加
            for (int j = 0; j < cols - 1; j++)
            {
                GameObject cell = Instantiate(cellPrefab, row.transform, false);
                
                // LayoutElementを追加して最小幅を設定
                LayoutElement layoutElement = cell.AddComponent<LayoutElement>();
                layoutElement.minWidth = 50; // 最小幅を設定
                
                // TextMeshProUGUIコンポーネントを取得してテキスト設定
                TextMeshProUGUI textComponent = cell.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"({i},{j + 1})";
                }
                else
                {
                    Debug.LogError("cellPrefab に TextMeshProUGUI コンポーネントが見つかりません！");
                }
            }
        }
    }
}
