using UnityEngine;
using System.Collections;
using OnePoker.Network;

/// <summary>
/// ベットフェーズのターン監視とUI制御を管理
/// awaitingPlayerベースのシンプルな実装
/// </summary>
public class OnlineBettingTurnManager : MonoBehaviour
{
    // 依存関係
    private OnlineGameDataProvider gameDataProvider;
    private OnlineBettingManager bettingManager;
    private OnlinePanelManager panelManager;
    
    // 監視用
    private Coroutine turnMonitorCoroutine;
    private bool isMonitoring = false;
    
    // 状態キャッシュ（重複呼び出し防止）
    private string lastAwaitingPlayer = "";
    private int lastRequiredBet = -1;
    
    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(
        OnlineGameDataProvider dataProvider,
        OnlineBettingManager bettingMgr,
        OnlinePanelManager panelMgr)
    {
        gameDataProvider = dataProvider;
        bettingManager = bettingMgr;
        panelManager = panelMgr;
        
        Debug.Log("[BettingTurnManager] Initialized");
    }
    
    /// <summary>
    /// ターン監視を開始
    /// </summary>
    public void StartTurnMonitoring()
    {
        if (isMonitoring) return;
        
        isMonitoring = true;
        Debug.Log("[BettingTurnManager] Starting turn monitoring");
        
        if (turnMonitorCoroutine != null)
        {
            StopCoroutine(turnMonitorCoroutine);
        }
        
        turnMonitorCoroutine = StartCoroutine(MonitorTurnStatus());
    }
    
    /// <summary>
    /// ターン監視を停止
    /// </summary>
    public void StopTurnMonitoring()
    {
        isMonitoring = false;
        
        if (turnMonitorCoroutine != null)
        {
            StopCoroutine(turnMonitorCoroutine);
            turnMonitorCoroutine = null;
        }
        
        Debug.Log("[BettingTurnManager] Turn monitoring stopped");
    }
    
    /// <summary>
    /// ターン状態を監視（1秒間隔）
    /// </summary>
    private IEnumerator MonitorTurnStatus()
    {
        while (isMonitoring)
        {
            yield return new WaitForSeconds(1f);
            
            if (gameDataProvider != null && gameDataProvider.IsValid())
            {
                CheckTurnStatus();
            }
        }
    }
    
    /// <summary>
    /// ゲーム状態を取得してターン判定
    /// </summary>
    private void CheckTurnStatus()
    {
        HttpManager.Instance.GetGameState(
            gameDataProvider.GameId,
            gameDataProvider.PlayerId,
            OnGameStateReceived,
            OnGameStateError
        );
    }
    
    /// <summary>
    /// ゲーム状態受信成功
    /// </summary>
    private void OnGameStateReceived(string response)
    {
        try
        {
            var gameState = JsonUtility.FromJson<GameStateResponse>(response);
            
            if (gameState == null)
            {
                Debug.LogError("[BettingTurnManager] Failed to parse game state");
                return;
            }
            
            Debug.Log($"[BettingTurnManager] Game state received - awaitingPlayer: {gameState.awaitingPlayer}, currentRequiredBet: {gameState.currentRequiredBet}");
            
            // ターン判定
            HandleTurnChange(gameState);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BettingTurnManager] Error parsing game state: {e.Message}");
        }
    }
    
    /// <summary>
    /// ゲーム状態取得失敗
    /// </summary>
    private void OnGameStateError(string error)
    {
        Debug.LogError($"[BettingTurnManager] Failed to get game state: {error}");
    }
    
    /// <summary>
    /// ターン変更を処理
    /// </summary>
    private void HandleTurnChange(GameStateResponse gameState)
    {
        // 状態が変わっていない場合は処理をスキップ
        if (gameState.awaitingPlayer == lastAwaitingPlayer && 
            gameState.currentRequiredBet == lastRequiredBet)
        {
            return; // 変化なし、何もしない
        }
        
        // 状態をキャッシュ
        lastAwaitingPlayer = gameState.awaitingPlayer;
        lastRequiredBet = gameState.currentRequiredBet;
        
        // 自分のプレイヤー識別子（"P1" or "P2"）
        string mySide = gameDataProvider.IsPlayer1 ? "P1" : "P2";
        
        // 自分のターンか判定
        bool isMyTurn = (gameState.awaitingPlayer == mySide);
        
        Debug.Log($"[BettingTurnManager] Turn change detected - MySide: {mySide}, AwaitingPlayer: {gameState.awaitingPlayer}, IsMyTurn: {isMyTurn}, RequiredBet: {gameState.currentRequiredBet}");
        
        if (isMyTurn)
        {
            // 自分のターン
            EnableMyTurn(gameState.currentRequiredBet);
        }
        else if (gameState.awaitingPlayer == "none")
        {
            // ベット完了（Revealへ遷移）
            Debug.Log("[BettingTurnManager] Betting complete, transitioning to reveal");
            StopTurnMonitoring();
        }
        else
        {
            // 相手のターン
            DisableMyTurn();
        }
    }
    
    /// <summary>
    /// 自分のターンを有効化
    /// </summary>
    private void EnableMyTurn(int requiredBet)
    {
        Debug.Log($"[BettingTurnManager] Enabling my turn with required bet: {requiredBet}");
        
        // ベット額を設定
        if (bettingManager != null)
        {
            bettingManager.SetMinimumBetValue(requiredBet);
            bettingManager.SetBetValue(requiredBet);
            bettingManager.SetMyTurn(true);
        }
        
        // UIを表示
        if (panelManager != null)
        {
            panelManager.ShowParentTurnPanel(); // 3秒後に自動非表示
            panelManager.UpdateBetAmountDisplay(requiredBet);
            panelManager.UpdateCallButtonText(requiredBet, requiredBet);
            panelManager.SetBettingButtonInteractable(true);
        }
    }
    
    /// <summary>
    /// 自分のターンを無効化（相手のターン）
    /// </summary>
    private void DisableMyTurn()
    {
        Debug.Log("[BettingTurnManager] Disabling my turn (opponent's turn)");
        
        // ベット入力を無効化
        if (bettingManager != null)
        {
            bettingManager.SetMyTurn(false);
        }
        
        // 待機UIを表示
        if (panelManager != null)
        {
            panelManager.ShowWaitingForChildPanel();
            panelManager.SetBettingButtonInteractable(false);
        }
    }
    
    /// <summary>
    /// ゲーム状態レスポンス
    /// </summary>
    [System.Serializable]
    private class GameStateResponse
    {
        public string gameId;
        public string gamePhase;
        public string awaitingPlayer;      // "P1", "P2", "none"
        public int currentRequiredBet;     // 現在必要なベット額
        public int player1BetAmount;
        public int player2BetAmount;
    }
}
