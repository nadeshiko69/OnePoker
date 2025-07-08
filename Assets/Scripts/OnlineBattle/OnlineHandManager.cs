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
        if (playerCard1 != null)
        {
            if (cardIds != null && cardIds.Length > 0)
            {
                playerCard1.SetCardValue(cardIds[0]);
                playerCard1.SetCard(true); // 表向き
            }
            else
            {
                playerCard1.SetCardValue(0);
                playerCard1.SetCard(true);
            }
        }
        if (playerCard2 != null)
        {
            if (cardIds != null && cardIds.Length > 1)
            {
                playerCard2.SetCardValue(cardIds[1]);
                playerCard2.SetCard(true); // 表向き
            }
            else
            {
                playerCard2.SetCardValue(0);
                playerCard2.SetCard(true);
            }
        }
    }

    // 相手の手札をUIに反映（初期は裏向き）
    public void SetOpponentHand(int[] cardIds)
    {
        if (opponentCard1 != null)
        {
            if (cardIds != null && cardIds.Length > 0)
            {
                opponentCard1.SetCardValue(cardIds[0]);
                opponentCard1.SetCard(false); // 裏向き
            }
            else
            {
                opponentCard1.SetCardValue(0);
                opponentCard1.SetCard(false);
            }
        }
        if (opponentCard2 != null)
        {
            if (cardIds != null && cardIds.Length > 1)
            {
                opponentCard2.SetCardValue(cardIds[1]);
                opponentCard2.SetCard(false); // 裏向き
            }
            else
            {
                opponentCard2.SetCardValue(0);
                opponentCard2.SetCard(false);
            }
        }
    }
} 