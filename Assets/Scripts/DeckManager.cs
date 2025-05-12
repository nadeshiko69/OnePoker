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

    public GameObject cardPrefab;
    public GameObject playerDropZone;

    public Transform playerCard1Anchor;
    public Transform playerCard2Anchor;
    public Transform opponentCard1Anchor;
    public Transform opponentCard2Anchor;

    void Start()
    {
        shuffledCard = Enumerable.Range(0, 51).ToList();
        ShuffleCard();
        Debug.Log(string.Join(", ", shuffledCard));

        DrawCard(playerUI1, playerCard1);
        DrawCard(playerUI2, playerCard2);
        DrawCard(opponentUI1, opponentCard1);
        DrawCard(opponentUI2, opponentCard2);
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
            DisplayUpDownUI(resultText, cardValue);
            cardPrefab.SetCard(true);
            DisplayCardUI(playerCardNumber1, playerCardMark1, cardValue, Mark, Number);
            playerCard1.SetCardValue(cardValue);
            Debug.Log($"Player Card 1 set: Value={playerCard1.CardValue}, Display={Number}{marks[Mark]}");
        }
        else if (resultText == playerUI2)
        {
            DisplayUpDownUI(resultText, cardValue);
            cardPrefab.SetCard(true);
            DisplayCardUI(playerCardNumber2, playerCardMark2, cardValue, Mark, Number);
            playerCard2.SetCardValue(cardValue);
            Debug.Log($"Player Card 2 set: Value={playerCard2.CardValue}, Display={Number}{marks[Mark]}");
        }
        else if (resultText == opponentUI1)
        {
            DisplayUpDownUI(resultText, cardValue);
            cardPrefab.SetCard(false);
            opponentCard1.SetCardValue(cardValue);
            Debug.Log($"Opponent Card 1 set: Value={opponentCard1.CardValue}, Display={Number}{marks[Mark]}");
        }
        else if (resultText == opponentUI2)
        {
            DisplayUpDownUI(resultText, cardValue);
            cardPrefab.SetCard(false);
            opponentCard2.SetCardValue(cardValue);
            Debug.Log($"Opponent Card 2 set: Value={opponentCard2.CardValue}, Display={Number}{marks[Mark]}");
        }
    }

    public void RefillCardsToNextGame()
    {
        Debug.Log("RefillCard called");
        if (!playerCard1.isActiveAndEnabled)
        {
            Debug.Log("playerCard1 is null");
            RefillCard(playerCard1, playerUI1, cardPrefab, playerCard1Anchor);
        }
        else if (!playerCard2.isActiveAndEnabled)
        {
            Debug.Log("playerCard2 is null");
            RefillCard(playerCard2, playerUI2, cardPrefab, playerCard2Anchor);
        }

        if (!opponentCard1.isActiveAndEnabled)
        {
            Debug.Log("opponentCard1 is null");
            RefillCard(opponentCard1, opponentUI1, cardPrefab, opponentCard1Anchor);
        }
        else if (!opponentCard2.isActiveAndEnabled)
        {
            Debug.Log("opponentCard2 is null");
            RefillCard(opponentCard2, opponentUI2, cardPrefab, opponentCard2Anchor);
        }
    }

    private void RefillCard(CardDisplay card, TextMeshProUGUI UI, GameObject cardPrefab, Transform anchor)
    {
            var obj = Instantiate(cardPrefab, anchor);
            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = new Vector3(3f, 3f, 1f);
            }
            obj.transform.localRotation = Quaternion.Euler(0f, -90f, 65f);
            card = obj.GetComponent<CardDisplay>();
            DrawCard(UI, card);
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
    public void DisplayUpDownUI(TextMeshProUGUI resultText, int idNumber)
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

        Debug.Log($"DisplayUpDownUI: {resultText.text}");
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
