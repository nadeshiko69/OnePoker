using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class ResultViewManager : MonoBehaviour
{
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

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
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        InitializeResultTable();
    }

    private void InitializeResultTable()
    {
        if (playerResultRow == null || opponentResultRow == null)
        {
            Debug.LogError("Result rows not assigned!");
            return;
        }

        // 数字のセルを取得（NameCellを除外）
        playerCells = playerResultRow.GetComponentsInChildren<TextMeshProUGUI>()
            .Where(cell => cell.gameObject.name != "NameCell")
            .ToArray();
        opponentCells = opponentResultRow.GetComponentsInChildren<TextMeshProUGUI>()
            .Where(cell => cell.gameObject.name != "NameCell")
            .ToArray();

        Debug.Log($"Found {playerCells.Length} player cells and {opponentCells.Length} opponent cells");

        // セルの初期化
        ResetResults();
    }

    // 勝敗判定を行い結果を表示
    public void ShowResult(int playerCardValue, int opponentCardValue)
    {
        if (resultPanel != null && resultText != null)
        {
            resultPanel.SetActive(true);
            
            // 同値の場合はDRAW
            if (playerCardValue == opponentCardValue)
            {
                resultText.text = "DRAW";
                resultText.color = Color.white;
                UpdateResultTable(playerCardValue, opponentCardValue, true);
                Debug.Log($"Draw! Both players have {cardRanks[playerCardValue]}");
                return;
            }

            bool playerWins = IsWinner(playerCardValue, opponentCardValue);
            
            if (playerWins)
            {
                resultText.text = "YOU WIN!";
                resultText.color = Color.red;
                UpdateResultTable(playerCardValue, opponentCardValue, false, true);
                Debug.Log($"Player wins with {cardRanks[playerCardValue]} vs {cardRanks[opponentCardValue]}");
            }
            else
            {
                resultText.text = "YOU LOSE...";
                resultText.color = Color.blue;
                UpdateResultTable(playerCardValue, opponentCardValue, false, false);
                Debug.Log($"Opponent wins with {cardRanks[opponentCardValue]} vs {cardRanks[playerCardValue]}");
            }
        }
    }

    // 結果表を更新
    private void UpdateResultTable(int playerCardValue, int opponentCardValue, bool isDraw, bool playerWins = false)
    {
        if (playerCells == null || opponentCells == null)
        {
            Debug.LogError("Result cells not initialized!");
            InitializeResultTable();
            return;
        }

        if (currentRound >= playerCells.Length || currentRound >= opponentCells.Length)
        {
            Debug.LogWarning($"No more cells available for results! Current round: {currentRound}");
            return;
        }

        Debug.Log($"Updating round {currentRound + 1} with Player: {cardRanks[playerCardValue]}, Opponent: {cardRanks[opponentCardValue]}");

        // プレイヤーのカードを表示
        if (playerCells[currentRound] != null)
        {
            playerCells[currentRound].text = cardRanks[playerCardValue];
            playerCells[currentRound].color = isDraw ? normalColor : (playerWins ? winColor : normalColor);
            Debug.Log($"Updated player cell {currentRound} with {cardRanks[playerCardValue]}");
        }
        else
        {
            Debug.LogError($"Player cell {currentRound} is null!");
        }

        // 相手のカードを表示
        if (opponentCells[currentRound] != null)
        {
            opponentCells[currentRound].text = cardRanks[opponentCardValue];
            opponentCells[currentRound].color = isDraw ? normalColor : (playerWins ? normalColor : winColor);
            Debug.Log($"Updated opponent cell {currentRound} with {cardRanks[opponentCardValue]}");
        }
        else
        {
            Debug.LogError($"Opponent cell {currentRound} is null!");
        }

        currentRound++;
    }

    // 特殊な勝敗判定ロジック
    private bool IsWinner(int playerValue, int opponentValue)
    {
        // 同値の場合は引き分け（この関数は同値チェック後に呼ばれる）
        if (playerValue == opponentValue)
        {
            return false;
        }

        // 2(index:0)とA(index:12)の特殊ケース
        if (playerValue == 0 && opponentValue == 12) // プレイヤーの2がAに勝つ
        {
            return true;
        }
        if (playerValue == 12 && opponentValue == 0) // 相手の2が自分のAに勝つ
        {
            return false;
        }

        // 通常の比較（大きい方が勝ち）
        return playerValue > opponentValue;
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

    // 結果表示を非表示にする
    public void HideResult()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
}
