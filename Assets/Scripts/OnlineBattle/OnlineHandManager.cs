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
        Debug.Log($"SetPlayerHand called with cardIds: {(cardIds != null ? string.Join(",", cardIds) : "null")}");
        Debug.Log($"playerCard1: {playerCard1 != null}, playerCard2: {playerCard2 != null}");
        
        if (playerCard1 != null)
        {
            if (cardIds != null && cardIds.Length > 0)
            {
                Debug.Log($"Setting playerCard1 to cardId: {cardIds[0]}");
                playerCard1.SetCardValue(cardIds[0]);
                playerCard1.SetCard(true); // 表向き
            }
            else
            {
                Debug.Log("Setting playerCard1 to default cardId: 0");
                playerCard1.SetCardValue(0);
                playerCard1.SetCard(true);
            }
        }
        else
        {
            Debug.LogError("playerCard1 is null!");
        }
        
        if (playerCard2 != null)
        {
            if (cardIds != null && cardIds.Length > 1)
            {
                Debug.Log($"Setting playerCard2 to cardId: {cardIds[1]}");
                playerCard2.SetCardValue(cardIds[1]);
                playerCard2.SetCard(true); // 表向き
            }
            else
            {
                Debug.Log("Setting playerCard2 to default cardId: 0");
                playerCard2.SetCardValue(0);
                playerCard2.SetCard(true);
            }
        }
        else
        {
            Debug.LogError("playerCard2 is null!");
        }
        
        Debug.Log("SetPlayerHand completed");
    }

    // 相手の手札をUIに反映（初期は裏向き）
    public void SetOpponentHand(int[] cardIds)
    {
        Debug.Log($"SetOpponentHand called with cardIds: {(cardIds != null ? string.Join(",", cardIds) : "null")}");
        Debug.Log($"opponentCard1: {opponentCard1 != null}, opponentCard2: {opponentCard2 != null}");
        
        if (opponentCard1 != null)
        {
            if (cardIds != null && cardIds.Length > 0)
            {
                Debug.Log($"Setting opponentCard1 to cardId: {cardIds[0]}");
                opponentCard1.SetCardValue(cardIds[0]);
                opponentCard1.SetCard(false); // 裏向き
            }
            else
            {
                Debug.Log("Setting opponentCard1 to default cardId: 0");
                opponentCard1.SetCardValue(0);
                opponentCard1.SetCard(false);
            }
        }
        else
        {
            Debug.LogError("opponentCard1 is null!");
        }
        
        if (opponentCard2 != null)
        {
            if (cardIds != null && cardIds.Length > 1)
            {
                Debug.Log($"Setting opponentCard2 to cardId: {cardIds[1]}");
                opponentCard2.SetCardValue(cardIds[1]);
                opponentCard2.SetCard(false); // 裏向き
            }
            else
            {
                Debug.Log("Setting opponentCard2 to default cardId: 0");
                opponentCard2.SetCardValue(0);
                opponentCard2.SetCard(false);
            }
        }
        else
        {
            Debug.LogError("opponentCard2 is null!");
        }
        
        Debug.Log("SetOpponentHand completed");
    }

    // 指定されたカードがプレイヤーのカードかどうかを判定
    public bool IsPlayerCard(CardDisplay card)
    {
        if (card == null) return false;
        
        return card == playerCard1 || card == playerCard2;
    }
} 