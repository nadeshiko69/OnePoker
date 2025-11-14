using UnityEngine;
using System.Collections;
using System;
using OnePoker.Network;

/// <summary>
/// オンライン対戦のゲームフェーズを管理するクラス
/// フェーズの状態管理、遷移、監視を担当
/// </summary>
public class OnlinePhaseManager : MonoBehaviour
{
    // フェーズ状態フラグ
    private bool isSetPhaseActive = false;
    private bool isRevealPhaseActive = false;
    private bool isSetCompletePhaseActive = false;
    private bool isBettingPhaseActive = false;
    
    // 監視用コルーチン
    private Coroutine gamePhaseMonitorCoroutine;
    
    // 依存関係
    private OnlinePanelManager panelManager;
    private OnlineGameDataProvider gameDataProvider;
    
    // プロパティ
    public bool IsSetPhaseActive => isSetPhaseActive;
    public bool IsRevealPhaseActive => isRevealPhaseActive;
    public bool IsSetCompletePhaseActive => isSetCompletePhaseActive;
    public bool IsBettingPhaseActive => isBettingPhaseActive;
    
    // イベント
    public event Action<string> OnPhaseChanged;
    public event Action OnSetPhaseStarted;
    public event Action OnBettingPhaseStarted;
    public event Action OnRevealPhaseStarted;
    
    public void Initialize(OnlinePanelManager panelMgr, OnlineGameDataProvider dataProvider)
    {
        panelManager = panelMgr;
        gameDataProvider = dataProvider;
        Debug.Log("[PhaseManager] Initialized");
    }
    
    /// <summary>
    /// ゲームフェーズ監視を開始
    /// </summary>
    public void StartPhaseMonitoring()
    {
        Debug.Log("[PhaseManager] Starting phase monitoring");
        
        if (gamePhaseMonitorCoroutine != null)
        {
            StopCoroutine(gamePhaseMonitorCoroutine);
        }
        
        gamePhaseMonitorCoroutine = StartCoroutine(MonitorGamePhase());
    }
    
    /// <summary>
    /// ゲームフェーズ監視を停止
    /// </summary>
    public void StopPhaseMonitoring()
    {
        if (gamePhaseMonitorCoroutine != null)
        {
            StopCoroutine(gamePhaseMonitorCoroutine);
            gamePhaseMonitorCoroutine = null;
            Debug.Log("[PhaseManager] Phase monitoring stopped");
        }
    }
    
    /// <summary>
    /// ゲームフェーズを定期的に監視
    /// </summary>
    private IEnumerator MonitorGamePhase()
    {
        Debug.Log("[PhaseManager] Phase monitoring coroutine started");
        
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            
            // セット完了フェーズは監視を継続
            if (isSetCompletePhaseActive)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
            if (!string.IsNullOrEmpty(gameDataProvider.GameId) && !string.IsNullOrEmpty(gameDataProvider.PlayerId))
            {
                StartCoroutine(CheckGamePhase());
            }
        }
    }
    
    /// <summary>
    /// 現在のゲームフェーズをサーバーに確認
    /// </summary>
    private IEnumerator CheckGamePhase()
    {
        try
        {
            HttpManager.Instance.GetGameState(
                gameDataProvider.GameId, 
                gameDataProvider.PlayerId, 
                OnGameStateReceived, 
                OnGameStateError
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhaseManager] Error checking game phase: {e.Message}");
        }
        
        yield return null;
    }
    
    /// <summary>
    /// ゲーム状態受信時の処理
    /// </summary>
    private void OnGameStateReceived(string response)
    {
        try
        {
            var gameState = JsonUtility.FromJson<GameStateResponse>(response);
            
            if (gameState != null)
            {
                // 使用済スキルをプロバイダに反映
                if (gameDataProvider != null)
                {
                    var p1 = gameState.player1UsedSkills != null ? new System.Collections.Generic.List<string>(gameState.player1UsedSkills) : new System.Collections.Generic.List<string>();
                    var p2 = gameState.player2UsedSkills != null ? new System.Collections.Generic.List<string>(gameState.player2UsedSkills) : new System.Collections.Generic.List<string>();
                    gameDataProvider.UpdateUsedSkills(p1, p2);
                    
                    // 手札情報を更新
                    if (gameState.player1Cards != null && gameState.player2Cards != null)
                    {
                        gameDataProvider.UpdateGameData(new OnlineGameDataProvider.OnlineGameDataWithCards
                        {
                            gameId = gameDataProvider.GameId,
                            playerId = gameDataProvider.MyPlayerId,
                            opponentId = gameDataProvider.OpponentId,
                            isPlayer1 = gameDataProvider.IsPlayer1,
                            roomCode = gameDataProvider.RoomCode,
                            player1Cards = gameState.player1Cards,
                            player2Cards = gameState.player2Cards,
                            player1CardValue = gameDataProvider.GameData?.player1CardValue,
                            player2CardValue = gameDataProvider.GameData?.player2CardValue,
                            player1Life = gameState.player1Life,
                            player2Life = gameState.player2Life,
                            player1UsedSkills = p1,
                            player2UsedSkills = p2,
                            currentDealer = gameDataProvider.GameData?.currentDealer ?? "P1"
                        });
                        
                        Debug.Log($"[PhaseManager] Updated hand cards from GameState - Player1: {string.Join(",", gameState.player1Cards)}, Player2: {string.Join(",", gameState.player2Cards)}");
                    }
                }

                HandlePhaseChange(gameState.gamePhase);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhaseManager] Error parsing game state: {e.Message}");
        }
    }
    
    /// <summary>
    /// ゲーム状態エラー時の処理
    /// </summary>
    private void OnGameStateError(string error)
    {
        Debug.LogError($"[PhaseManager] Game state error: {error}");
    }
    
    /// <summary>
    /// フェーズ変更を処理
    /// </summary>
    public void HandlePhaseChange(string newPhase)
    {
        // フェーズテキストを更新
        if (panelManager != null)
        {
            panelManager.UpdatePhaseText(newPhase);
        }
        
        // 現在のフェーズを取得
        string currentPhase = GetCurrentPhaseString();
        
        // 同じフェーズの場合はスキップ
        if (currentPhase == newPhase)
        {
            Debug.Log($"[PhaseManager] Phase {newPhase} already active, skipping");
            return;
        }
        
        Debug.Log($"[PhaseManager] Processing phase change from {currentPhase} to {newPhase}");
        
        // フェーズ遷移処理
        switch (newPhase)
        {
            case "set_phase":
                TransitionToSetPhase();
                break;
                
            case "card_placement":
                HandlePhaseChange("set_phase"); // set_phaseに統合
                break;
                
            case "betting":
                TransitionToBettingPhase();
                break;
                
            case "reveal":
                TransitionToRevealPhase();
                break;
                
            default:
                Debug.LogWarning($"[PhaseManager] Unknown game phase: {newPhase}");
                break;
        }
        
        // イベント発火
        OnPhaseChanged?.Invoke(newPhase);
    }
    
    /// <summary>
    /// SetPhaseに遷移
    /// </summary>
    private void TransitionToSetPhase()
    {
        if (isSetPhaseActive) return;
        
        Debug.Log("[PhaseManager] Transitioning to Set Phase");
        
        isSetPhaseActive = true;
        isRevealPhaseActive = false;
        isBettingPhaseActive = false;
        isSetCompletePhaseActive = false;
        
        if (panelManager != null)
        {
            panelManager.ShowStartPhasePanel("Set Phase", "カードをSetZoneにセットしてください");
        }
        
        OnSetPhaseStarted?.Invoke();
    }
    
    /// <summary>
    /// BettingPhaseに遷移
    /// </summary>
    private void TransitionToBettingPhase()
    {
        if (isBettingPhaseActive) return;
        
        Debug.Log("[PhaseManager] Transitioning to Betting Phase");
        
        isBettingPhaseActive = true;
        isSetPhaseActive = false;
        isSetCompletePhaseActive = false;
        
        if (panelManager != null)
        {
            panelManager.ShowBettingPhasePanel();
        }
        
        OnBettingPhaseStarted?.Invoke();
    }
    
    /// <summary>
    /// RevealPhaseに遷移
    /// </summary>
    private void TransitionToRevealPhase()
    {
        if (isRevealPhaseActive) return;
        
        Debug.Log("[PhaseManager] Transitioning to Reveal Phase");
        
        isRevealPhaseActive = true;
        isSetPhaseActive = false;
        isBettingPhaseActive = false;
        isSetCompletePhaseActive = false;
        
        if (panelManager != null)
        {
            panelManager.ShowRevealPhasePanel();
        }
        
        OnRevealPhaseStarted?.Invoke();
    }
    
    /// <summary>
    /// SetCompletePhaseを開始
    /// </summary>
    public void StartSetCompletePhase()
    {
        Debug.Log("[PhaseManager] Starting Set Complete Phase");
        
        isSetCompletePhaseActive = true;
        isSetPhaseActive = false;
        isBettingPhaseActive = false;
    }
    
    /// <summary>
    /// SetCompletePhaseを終了
    /// </summary>
    public void EndSetCompletePhase()
    {
        Debug.Log("[PhaseManager] Ending Set Complete Phase");
        isSetCompletePhaseActive = false;
    }
    
    /// <summary>
    /// 現在のフェーズを文字列で取得
    /// </summary>
    private string GetCurrentPhaseString()
    {
        if (isSetPhaseActive) return "set_phase";
        if (isBettingPhaseActive) return "betting";
        if (isRevealPhaseActive) return "reveal";
        if (isSetCompletePhaseActive) return "set_complete";
        return "unknown";
    }
    
    [Serializable]
    private class GameStateResponse
    {
        public string gameId;
        public string gamePhase;
        public string[] player1UsedSkills;
        public string[] player2UsedSkills;
        public int[] player1Cards;
        public int[] player2Cards;
        public int player1Life;
        public int player2Life;
    }
}

