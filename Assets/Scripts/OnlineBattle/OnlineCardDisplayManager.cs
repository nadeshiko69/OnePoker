using UnityEngine;
using System;

/// <summary>
/// オンライン対戦のカード表示を管理するクラス
/// 相手のカード表示、表裏の切り替えを担当
/// </summary>
public class OnlineCardDisplayManager : MonoBehaviour
{
    [Header("Card Prefab")]
    public GameObject cardPrefab;
    
    private OnlineGameDataProvider gameDataProvider;
    
    public void Initialize(OnlineGameDataProvider dataProvider)
    {
        gameDataProvider = dataProvider;
        Debug.Log("[CardDisplayManager] Initialized");
    }
    
    /// <summary>
    /// 相手のカードを裏向きで表示
    /// </summary>
    public void DisplayOpponentCardFaceDown(int cardValue)
    {
        Debug.Log($"[CardDisplayManager] Displaying opponent card face down: {cardValue}");
        
        OnlineDropZone opponentZone = FindOpponentDropZone();
        
        if (opponentZone != null)
        {
            CreateOpponentCard(cardValue, opponentZone, true);
        }
        else
        {
            Debug.LogError("[CardDisplayManager] Could not find opponent DropZone");
        }
    }
    
    /// <summary>
    /// 相手のカードを表向きで表示
    /// </summary>
    public void DisplayOpponentCardFaceUp(int cardValue)
    {
        Debug.Log($"[CardDisplayManager] Displaying opponent card face up: {cardValue}");
        
        OnlineDropZone opponentZone = FindOpponentDropZone();
        
        if (opponentZone != null)
        {
            CardDisplay opponentCard = opponentZone.GetComponentInChildren<CardDisplay>();
            
            if (opponentCard != null)
            {
                // 既存のカードを表向きに
                opponentCard.SetCardValue(cardValue);
                opponentCard.SetCardFaceDown(false);
                Debug.Log($"[CardDisplayManager] Opponent card {cardValue} flipped face up");
            }
            else
            {
                // カードが見つからない場合は新規作成
                Debug.LogWarning("[CardDisplayManager] No opponent card found, creating new one");
                CreateOpponentCard(cardValue, opponentZone, false);
            }
        }
        else
        {
            Debug.LogError("[CardDisplayManager] Could not find opponent DropZone");
        }
    }
    
    /// <summary>
    /// 相手のカードが確実に表示されているか確認
    /// </summary>
    public void EnsureOpponentCardDisplayed()
    {
        Debug.Log("[CardDisplayManager] Ensuring opponent card is displayed");
        
        OnlineDropZone opponentZone = FindOpponentDropZone();
        
        if (opponentZone != null)
        {
            CardDisplay existingCard = opponentZone.GetComponentInChildren<CardDisplay>();
            
            if (existingCard == null)
            {
                int opponentCardValue = gameDataProvider.GetOpponentCardValue(0);
                Debug.Log($"[CardDisplayManager] No opponent card found, creating with value: {opponentCardValue}");
                CreateOpponentCard(opponentCardValue, opponentZone, true);
            }
            else
            {
                Debug.Log("[CardDisplayManager] Opponent card already displayed");
            }
        }
        else
        {
            Debug.LogError("[CardDisplayManager] Could not find opponent DropZone");
        }
    }
    
    /// <summary>
    /// 相手側のDropZoneを探す
    /// </summary>
    private OnlineDropZone FindOpponentDropZone()
    {
        OnlineDropZone[] dropZones = FindObjectsOfType<OnlineDropZone>();
        
        foreach (OnlineDropZone zone in dropZones)
        {
            if (!zone.isPlayerZone)
            {
                Debug.Log($"[CardDisplayManager] Found opponent DropZone: {zone.name}");
                return zone;
            }
        }
        
        Debug.LogWarning("[CardDisplayManager] No opponent DropZone found");
        return null;
    }
    
    /// <summary>
    /// 相手のカードを生成して配置
    /// </summary>
    private void CreateOpponentCard(int cardValue, OnlineDropZone opponentZone, bool faceDown)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("[CardDisplayManager] cardPrefab is null! Please assign it in the Inspector.");
            return;
        }
        
        GameObject cardObject = Instantiate(cardPrefab, opponentZone.transform);
        CardDisplay cardDisplay = cardObject.GetComponent<CardDisplay>();
        
        if (cardDisplay != null)
        {
            cardDisplay.SetCardValue(cardValue);
            cardDisplay.SetCardFaceDown(faceDown);
            cardObject.transform.localPosition = Vector3.zero;
            
            string faceState = faceDown ? "face down" : "face up";
            Debug.Log($"[CardDisplayManager] Opponent card {cardValue} created {faceState}");
        }
        else
        {
            Debug.LogError("[CardDisplayManager] CardDisplay component not found on card prefab");
        }
    }
}

