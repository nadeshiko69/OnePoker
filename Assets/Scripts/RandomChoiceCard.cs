using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class RandomChoiceCard : MonoBehaviour
{
    private List<int> shuffledCard;
    public IReadOnlyList<int> ShuffledCard => shuffledCard;
    
    // UP/DOWNの表示テキスト
    public TextMeshProUGUI playerUI1;
    public TextMeshProUGUI playerUI2;
    public TextMeshProUGUI opponentUI3;
    public TextMeshProUGUI opponentUI4;

    // プレイヤーのカードの数字とマークを表示
    public TextMeshProUGUI playerCardNumber1;
    public TextMeshProUGUI playerCardNumber2;
    public TextMeshProUGUI playerCardMark1;
    public TextMeshProUGUI playerCardMark2;
    private readonly string[] marks = { "♦", "♥", "♠", "♣" };
    private readonly Color redColor = new Color(1f, 0.2f, 0.2f);        // 文字色　赤
    private readonly Color blackColor = new Color(0.1f, 0.1f, 0.1f);    // 文字色　黒

    void Start()
    {
        shuffledCard = Enumerable.Range(0, 51).ToList();
        ShuffleCard();
        Debug.Log(string.Join(", ", shuffledCard));

        // DisplayUI(playerUI1);
        // DisplayUI(playerUI2);
        // DisplayUI(opponentUI3);
        // DisplayUI(opponentUI4);

        DisplayCardUI(playerCardNumber1, playerCardMark1);
        DisplayCardUI(playerCardNumber2, playerCardMark2);
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

    // カードを一枚引く
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
    public void DisplayUI(TextMeshProUGUI resultText)
    {
        var (idNumber, _, _) = PopCard(); // Markは使わないので無視 `_`

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
    public void DisplayCardUI(TextMeshProUGUI numberText, TextMeshProUGUI markText)
    {
        var (idNumber, mark, number) = PopCard();

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
