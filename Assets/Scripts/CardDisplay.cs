using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public TextMeshProUGUI numberText;
    public TextMeshProUGUI markText;
    public Image cardImage;
    public Sprite frontSprite;
    public Sprite backSprite;

    private bool isFaceUp = true; // 表向きかどうか
    private int cardValue;
    private readonly string[] cardRanks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private readonly string[] marks = { "♦", "♥", "♠", "♣" };
    private readonly Color redColor = new Color(1f, 0.2f, 0.2f);    // 文字色　赤
    private readonly Color blackColor = new Color(0.1f, 0.1f, 0.1f); // 文字色　黒

    // カードの値を取得するためのプロパティ
    public int CardValue => cardValue;

    // カードを設定
    public void SetCard(bool faceUp)
    {
        isFaceUp = faceUp;

        if (isFaceUp)
        {
            cardImage.sprite = frontSprite;
            numberText.enabled = true;
            markText.enabled = true;

            // カードの数字とマークを表示
            if (numberText != null && markText != null)
            {
                // カードの値を表示（例：cardValueを使用）
                int mark = cardValue / 13;  // マーク（0-3: ダイヤ、ハート、スペード、クラブ）
                string number = cardRanks[cardValue % 13];

                numberText.text = number;
                markText.text = marks[mark];

                // マークの色を設定
                Color textColor = (mark == 0 || mark == 1) ? redColor : blackColor;
                numberText.color = textColor;
                markText.color = textColor;
            }
        }
        else
        {
            cardImage.sprite = backSprite;
            numberText.enabled = false;
            markText.enabled = false;
        }
    }

    // 裏返す
    public void FlipCard()
    {
        SetCard(!isFaceUp);
    }


}
