using UnityEngine;

public class OnlineHandManager : MonoBehaviour
{
    public CardDisplay playerCard1;
    public CardDisplay playerCard2;
    public CardDisplay opponentCard1;
    public CardDisplay opponentCard2;

    // 参照がMissingの場合に自動で再取得する
    private void EnsureBindings()
    {
        if (playerCard1 == null) playerCard1 = FindOrCreateCard("Player_Card1", "Player_CardAnchor1");
        if (playerCard2 == null) playerCard2 = FindOrCreateCard("Player_Card2", "Player_CardAnchor2");
        if (opponentCard1 == null) opponentCard1 = FindOrCreateCard("Opponent_Card1", "Opponent_CardAnchor1");
        if (opponentCard2 == null) opponentCard2 = FindOrCreateCard("Opponent_Card2", "Opponent_CardAnchor2");
    }

    // 指定アンカー配下にカードPrefabを再生成して返す（まず既存を探索）
    private CardDisplay FindOrCreateCard(string cardName, string anchorName)
    {
        var existing = GameObject.Find(cardName);
        if (existing != null)
        {
            var cd = existing.GetComponent<CardDisplay>();
            if (cd != null) return cd;
        }

        var anchor = GameObject.Find(anchorName);
        if (anchor == null)
        {
            Debug.LogWarning($"[HAND_DEBUG] Anchor not found: {anchorName}");
            return null;
        }

        // PrefabをResourcesからロードして生成（Assets/Resources/Prefabs/Card.prefabを想定）
        var prefab = Resources.Load<GameObject>("Prefabs/Card");
        if (prefab == null)
        {
            Debug.LogError("[HAND_DEBUG] Card prefab not found at Resources/Prefabs/Card");
            return null;
        }

        var go = Instantiate(prefab, anchor.transform);
        go.name = cardName;
        // アンカー中央に整列
        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        var display = go.GetComponent<CardDisplay>();
        if (display == null)
        {
            display = go.AddComponent<CardDisplay>();
        }
        // ドラッグ可能にしたい場合はCardDraggableが付いている前提（無ければ付与可能）
        if (go.GetComponent<CardDraggable>() == null)
        {
            go.AddComponent<CardDraggable>();
        }

        Debug.Log($"[HAND_DEBUG] Recreated missing card under {anchorName}: {cardName}");
        return display;
    }

    // プレイヤーの手札をUIに反映
    public void SetPlayerHand(int[] cardIds)
    {
        EnsureBindings();
        Debug.Log($"[HAND_DEBUG] SetPlayerHand called with cardIds: {(cardIds != null ? string.Join(",", cardIds) : "null")}");
        Debug.Log($"[HAND_DEBUG] playerCard1: {playerCard1 != null}, playerCard2: {playerCard2 != null}");
        
        try
        {
        
        if (playerCard1 != null)
        {
            Debug.Log($"[HAND_DEBUG] playerCard1 GameObject active: {playerCard1.gameObject.activeInHierarchy}, enabled: {playerCard1.enabled}");
            
            // コンポーネントが無効になっている場合は有効にする
            if (!playerCard1.enabled)
            {
                Debug.Log("[HAND_DEBUG] playerCard1 component was disabled, enabling it");
                playerCard1.enabled = true;
            }
            
            if (cardIds != null && cardIds.Length > 0)
            {
                Debug.Log($"Setting playerCard1 to cardId: {cardIds[0]}");
                playerCard1.SetCardValue(cardIds[0]);
                playerCard1.SetCard(true); // 表向き
                Debug.Log($"[HAND_DEBUG] playerCard1.SetCard completed");
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
            Debug.Log($"[HAND_DEBUG] playerCard2 GameObject active: {playerCard2.gameObject.activeInHierarchy}, enabled: {playerCard2.enabled}");
            
            // コンポーネントが無効になっている場合は有効にする
            if (!playerCard2.enabled)
            {
                Debug.Log("[HAND_DEBUG] playerCard2 component was disabled, enabling it");
                playerCard2.enabled = true;
            }
            
            if (cardIds != null && cardIds.Length > 1)
            {
                Debug.Log($"Setting playerCard2 to cardId: {cardIds[1]}");
                playerCard2.SetCardValue(cardIds[1]);
                playerCard2.SetCard(true); // 表向き
                Debug.Log($"[HAND_DEBUG] playerCard2.SetCard completed");
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
        
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HAND_DEBUG] Exception in SetPlayerHand: {e.Message}");
        }
    }

    // 相手の手札をUIに反映（初期は裏向き）
    public void SetOpponentHand(int[] cardIds)
    {
        EnsureBindings();
        Debug.Log($"[HAND_DEBUG] SetOpponentHand called with cardIds: {(cardIds != null ? string.Join(",", cardIds) : "null")}");
        Debug.Log($"[HAND_DEBUG] opponentCard1: {opponentCard1 != null}, opponentCard2: {opponentCard2 != null}");
        
        try
        {
        
        if (opponentCard1 != null)
        {
            Debug.Log($"[HAND_DEBUG] opponentCard1 GameObject active: {opponentCard1.gameObject.activeInHierarchy}, enabled: {opponentCard1.enabled}");
            
            // コンポーネントが無効になっている場合は有効にする
            if (!opponentCard1.enabled)
            {
                Debug.Log("[HAND_DEBUG] opponentCard1 component was disabled, enabling it");
                opponentCard1.enabled = true;
            }
            
            if (cardIds != null && cardIds.Length > 0)
            {
                Debug.Log($"Setting opponentCard1 to cardId: {cardIds[0]}");
                opponentCard1.SetCardValue(cardIds[0]);
                opponentCard1.SetCard(false); // 裏向き
                Debug.Log($"[HAND_DEBUG] opponentCard1.SetCard completed");
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
            Debug.Log($"[HAND_DEBUG] opponentCard2 GameObject active: {opponentCard2.gameObject.activeInHierarchy}, enabled: {opponentCard2.enabled}");
            
            // コンポーネントが無効になっている場合は有効にする
            if (!opponentCard2.enabled)
            {
                Debug.Log("[HAND_DEBUG] opponentCard2 component was disabled, enabling it");
                opponentCard2.enabled = true;
            }
            
            if (cardIds != null && cardIds.Length > 1)
            {
                Debug.Log($"Setting opponentCard2 to cardId: {cardIds[1]}");
                opponentCard2.SetCardValue(cardIds[1]);
                opponentCard2.SetCard(false); // 裏向き
                Debug.Log($"[HAND_DEBUG] opponentCard2.SetCard completed");
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
        
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HAND_DEBUG] Exception in SetOpponentHand: {e.Message}");
        }
    }

    // 指定されたカードがプレイヤーのカードかどうかを判定
    public bool IsPlayerCard(CardDisplay card)
    {
        if (card == null) return false;
        
        return card == playerCard1 || card == playerCard2;
    }
} 