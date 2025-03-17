// Card.cs
// カードの表示と管理を行うクラス
// カードの数字と表示を管理し、カードの表示を更新する   
// カードの数字を取得するメソッドも提供する

// 使用方法:
// 1. カードを表示するにはSetCard()を呼び出す
// 2. カードを裏返すにはFlipCard()を呼び出す
// 3. カードの数字を取得するにはGetNumber()を呼び出す

// 主な機能:
// - カードの数字の設定と取得
// - カードの表示状態(表/裏)の切り替え
// - カードの表示更新（数字の表示、カードの色）
// - カードのアニメーション効果（フリップ、移動など）


using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Image cardImage;
    
    private int number;
    private bool isFaceUp = false;
    
    public void SetCard(int num)
    {
        number = num;
        UpdateCardDisplay();
    }
    
    public void FlipCard(bool faceUp)
    {
        isFaceUp = faceUp;
        UpdateCardDisplay();
    }
    
    private void UpdateCardDisplay()
    {
        if (isFaceUp)
        {
            numberText.text = number.ToString();
            cardImage.color = Color.white;
        }
        else
        {
            numberText.text = "";
            cardImage.color = Color.gray;
        }
    }
    
    public int GetNumber()
    {
        return number;
    }
} 