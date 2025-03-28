using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultViewManager : MonoBehaviour
{
    public GameObject cellPrefab; // セルのプレハブ
    public RectTransform resultView; // GridLayoutGroup を持つコンテナ
    
    private int rows = 3;  // 行数
    private int cols = 16; // 列数

    void Start()
    {
        GenerateTable();
    }

    void GenerateTable()
    {
        // 既存のセルを削除
        foreach (Transform child in resultView)
        {
            Destroy(child.gameObject);
        }

        // セルを動的に生成
        for (int i = 0; i < cols; i++) // 列を先に回す
        {
            for (int j = 0; j < rows; j++) // 行を回す
            {
                GameObject cell = Instantiate(cellPrefab, resultView, false);

                // TextMeshProUGUI コンポーネントを取得
                TextMeshProUGUI textComponent = cell.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = $"({j},{i})";
                }
            }
        }
    }
}
