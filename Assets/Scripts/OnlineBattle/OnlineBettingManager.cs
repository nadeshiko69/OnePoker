using UnityEngine;
using System;
using OnePoker.Network;

/// <summary>
/// オンライン対戦のベッティングを管理するクラス
/// ベット値の管理、アクション実行、UI更新を担当
/// </summary>
public class OnlineBettingManager : MonoBehaviour
{
    // ベット関連
    private int currentBetValue = 1;
    private int minimumBetValue = 1;
    private bool isMyTurn = false;
    
    // 依存関係
    private OnlinePanelManager panelManager;
    private OnlineGameDataProvider gameDataProvider;
    
    // プロパティ
    public int CurrentBetValue => currentBetValue;
    public int MinimumBetValue => minimumBetValue;
    public bool IsMyTurn => isMyTurn;
    
    // イベント
    public event Action<string, int> OnBetActionExecuted; // actionType, betValue
    
    public void Initialize(OnlinePanelManager panelMgr, OnlineGameDataProvider dataProvider)
    {
        panelManager = panelMgr;
        gameDataProvider = dataProvider;
        
        Debug.Log("[BettingManager] Initialized");
    }
    
    /// <summary>
    /// ベット値を増やす
    /// </summary>
    public void IncreaseBetValue()
    {
        if (currentBetValue < 10)
        {
            currentBetValue++;
            Debug.Log($"[BettingManager] Bet increased to: {currentBetValue}");
            UpdateBetUI();
        }
    }
    
    /// <summary>
    /// ベット値を減らす
    /// </summary>
    public void DecreaseBetValue()
    {
        if (currentBetValue > minimumBetValue)
        {
            currentBetValue--;
            Debug.Log($"[BettingManager] Bet decreased to: {currentBetValue}");
            UpdateBetUI();
        }
    }
    
    /// <summary>
    /// ベットアクションを実行
    /// </summary>
    public void ExecuteBetAction(string actionType)
    {
        Debug.Log($"[BettingManager] Executing bet action: {actionType}, value: {currentBetValue}");
        
        if (!isMyTurn)
        {
            Debug.LogWarning("[BettingManager] Not my turn, ignoring action");
            return;
        }
        
        // サーバーに送信
        SendBetActionToServer(actionType, currentBetValue);
        
        // イベント発火
        OnBetActionExecuted?.Invoke(actionType, currentBetValue);
    }
    
    /// <summary>
    /// Call/Raiseアクション
    /// </summary>
    public void CallOrRaise()
    {
        string actionType = currentBetValue == minimumBetValue ? "call" : "raise";
        ExecuteBetAction(actionType);
    }
    
    /// <summary>
    /// Dropアクション
    /// </summary>
    public void Drop()
    {
        ExecuteBetAction("drop");
    }
    
    /// <summary>
    /// ベット値を設定
    /// </summary>
    public void SetBetValue(int value)
    {
        currentBetValue = value;
        Debug.Log($"[BettingManager] Bet value set to: {currentBetValue}");
        UpdateBetUI();
    }
    
    /// <summary>
    /// 最低ベット値を設定
    /// </summary>
    public void SetMinimumBetValue(int value)
    {
        minimumBetValue = value;
        if (currentBetValue < minimumBetValue)
        {
            currentBetValue = minimumBetValue;
        }
        Debug.Log($"[BettingManager] Minimum bet set to: {minimumBetValue}");
        UpdateBetUI();
    }
    
    /// <summary>
    /// ターン状態を設定
    /// </summary>
    public void SetMyTurn(bool isMyTurn)
    {
        this.isMyTurn = isMyTurn;
        Debug.Log($"[BettingManager] My turn: {isMyTurn}");
        
        if (panelManager != null)
        {
            panelManager.SetBettingButtonInteractable(isMyTurn);
        }
    }
    
    /// <summary>
    /// ベットUIを更新
    /// </summary>
    private void UpdateBetUI()
    {
        if (panelManager != null)
        {
            panelManager.UpdateCallButtonText(currentBetValue, minimumBetValue);
            panelManager.UpdateBetAmountDisplay(currentBetValue);
            panelManager.SetBettingButtonInteractable(isMyTurn);
        }
    }
    
    /// <summary>
    /// ベットアクションをサーバーに送信
    /// </summary>
    private void SendBetActionToServer(string actionType, int betValue)
    {
        Debug.Log($"[BettingManager] Sending bet action to server: {actionType}, {betValue}");
        
        HttpManager.Instance.SendBetAction(
            gameDataProvider.GameId,
            gameDataProvider.PlayerId,
            actionType,
            betValue,
            OnBetActionSuccess,
            OnBetActionError
        );
    }
    
    /// <summary>
    /// Player1BetAmountをサーバーに更新
    /// </summary>
    public void UpdatePlayerBetAmountInServer(int betAmount)
    {
        Debug.Log($"[BettingManager] Updating player bet amount in server: {betAmount}");
        
        HttpManager.Instance.UpdatePlayerBetAmount(
            gameDataProvider.GameId,
            gameDataProvider.PlayerId,
            betAmount,
            OnBetAmountUpdateSuccess,
            OnBetAmountUpdateError
        );
    }
    
    /// <summary>
    /// ベットアクション成功時の処理
    /// </summary>
    private void OnBetActionSuccess(string response)
    {
        Debug.Log($"[BettingManager] Bet action successful: {response}");
        
        try
        {
            var betResponse = JsonUtility.FromJson<BetActionResponse>(response);
            
            if (betResponse != null)
            {
                isMyTurn = betResponse.isMyTurn;
                UpdateBetUI();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BettingManager] Error parsing bet response: {e.Message}");
        }
    }
    
    /// <summary>
    /// ベットアクションエラー時の処理
    /// </summary>
    private void OnBetActionError(string error)
    {
        Debug.LogError($"[BettingManager] Bet action error: {error}");
        
        if (panelManager != null)
        {
            panelManager.SetBettingButtonInteractable(true);
        }
    }
    
    /// <summary>
    /// ベット金額更新成功時の処理
    /// </summary>
    private void OnBetAmountUpdateSuccess(string response)
    {
        Debug.Log($"[BettingManager] Bet amount updated: {response}");
    }
    
    /// <summary>
    /// ベット金額更新エラー時の処理
    /// </summary>
    private void OnBetAmountUpdateError(string error)
    {
        Debug.LogError($"[BettingManager] Bet amount update error: {error}");
    }
    
    [Serializable]
    private class BetActionResponse
    {
        public string gameId;
        public string gamePhase;
        public bool isMyTurn;
        public int currentBet;
        public string message;
    }
}

