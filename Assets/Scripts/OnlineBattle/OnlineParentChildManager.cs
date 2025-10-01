using UnityEngine;
using System;
using System.Collections;
using OnePoker.Network;

/// <summary>
/// オンライン対戦の親子システムを管理するクラス
/// 親子の初期化、交代、ターン管理、親のBet完了監視を担当
/// </summary>
public class OnlineParentChildManager : MonoBehaviour
{
    // 親子システム
    private bool isParent = false;
    private int currentRound = 0;
    private int parentChangeRound = 3;
    private bool isParentTurn = false;
    private bool waitingForParentAction = false;
    
    // 監視用コルーチン
    private Coroutine parentBetMonitorCoroutine;
    
    // 依存関係
    private OnlinePanelManager panelManager;
    private OnlineBettingManager bettingManager;
    private OnlineGameDataProvider gameDataProvider;
    
    // プロパティ
    public bool IsParent => isParent;
    public bool IsParentTurn => isParentTurn;
    public int CurrentRound => currentRound;
    
    // イベント
    public event Action OnParentTurnStarted;
    public event Action OnChildTurnStarted;
    public event Action<string, int> OnParentBetComplete; // betAction, betAmount
    
    public void Initialize(OnlinePanelManager panelMgr, OnlineBettingManager bettingMgr, OnlineGameDataProvider dataProvider)
    {
        panelManager = panelMgr;
        bettingManager = bettingMgr;
        gameDataProvider = dataProvider;
        
        Debug.Log("[ParentChildManager] Initialized");
    }
    
    /// <summary>
    /// 親子システムを初期化
    /// </summary>
    public void InitializeParentChildSystem(bool isPlayer1)
    {
        isParent = isPlayer1;
        currentRound = 1;
        
        Debug.Log($"[ParentChildManager] Parent-Child system initialized. isParent: {isParent}, round: {currentRound}");
        
        if (panelManager != null)
        {
            panelManager.UpdatePlayerRoleDisplay(isParent);
        }
    }
    
    /// <summary>
    /// 親のターンを開始
    /// </summary>
    public void StartParentTurn()
    {
        Debug.Log("[ParentChildManager] Starting parent turn");
        
        if (isParent)
        {
            // 自分が親の場合
            isParentTurn = true;
            waitingForParentAction = false;
            
            if (bettingManager != null)
            {
                bettingManager.SetMyTurn(true);
            }
            
            if (panelManager != null)
            {
                panelManager.ShowParentTurnPanel();
                panelManager.SetBettingButtonInteractable(true);
            }
            
            OnParentTurnStarted?.Invoke();
        }
        else
        {
            // 子の場合は親のアクション待ち
            isParentTurn = false;
            waitingForParentAction = true;
            
            if (bettingManager != null)
            {
                bettingManager.SetMyTurn(false);
            }
            
            if (panelManager != null)
            {
                panelManager.ShowWaitingForParentPanel();
                panelManager.ShowParentBettingPanel();
                panelManager.SetBettingButtonInteractable(false);
            }
            
            StartParentBetMonitoring();
        }
    }
    
    /// <summary>
    /// 子のターンを開始
    /// </summary>
    public void StartChildTurn()
    {
        Debug.Log($"[ParentChildManager] Starting child turn. isParent: {isParent}");
        
        if (!isParent)
        {
            // 自分が子の場合
            isParentTurn = false;
            waitingForParentAction = false;
            
            if (bettingManager != null)
            {
                bettingManager.SetMyTurn(true);
            }
            
            if (panelManager != null)
            {
                panelManager.HideWaitingForParentPanel();
                panelManager.HideParentBettingPanel();
                panelManager.SetBettingButtonInteractable(true);
                
                if (panelManager.betAmountText != null)
                {
                    panelManager.betAmountText.gameObject.SetActive(true);
                    panelManager.UpdateBetAmountDisplay(bettingManager.CurrentBetValue);
                }
            }
            
            // 親のベット額を取得して表示
            StartCoroutine(DisplayParentBetAmount());
            
            OnChildTurnStarted?.Invoke();
        }
        else
        {
            // 親の場合は子のアクション待ち
            isParentTurn = true;
            
            if (bettingManager != null)
            {
                bettingManager.SetMyTurn(false);
            }
            
            if (panelManager != null)
            {
                panelManager.ShowWaitingForChildPanel();
                panelManager.SetBettingButtonInteractable(false);
            }
        }
    }
    
    /// <summary>
    /// 子のターンを遅延付きで開始
    /// </summary>
    public void StartChildTurnWithDelay()
    {
        Debug.Log("[ParentChildManager] Starting child turn with delay");
        
        if (panelManager != null)
        {
            panelManager.ShowParentBetCompletePanel();
            panelManager.UpdateBetAmountDisplay(bettingManager.CurrentBetValue);
            StartCoroutine(DisplayParentBetAmount());
        }
        
        StartCoroutine(StartChildTurnAfterDelay());
    }
    
    /// <summary>
    /// 3秒後に子のターンを開始
    /// </summary>
    private IEnumerator StartChildTurnAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        StartChildTurn();
    }
    
    /// <summary>
    /// 親のベット額を表示
    /// </summary>
    private IEnumerator DisplayParentBetAmount()
    {
        if (gameDataProvider == null) yield break;
        
        HttpManager.Instance.GetGameState(
            gameDataProvider.GameId,
            gameDataProvider.PlayerId,
            OnParentBetAmountReceived,
            OnParentBetAmountError
        );
        
        yield return null;
    }
    
    /// <summary>
    /// 親のベット額受信時の処理
    /// </summary>
    private void OnParentBetAmountReceived(string response)
    {
        try
        {
            var gameState = JsonUtility.FromJson<GameStateResponse>(response);
            
            if (gameState != null && gameState.player1BetAmount > 0)
            {
                if (bettingManager != null)
                {
                    bettingManager.SetMinimumBetValue(gameState.player1BetAmount);
                    bettingManager.SetBetValue(gameState.player1BetAmount);
                }
                
                if (panelManager != null)
                {
                    panelManager.UpdateOpponentBetAmountDisplay(gameState.player1BetAmount);
                    panelManager.UpdateBetAmountDisplay(gameState.player1BetAmount);
                    panelManager.UpdateCallButtonText(gameState.player1BetAmount, gameState.player1BetAmount);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ParentChildManager] Error parsing parent bet amount: {e.Message}");
        }
    }
    
    /// <summary>
    /// 親のベット額エラー時の処理
    /// </summary>
    private void OnParentBetAmountError(string error)
    {
        Debug.LogError($"[ParentChildManager] Parent bet amount error: {error}");
    }
    
    /// <summary>
    /// 親のBet完了監視を開始
    /// </summary>
    private void StartParentBetMonitoring()
    {
        if (isParent) return;
        
        Debug.Log("[ParentChildManager] Starting parent bet monitoring");
        
        if (parentBetMonitorCoroutine != null)
        {
            StopCoroutine(parentBetMonitorCoroutine);
        }
        
        parentBetMonitorCoroutine = StartCoroutine(MonitorParentBetStatus());
    }
    
    /// <summary>
    /// 親のBet完了状態を監視
    /// </summary>
    private IEnumerator MonitorParentBetStatus()
    {
        while (waitingForParentAction)
        {
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(CheckParentBetStatus());
        }
    }
    
    /// <summary>
    /// 親のBet完了状態を確認
    /// </summary>
    private IEnumerator CheckParentBetStatus()
    {
        if (gameDataProvider == null) yield break;
        
        HttpManager.Instance.GetGameState(
            gameDataProvider.GameId,
            gameDataProvider.PlayerId,
            OnParentBetStatusReceived,
            OnParentBetStatusError
        );
        
        yield return null;
    }
    
    /// <summary>
    /// 親のBet状態受信時の処理
    /// </summary>
    private void OnParentBetStatusReceived(string response)
    {
        try
        {
            var gameState = JsonUtility.FromJson<GameStateResponse>(response);
            
            if (gameState != null && gameState.player1BetAmount > 0)
            {
                Debug.Log($"[ParentChildManager] Parent bet complete: {gameState.player1BetAmount}");
                
                // 監視を停止
                waitingForParentAction = false;
                if (parentBetMonitorCoroutine != null)
                {
                    StopCoroutine(parentBetMonitorCoroutine);
                    parentBetMonitorCoroutine = null;
                }
                
                // イベント発火
                OnParentBetComplete?.Invoke("call", gameState.player1BetAmount);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ParentChildManager] Error parsing parent bet status: {e.Message}");
        }
    }
    
    /// <summary>
    /// 親のBet状態エラー時の処理
    /// </summary>
    private void OnParentBetStatusError(string error)
    {
        Debug.LogError($"[ParentChildManager] Parent bet status error: {error}");
    }
    
    /// <summary>
    /// 親子交代をチェック
    /// </summary>
    public void CheckParentChange()
    {
        if (currentRound % parentChangeRound == 0)
        {
            isParent = !isParent;
            Debug.Log($"[ParentChildManager] Parent changed at round {currentRound}. New parent: {isParent}");
            
            if (panelManager != null)
            {
                panelManager.UpdatePlayerRoleDisplay(isParent);
            }
        }
    }
    
    /// <summary>
    /// ラウンド終了処理
    /// </summary>
    public void OnRoundEnd()
    {
        Debug.Log($"[ParentChildManager] Round {currentRound} ended");
        
        CheckParentChange();
        currentRound++;
        
        Debug.Log($"[ParentChildManager] Moving to round {currentRound}");
    }
    
    [Serializable]
    private class GameStateResponse
    {
        public int player1BetAmount;
    }
}

