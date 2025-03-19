using UnityEngine;
using UnityEngine.UI;

public class ResultViewManager : MonoBehaviour
{
    public GameObject cellPrefab;  // セルのプレハブ
    public Transform fixedColumn;  // 左端の固定列
    public Transform contentPanel; // スクロールする表のパネル

    private int rows = 3;
    private int cols = 16;

    private string[] rowTitles = { "項目1", "項目2", "項目3" };

    void Start()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("cellPrefab is null");
            return;
        }
        if (fixedColumn == null)
        {
            Debug.LogError("fixedColumn is null");
            return;
        }
        if (contentPanel == null)
        {
            Debug.LogError("contentPanel is null");
            return;
        }

        GenerateTable();
    }

    void GenerateTable()
    {
        // 固定列（左側のタイトル部分）
        for (int i = 0; i < rows; i++)
        {
            GameObject cell = Instantiate(cellPrefab, fixedColumn);
            if (cell.GetComponent<Text>() != null)
            {
                cell.GetComponent<Text>().text = rowTitles[i];
            }
        }

        // データ部分（スクロールする表）
        for (int i = 0; i < rows; i++)
        {
            GameObject row = new GameObject("Row" + i, typeof(RectTransform));
            row.transform.SetParent(contentPanel);
            row.AddComponent<HorizontalLayoutGroup>(); // 横にセルを並べる

            for (int j = 0; j < cols - 1; j++) // 1列分少ない
            {
                GameObject cell = Instantiate(cellPrefab, row.transform);
                if (cell.GetComponent<Text>() != null)
                {
                    cell.GetComponent<Text>().text = $"({i},{j + 1})";
                }
            }
        }
    }
}
