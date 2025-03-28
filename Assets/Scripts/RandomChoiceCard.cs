using UnityEngine;
using System.Linq;
using System.Collections.Generic; // Listを使う

public class RandomChoiceCard : MonoBehaviour
{
    private List<int> shuffledCard;
    public IReadOnlyList<int> ShuffledCard => shuffledCard;

    void Start()
    {
        shuffledCard = Enumerable.Range(0, 51).ToList();
        ShuffleCard();
        PrintCard();

        PopCard();
        PopCard();
        PopCard();
        PopCard();
    }

    public void ShuffleCard()
    {
        for (int i = shuffledCard.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffledCard[i], shuffledCard[j]) = (shuffledCard[j], shuffledCard[i]);
        }
    }

    public void PrintCard()
    {
        Debug.Log(string.Join(", ", shuffledCard));
    }

    public (int Mark, int Number) PopCard()
    {
        if (shuffledCard.Count == 0)
        {
            Debug.LogWarning("No more cards to pop!");
            return (-1, -1); // エラー値
        }

        int topCard = shuffledCard[shuffledCard.Count - 1];
        shuffledCard.RemoveAt(shuffledCard.Count - 1);

        int Mark = topCard / 13;  // 0 ダイヤ, 1 ハート, 2 スペード, 3 クラブ
        int Number = topCard % 13 + 1;

        Debug.Log($"Mark: {Mark}, Number: {Number}");
        return (Mark, Number);
    }
}
