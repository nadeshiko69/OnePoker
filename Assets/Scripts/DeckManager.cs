using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class DeckManager : MonoBehaviour
{
    private List<int> shuffledCard;
    public IReadOnlyList<int> ShuffledCard => shuffledCard;
    
    // UP/DOWNの表示テキスト
    public TextMeshProUGUI playerUI1;
    public TextMeshProUGUI playerUI2;
    public TextMeshProUGUI opponentUI1;
    public TextMeshProUGUI opponentUI2;

    // 勝敗表示用のUI
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    public CardDisplay playerCard1;
    public CardDisplay playerCard2;
    public CardDisplay opponentCard1;
    public CardDisplay opponentCard2;

    // プレイヤーのカードの数字とマークを表示
    public TextMeshProUGUI playerCardNumber1;
    public TextMeshProUGUI playerCardNumber2;
    public TextMeshProUGUI playerCardMark1;
    public TextMeshProUGUI playerCardMark2;
    private readonly string[] marks = { "♦", "♥", "♠", "♣" };
    private readonly Color redColor = new Color(1f, 0.2f, 0.2f);        // 文字色　赤
    private readonly Color blackColor = new Color(0.1f, 0.1f, 0.1f);    // 文字色　黒

    // カードの数字を保持
    private int playerCardValue;
    private int opponentCardValue1;  // 相手の1枚目のカード
    private int opponentCardValue2;  // 相手の2枚目のカード

    // カードの値を外部から取得するためのプロパティ
    public int PlayerCardValue => playerCardValue;
    public int OpponentCardValue1 => opponentCardValue1;
    public int OpponentCardValue2 => opponentCardValue2;

    void Start()
    {
        shuffledCard = Enumerable.Range(0, 51).ToList();
        ShuffleCard();
        Debug.Log(string.Join(", ", shuffledCard));

        DrawCard(playerUI1, playerCard1);
        DrawCard(playerUI2, playerCard2);
        DrawCard(opponentUI1, opponentCard1);
        DrawCard(opponentUI2, opponentCard2);

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    // カードをシャッフルする
    public void ShuffleCard()
    {
        for (int i = shuffledCard.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffledCard[i], shuffledCard[j]) = (shuffledCard[j], shuffledCard[i]);
        }
    }

    public void DrawCard(TextMeshProUGUI resultText, CardDisplay cardPrefab)
    {
        var (cardValue, Mark, Number) = PopCard();
        Debug.Log($"DrawCard - Before assignment: cardValue={cardValue}, Number={Number}, Mark={marks[Mark]}");

        if (resultText == playerUI1)
        {
            DisplayUI(resultText, cardValue);
            cardPrefab.SetCard(true);
            DisplayCardUI(playerCardNumber1, playerCardMark1, cardValue, Mark, Number);
            playerCardValue = cardValue;
            Debug.Log($"Player Card 1 set: Value={playerCardValue}, Display={Number}{marks[Mark]}");
        }
        else if (resultText == playerUI2)
        {
            DisplayUI(resultText, cardValue);
            cardPrefab.SetCard(true);
            DisplayCardUI(playerCardNumber2, playerCardMark2, cardValue, Mark, Number);
            playerCardValue = cardValue;  // 2枚目のカードの値を保存
            Debug.Log($"Player Card 2 set: Value={playerCardValue}, Display={Number}{marks[Mark]}");
        }
        else if (resultText == opponentUI1)
        {
            DisplayUI(resultText, cardValue);
            cardPrefab.SetCard(false);
            opponentCardValue1 = cardValue;  // 1枚目の値を保存
            Debug.Log($"Opponent Card 1 set: Value={opponentCardValue1}, Display={Number}{marks[Mark]}");
        }
        else if (resultText == opponentUI2)
        {
            DisplayUI(resultText, cardValue);
            cardPrefab.SetCard(false);
            opponentCardValue2 = cardValue;  // 2枚目の値を保存
            Debug.Log($"Opponent Card 2 set: Value={opponentCardValue2}, Display={Number}{marks[Mark]}");
        }
    }

    // 勝敗判定を行い結果を表示
    public void ShowResult()
    {
        if (resultPanel != null && resultText != null)
        {
            resultPanel.SetActive(true);
            
            if (playerCardValue > opponentCardValue1 && playerCardValue > opponentCardValue2)
            {
                resultText.text = "YOU WIN!";
                resultText.color = Color.red;
            }
            else if (playerCardValue < opponentCardValue1 && playerCardValue < opponentCardValue2)
            {
                resultText.text = "YOU LOSE...";
                resultText.color = Color.blue;
            }
            else
            {
                resultText.text = "DRAW";
                resultText.color = Color.white;
            }

            Debug.Log($"Battle Result - Player: {playerCardValue} vs Opponent: {opponentCardValue1}, {opponentCardValue2}");
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

    // 山札からカードを引いて、そのカードのID、マーク、数字を返す
    public (int idNumber, int Mark, string Number) PopCard()
    {
        var NumberView = new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        if (shuffledCard.Count == 0)
        {
            Debug.LogWarning("No more cards to pop!");
            return (-1, -1, null); // エラー値
        }

        int topCard = shuffledCard[shuffledCard.Count - 1];
        shuffledCard.RemoveAt(shuffledCard.Count - 1);

        int Mark = topCard / 13;  // 0 ダイヤ, 1 ハート, 2 スペード, 3 クラブ
        string Number = NumberView[topCard % 13];

        Debug.Log($"idNumber: {topCard}, Mark: {Mark}, Number: {Number}");
        return (topCard%13, Mark, Number);
    }

    // カードを1枚引いて、DOWN/UPをUIに表示
    public void DisplayUI(TextMeshProUGUI resultText, int idNumber)
    {
        // var (idNumber, _, _) = PopCard(); // Markは使わないので無視 `_`

        // UI に表示
        if (resultText != null)
        {
            resultText.text = (idNumber <= 5) ? "DOWN" : "UP";
        }
        else
        {
            Debug.LogWarning("resultText is null");
        }

        Debug.Log($"DisplayUI: {resultText.text}");
    }

    // カードの数字とマークを表示
    public void DisplayCardUI(TextMeshProUGUI numberText, TextMeshProUGUI markText, int idNumber, int mark, string number)
    {
        // var (idNumber, mark, number) = PopCard();

        if (numberText != null)
        {
            numberText.text = number.ToString();
            numberText.color = (mark == 0 || mark == 1) ? redColor : blackColor;
        }

        if (markText != null)
        {
            markText.text = marks[mark]; // マーク文字をセット
            markText.color = (mark == 0 || mark == 1) ? redColor : blackColor;
        }
    }
}
