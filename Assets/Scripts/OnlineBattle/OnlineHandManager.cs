using UnityEngine;

public class OnlineHandManager : MonoBehaviour
{
    public CardDisplay playerCard1;
    public CardDisplay playerCard2;
    public CardDisplay opponentCard1;
    public CardDisplay opponentCard2;

    // プレイヤーの手札をUIに反映
    public void SetPlayerHand(int[] cardIds)
    {
        if (cardIds.Length > 0)
        {
            playerCard1.SetCardValue(cardIds[0]);
            playerCard1.SetCard(true); // 表向き
        }
        if (cardIds.Length > 1)
        {
            playerCard2.SetCardValue(cardIds[1]);
            playerCard2.SetCard(true); // 表向き
        }
    }

    // 相手の手札をUIに反映（初期は裏向き）
    public void SetOpponentHand(int[] cardIds)
    {
        if (cardIds.Length > 0)
        {
            opponentCard1.SetCardValue(cardIds[0]);
            opponentCard1.SetCard(false); // 裏向き
        }
        if (cardIds.Length > 1)
        {
            opponentCard2.SetCardValue(cardIds[1]);
            opponentCard2.SetCard(false); // 裏向き
        }
    }
} 