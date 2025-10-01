using UnityEngine;
using System;
using System.Collections;
using OnePoker.Network;

/// <summary>
/// オンライン対戦のカード配置を管理するクラス
/// カードの配置、確認、キャンセル、サーバー通知を担当
/// </summary>
public class OnlineCardPlacementManager : MonoBehaviour
{
    // カード配置管理
    private CardDisplay currentCard;
    private OnlineDropZone currentZone;
    private CardDisplay setPlayerCard;
    private bool canSetCard = false;
    
    // 依存関係
    private OnlinePanelManager panelManager;
    private OnlineHandManager handManager;
    private OnlineGameDataProvider gameDataProvider;
    
    // プロパティ
    public CardDisplay SetPlayerCard => setPlayerCard;
    public bool CanSetCard => canSetCard;
    
    // イベント
    public event Action<int> OnCardPlaced;
    public event Action<int, int> OnBothPlayersPlaced;
    
    public void Initialize(OnlinePanelManager panelMgr, OnlineHandManager handMgr, OnlineGameDataProvider dataProvider)
    {
        panelManager = panelMgr;
        handManager = handMgr;
        gameDataProvider = dataProvider;
        
        Debug.Log("[CardPlacementManager] Initialized");
    }
    
    /// <summary>
    /// カードセットを有効化
    /// </summary>
    public void EnableCardPlacement()
    {
        canSetCard = true;
        Debug.Log("[CardPlacementManager] Card placement enabled");
    }
    
    /// <summary>
    /// カードセットを無効化
    /// </summary>
    public void DisableCardPlacement()
    {
        canSetCard = false;
        Debug.Log("[CardPlacementManager] Card placement disabled");
    }
    
    /// <summary>
    /// カード配置確認パネルを表示
    /// </summary>
    public void ShowConfirmation(CardDisplay card, OnlineDropZone zone)
    {
        Debug.Log($"[CardPlacementManager] ShowConfirmation called for card: {card.CardValue}");
        
        if (!canSetCard)
        {
            Debug.LogWarning($"[CardPlacementManager] Cannot set card, returning to original position");
            ReturnCardToOriginalPosition(card);
            return;
        }
        
        currentCard = card;
        currentZone = zone;
        
        if (panelManager != null)
        {
            panelManager.confirmationPanel.SetActive(true);
            panelManager.yesButton.onClick.RemoveAllListeners();
            panelManager.noButton.onClick.RemoveAllListeners();
            panelManager.yesButton.onClick.AddListener(ConfirmPlacement);
            panelManager.noButton.onClick.AddListener(CancelPlacement);
        }
    }
    
    /// <summary>
    /// カード配置を確定
    /// </summary>
    public void ConfirmPlacement()
    {
        Debug.Log($"[CardPlacementManager] ConfirmPlacement called for card: {currentCard?.CardValue}");
        
        if (currentCard != null && currentZone != null)
        {
            // カードをDropZoneに配置
            currentCard.transform.SetParent(currentZone.transform);
            currentCard.transform.localPosition = Vector3.zero;
            
            setPlayerCard = currentCard;
            
            // 確認パネルを非表示
            if (panelManager != null)
            {
                panelManager.confirmationPanel.SetActive(false);
            }
            
            // サーバーに通知
            StartCoroutine(NotifyCardPlacement(currentCard.CardValue));
            
            canSetCard = false;
        }
    }
    
    /// <summary>
    /// カード配置をキャンセル
    /// </summary>
    public void CancelPlacement()
    {
        Debug.Log("[CardPlacementManager] CancelPlacement called");
        
        if (currentCard != null)
        {
            ReturnCardToOriginalPosition(currentCard);
        }
        
        if (panelManager != null)
        {
            panelManager.confirmationPanel.SetActive(false);
        }
        
        ResetPlacementState();
    }
    
    /// <summary>
    /// カードを元の位置に戻す
    /// </summary>
    private void ReturnCardToOriginalPosition(CardDisplay card)
    {
        Debug.Log($"[CardPlacementManager] Returning card {card.CardValue} to original position");
        
        if (card == null) return;
        
        var drag = card.GetComponent<CardDraggable>();
        if (drag != null && drag.OriginalParent != null)
        {
            card.transform.SetParent(drag.OriginalParent);
            
            if (drag.OriginalPosition != Vector3.zero)
            {
                card.transform.position = drag.OriginalPosition;
            }
            else
            {
                card.transform.localPosition = Vector3.zero;
            }
        }
        else if (handManager != null)
        {
            // フォールバック処理
            if (card == handManager.playerCard1 || card.CardValue == handManager.playerCard1?.CardValue)
            {
                card.transform.SetParent(handManager.playerCard1.transform.parent);
                card.transform.position = handManager.playerCard1.transform.position;
            }
            else if (card == handManager.playerCard2 || card.CardValue == handManager.playerCard2?.CardValue)
            {
                card.transform.SetParent(handManager.playerCard2.transform.parent);
                card.transform.position = handManager.playerCard2.transform.position;
            }
        }
    }
    
    /// <summary>
    /// 配置状態をリセット
    /// </summary>
    private void ResetPlacementState()
    {
        currentCard = null;
        currentZone = null;
        
        if (panelManager != null)
        {
            panelManager.confirmationPanel.SetActive(false);
            panelManager.yesButton.onClick.RemoveAllListeners();
            panelManager.noButton.onClick.RemoveAllListeners();
        }
    }
    
    /// <summary>
    /// サーバーにカード配置を通知
    /// </summary>
    private IEnumerator NotifyCardPlacement(int cardValue)
    {
        Debug.Log($"[CardPlacementManager] Notifying card placement: {cardValue}");
        
        SetCard(gameDataProvider.GameId, gameDataProvider.PlayerId, cardValue, OnCardPlacementSuccess, OnCardPlacementError);
        
        yield return null;
    }
    
    /// <summary>
    /// カードセットAPI呼び出し
    /// </summary>
    private void SetCard(string gameId, string playerId, int cardValue, Action<string> onSuccess, Action<string> onError)
    {
        string url = $"{HttpManager.ApiBaseUrl}/update-state";
        string jsonBody = JsonUtility.ToJson(new SetCardRequest
        {
            gameId = gameId,
            playerId = playerId,
            cardValue = cardValue
        });
        
        HttpManager.Instance.Post<SetCardResponse>(
            url,
            jsonBody,
            (response) => {
                string responseJson = JsonUtility.ToJson(response);
                onSuccess?.Invoke(responseJson);
            },
            (error) => {
                onError?.Invoke(error);
            }
        );
    }
    
    /// <summary>
    /// カード配置成功時のコールバック
    /// </summary>
    private void OnCardPlacementSuccess(string response)
    {
        Debug.Log($"[CardPlacementManager] Card placement successful: {response}");
        
        try
        {
            var setCardResponse = JsonUtility.FromJson<SetCardResponse>(response);
            
            // イベント発火
            OnCardPlaced?.Invoke(setCardResponse.player1CardValue);
            
            // 両プレイヤーがセット済みかチェック
            if (setCardResponse.player1Set && setCardResponse.player2Set)
            {
                Debug.Log("[CardPlacementManager] Both players have set cards");
                OnBothPlayersPlaced?.Invoke(setCardResponse.player1CardValue, setCardResponse.player2CardValue);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CardPlacementManager] Error parsing response: {e.Message}");
        }
    }
    
    /// <summary>
    /// カード配置エラー時のコールバック
    /// </summary>
    private void OnCardPlacementError(string error)
    {
        Debug.LogError($"[CardPlacementManager] Card placement error: {error}");
    }
    
    [Serializable]
    private class SetCardRequest
    {
        public string gameId;
        public string playerId;
        public int cardValue;
    }
    
    [Serializable]
    private class SetCardResponse
    {
        public bool player1Set;
        public bool player2Set;
        public int player1CardValue;
        public int player2CardValue;
    }
}

