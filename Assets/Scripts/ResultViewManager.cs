using UnityEngine;
using TMPro;

public class ResultViewManager : MonoBehaviour
{
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    private readonly string[] cardRanks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

    void Start()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
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
                Debug.Log($"Draw! Both players have {cardRanks[playerCardValue]}");
                return;
            }

            bool playerWins = IsWinner(playerCardValue, opponentCardValue);
            
            if (playerWins)
            {
                resultText.text = "YOU WIN!";
                resultText.color = Color.red;
                Debug.Log($"Player wins with {cardRanks[playerCardValue]} vs {cardRanks[opponentCardValue]}");
            }
            else
            {
                resultText.text = "YOU LOSE...";
                resultText.color = Color.blue;
                Debug.Log($"Opponent wins with {cardRanks[opponentCardValue]} vs {cardRanks[playerCardValue]}");
            }
        }
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

    // 結果表示を非表示にする
    public void HideResult()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
}
