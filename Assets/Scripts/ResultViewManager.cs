using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultViewManager : MonoBehaviour
{
    public GameObject cellPrefab; // セルのプレハブ
    public RectTransform resultView; // 表全体のコンテンツ
    public RectTransform columnTitle; // 固定列 (行タイトル)

    private int rows = 3; // 行数
    private int cols = 16; // 列数

    void Start()
    {
        if (resultView == null)
        {
            Debug.LogError("resultView が設定されていません！", this);
            return;
        }
        if (cellPrefab == null)
        {
            Debug.LogError("cellPrefab が設定されていません！", this);
            return;
        }
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
            row.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            row.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            row.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            // 各セルを追加
            for (int j = 0; j < cols - 1; j++)
            {
                GameObject cell = Instantiate(cellPrefab, row.transform, false);
                cell.GetComponent<Text>().text = $"({i},{j + 1})";
            }
        }
    }
}
