using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class OnlineResultViewManager : MonoBehaviour
{
    // 結果表示用のテーブル
    public Transform playerResultRow;    // プレイヤーの結果を表示する行
    public Transform opponentResultRow;  // 相手の結果を表示する行
    private TextMeshProUGUI[] playerCells;
    private TextMeshProUGUI[] opponentCells;
    private int currentRound = 0;

    private readonly string[] cardRanks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private readonly Color winColor = new Color(1f, 0.2f, 0.2f);    // 勝者の文字色（赤）
    private readonly Color normalColor = Color.black;               // 通常の文字色

    void Start()
    {
        InitializeResultTable();
    }

    private GameObject CreateCell(string name, Transform parent)
    {
        var cell = new GameObject(name);
        cell.transform.SetParent(parent, false);

        // LayerをUI(5)に設定
        cell.layer = 5;

        // 必要なコンポーネントを追加
        var rectTransform = cell.AddComponent<RectTransform>();
        var canvasRenderer = cell.AddComponent<CanvasRenderer>();
        var tmp = cell.AddComponent<TextMeshProUGUI>();

        // RectTransformの設定
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(55, 100);
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one;

        // TextMeshProの設定
        tmp.fontStyle = FontStyles.Bold;
        tmp.fontSize = 30;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;

        return cell;
    }

    private void InitializeResultTable()
    {       
        if (playerResultRow == null || opponentResultRow == null)
        {
            Debug.LogError("Required references are not set in the inspector!");
            return;
        }

        try
        {
            // 子オブジェクトを直接取得
            var playerChildren = new List<GameObject>();
            var opponentChildren = new List<GameObject>();

            for (int i = 1; i <= 15; i++)
            {
                var playerChild = playerResultRow.Find(i.ToString())?.gameObject;
                var opponentChild = opponentResultRow.Find(i.ToString())?.gameObject;

                if (playerChild != null)
                {
                    var tmp = playerChild.GetComponent<TextMeshProUGUI>();
                    if (tmp == null)
                    {
                        // 既存のセルにTextMeshProUGUIがない場合は削除して新規作成
                        GameObject.Destroy(playerChild);
                        playerChild = CreateCell(i.ToString(), playerResultRow);
                    }
                    playerChildren.Add(playerChild);
                }
                else
                {
                    // プレイヤーセルが見つからない場合は新規作成
                    playerChild = CreateCell(i.ToString(), playerResultRow);
                    playerChildren.Add(playerChild);
                }

                if (opponentChild != null)
                {
                    var tmp = opponentChild.GetComponent<TextMeshProUGUI>();
                    if (tmp == null)
                    {
                        // 既存のセルにTextMeshProUGUIがない場合は削除して新規作成
                        GameObject.Destroy(opponentChild);
                        opponentChild = CreateCell(i.ToString(), opponentResultRow);
                    }
                    opponentChildren.Add(opponentChild);
                }
                else
                {
                    // 相手セルが見つからない場合は新規作成
                    opponentChild = CreateCell(i.ToString(), opponentResultRow);
                    opponentChildren.Add(opponentChild);
                }
            }

            // 配列を初期化
            playerCells = new TextMeshProUGUI[playerChildren.Count];
            opponentCells = new TextMeshProUGUI[opponentChildren.Count];

            // セルの参照を設定
            for (int i = 0; i < playerChildren.Count; i++)
            {
                playerCells[i] = playerChildren[i].GetComponent<TextMeshProUGUI>();
            }

            for (int i = 0; i < opponentChildren.Count; i++)
            {
                opponentCells[i] = opponentChildren[i].GetComponent<TextMeshProUGUI>();
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during result table initialization: {e.Message}\n{e.StackTrace}");
        }
    }

    // 勝敗判定を行い結果を表示
    public void ShowResultTable(int playerCardValue, int opponentCardValue)
    {    
        // カード値をランクインデックスに変換（0-12の範囲）
        int playerRankIndex = playerCardValue % 13;
        int opponentRankIndex = opponentCardValue % 13;
        
        Debug.Log($"[RESULT_DEBUG] ShowResultTable called - Player: {playerCardValue} (rank: {playerRankIndex}), Opponent: {opponentCardValue} (rank: {opponentRankIndex})");
        
        // 同値の場合はDRAW
        if (playerRankIndex == opponentRankIndex)
        {
            UpdateResultTable(playerRankIndex, opponentRankIndex, true);
            return;
        }

        bool playerWins = IsWinner(playerRankIndex, opponentRankIndex);
        Debug.Log($"[RESULT_DEBUG] playerWins: {playerWins}");
        if (playerWins)
        {
            UpdateResultTable(playerRankIndex, opponentRankIndex, false, true);
        }
        else
        {
            UpdateResultTable(playerRankIndex, opponentRankIndex, false, false);
        }
    }

    // 結果表を更新
    public void UpdateResultTable(int playerCardValue, int opponentCardValue, bool isDraw, bool playerWins = false)
    {
        if (playerCells == null || opponentCells == null)
        {
            Debug.LogError("Result cells not initialized!");
            InitializeResultTable();
            return;
        }

        if (currentRound >= playerCells.Length || currentRound >= opponentCells.Length)
        {
            Debug.LogWarning($"[RESULT_DEBUG] Current round {currentRound} exceeds table length (player: {playerCells.Length}, opponent: {opponentCells.Length})");
            return;
        }

        Debug.Log($"[RESULT_DEBUG] Updating result table - Round: {currentRound}, Player: {playerCardValue}, Opponent: {opponentCardValue}, isDraw: {isDraw}, playerWins: {playerWins}");
        
        // プレイヤーのカードを表示
        if (playerCells[currentRound] != null)
        {
            Debug.Log($"[RESULT_DEBUG] Player card value: {playerCardValue}, card rank: {cardRanks[playerCardValue]}");   
            playerCells[currentRound].text = cardRanks[playerCardValue];
            playerCells[currentRound].color = isDraw ? normalColor : (playerWins ? winColor : normalColor);
        }
        else
        {
            Debug.LogError($"Player cell {currentRound} is null!");
        }

        // 相手のカードを表示
        if (opponentCells[currentRound] != null)
        {
            Debug.Log($"[RESULT_DEBUG] Opponent card value: {opponentCardValue}, card rank: {cardRanks[opponentCardValue]}");
            opponentCells[currentRound].text = cardRanks[opponentCardValue];
            opponentCells[currentRound].color = isDraw ? normalColor : (playerWins ? normalColor : winColor);
        }
        else
        {
            Debug.LogError($"Opponent cell {currentRound} is null!");
        }

        currentRound++;
        Debug.Log($"[RESULT_DEBUG] Result table updated, next round: {currentRound}");
    }

    // 特殊な勝敗判定ロジック（ランクインデックス: 0=2, 1=3, ..., 12=A）
    public bool IsWinner(int playerRankIndex, int opponentRankIndex)
    {
        // 同値の場合は引き分け（この関数は同値チェック後に呼ばれる）
        if (playerRankIndex == opponentRankIndex)
        {
            return false;
        }

        // 2(index:0)とA(index:12)の特殊ケース
        if (playerRankIndex == 0 && opponentRankIndex == 12) // プレイヤーの2がAに勝つ
        {
            return true;
        }
        if (playerRankIndex == 12 && opponentRankIndex == 0) // 相手の2が自分のAに勝つ
        {
            return false;
        }

        // 通常の比較（大きい方が勝ち）
        return playerRankIndex > opponentRankIndex;
    }

    // 結果表示をリセット
    public void ResetResults()
    {
        currentRound = 0;
        if (playerCells != null && opponentCells != null)
        {
            for (int i = 0; i < playerCells.Length; i++)
            {
                if (playerCells[i] != null)
                {
                    playerCells[i].text = "-";
                    playerCells[i].color = normalColor;
                }
            }
            for (int i = 0; i < opponentCells.Length; i++)
            {
                if (opponentCells[i] != null)
                {
                    opponentCells[i].text = "-";
                    opponentCells[i].color = normalColor;
                }
            }
        }
    }
}
