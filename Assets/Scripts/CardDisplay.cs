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

    // カードを設定
    public void SetCard(bool faceUp)
    {
        isFaceUp = faceUp;

        if (isFaceUp)
        {
            cardImage.sprite = frontSprite;
            numberText.enabled = true;
            markText.enabled = true;
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
