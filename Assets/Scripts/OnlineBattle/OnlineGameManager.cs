using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using OnePoker.Network;

public class OnlineGameManager : MonoBehaviour
{
    public enum SkillType
    {
        Scan,
        Change,
        Obstruct,
        FakeOut,
        Copy,
        None
    }
    private OnlineMatchManager matchManager;
    private OnlineResultViewManager resultViewManager;
    private OnlinePanelManager panelManager;
    private OnlineSkillManager skillManager;
    private OnlineHandManager handManager;

    // プレイヤーの手札・相手の手札
    private int[] myHand;
    private int[] opponentHand;

    // プレイヤー情報
    private bool isPlayer1;
    private string playerId;
    private string opponentId;

    // ライフ
    public TextMeshProUGUI playerLifeText;
    public TextMeshProUGUI opponentLifeText;

    // ゲームフェーズ管理
    private bool isSetPhaseActive = false;
    private bool canSetCard = false;
    private string currentGameId = "";
    private string currentPlayerId = "";
    private Coroutine gamePhaseMonitorCoroutine;
    private OnlineGameDataWithCards gameData; // ゲームデータを保存

    void Start()
    {
        Debug.Log("OnlineGameManager.Start() called");
        
        // 各マネージャの取得
        matchManager = FindObjectOfType<OnlineMatchManager>();
        resultViewManager = FindObjectOfType<OnlineResultViewManager>();
        panelManager = FindObjectOfType<OnlinePanelManager>();
        skillManager = FindObjectOfType<OnlineSkillManager>();
        handManager = FindObjectOfType<OnlineHandManager>();

        Debug.Log($"Managers found - matchManager: {matchManager != null}, resultViewManager: {resultViewManager != null}, panelManager: {panelManager != null}, skillManager: {skillManager != null}, handManager: {handManager != null}");

        // gameDataをクラスフィールドとして初期化
        gameData = null;

        // OnlineGameDataから手札・プレイヤー情報を取得
        string gameDataJson = PlayerPrefs.GetString("OnlineGameData", "");
        Debug.Log($"OnlineGameData from PlayerPrefs: {gameDataJson}");
        
        if (!string.IsNullOrEmpty(gameDataJson))
        {
            this.gameData = JsonUtility.FromJson<OnlineGameDataWithCards>(gameDataJson);
            Debug.Log($"Parsed gameData: {gameData != null}");
            
            if (gameData != null)
            {
                Debug.Log($"GameData - isPlayer1: {gameData.isPlayer1}, playerId: {gameData.playerId}, opponentId: {gameData.opponentId}");
                Debug.Log($"GameData - player1Cards: {(gameData.player1Cards != null ? string.Join(",", gameData.player1Cards) : "null")}");
                Debug.Log($"GameData - player2Cards: {(gameData.player2Cards != null ? string.Join(",", gameData.player2Cards) : "null")}");
                
                if (handManager != null)
                {
                    isPlayer1 = gameData.isPlayer1;
                    playerId = gameData.playerId;
                    opponentId = gameData.opponentId;

                    // 手札をセット
                    myHand = isPlayer1 ? gameData.player1Cards : gameData.player2Cards;
                    opponentHand = isPlayer1 ? gameData.player2Cards : gameData.player1Cards;

                    Debug.Log($"Setting hands - myHand: {(myHand != null ? string.Join(",", myHand) : "null")}, opponentHand: {(opponentHand != null ? string.Join(",", opponentHand) : "null")}");

                    handManager.SetPlayerHand(myHand);
                    handManager.SetOpponentHand(opponentHand);
                }
                else
                {
                    Debug.LogError("handManager is null!");
                }
            }
            else
            {
                Debug.LogError("Failed to parse OnlineGameData from JSON");
            }
        }
        else
        {
            Debug.LogWarning("OnlineGameData is empty in PlayerPrefs");
        }

        // ライフUI初期化
        UpdateLifeUI();
        Debug.Log("OnlineGameManager.Start() completed");

        // マッチング完了パネル表示後にゲームフェーズ監視を開始
        if (panelManager != null && gameData != null)
        {
            string playerName = gameData.playerId;
            string opponentName = gameData.opponentId;
            Debug.Log($"[MatchStartPanel] playerName: {playerName}, opponentName: {opponentName}");
            
            // マッチング完了パネルを表示し、3秒後にゲームフェーズ監視を開始
            StartCoroutine(ShowMatchStartPanelAndStartMonitoring(playerName, opponentName, 3f));
        }
        else if (gameData != null)
        {
            // panelManagerがnullの場合は即座にゲームフェーズ監視を開始
            currentGameId = gameData.gameId;
            currentPlayerId = gameData.playerId;
            Debug.Log($"OnlineGameManager - Starting game phase monitoring for gameId: {currentGameId}, playerId: {currentPlayerId}");
            StartGamePhaseMonitoring();
        }
        else
        {
            Debug.LogError("OnlineGameManager - gameData is null, cannot start phase monitoring");
        }
    }

    // ライフUIの更新
    public void UpdateLifeUI()
    {
        if (playerLifeText != null)
            playerLifeText.text = $"Life: {matchManager.PlayerLife}";
        if (opponentLifeText != null)
            opponentLifeText.text = $"Life: {matchManager.OpponentLife}";
    }

    // マッチング完了パネル表示後にゲームフェーズ監視を開始するコルーチン
    private IEnumerator ShowMatchStartPanelAndStartMonitoring(string playerName, string opponentName, float duration)
    {
        Debug.Log($"OnlineGameManager - Showing match start panel for {duration} seconds");
        
        // マッチング完了パネルを表示
        panelManager.ShowMatchStartPanel(playerName, opponentName, duration);
        
        // パネル表示時間分待機
        yield return new WaitForSeconds(duration);
        
        Debug.Log("OnlineGameManager - Match start panel duration completed, setting phase transition time");
        
        // フェーズ移行時間を設定してからゲームフェーズ監視を開始
        if (gameData != null)
        {
            currentGameId = gameData.gameId;
            currentPlayerId = gameData.playerId;
            
            // フェーズ移行時間を設定
            yield return StartCoroutine(SetPhaseTransitionTime());
            
            Debug.Log($"OnlineGameManager - Starting game phase monitoring for gameId: {currentGameId}, playerId: {currentPlayerId}");
            StartGamePhaseMonitoring();
        }
        else
        {
            Debug.LogError("OnlineGameManager - gameData is null, cannot start phase monitoring");
        }
    }

    // フェーズ移行時間を設定するコルーチン
    private IEnumerator SetPhaseTransitionTime()
    {
        Debug.Log($"OnlineGameManager - Setting phase transition time for gameId: {currentGameId}, playerId: {currentPlayerId}");
        
        // set-phase-transition APIを呼び出す
        HttpManager.Instance.SetPhaseTransitionTime(currentGameId, currentPlayerId, 3, OnPhaseTransitionSet, OnPhaseTransitionError);
        
        // 非同期処理の完了を待つ
        yield return new WaitForSeconds(0.1f);
    }

    // フェーズ移行時間設定成功時のコールバック
    private void OnPhaseTransitionSet(string response)
    {
        Debug.Log($"OnlineGameManager - Phase transition time set successfully: {response}");
    }

    // フェーズ移行時間設定エラー時のコールバック
    private void OnPhaseTransitionError(string error)
    {
        Debug.LogError($"OnlineGameManager - Failed to set phase transition time: {error}");
    }

    // ゲームフェーズ監視を開始
    private void StartGamePhaseMonitoring()
    {
        if (gamePhaseMonitorCoroutine != null)
        {
            StopCoroutine(gamePhaseMonitorCoroutine);
        }
        gamePhaseMonitorCoroutine = StartCoroutine(MonitorGamePhase());
    }

    // ゲームフェーズを定期的に監視
    private IEnumerator MonitorGamePhase()
    {
        Debug.Log("OnlineGameManager - Starting game phase monitoring coroutine");
        
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // 0.1秒ごとに状態確認（短縮）
            
            if (!string.IsNullOrEmpty(currentGameId) && !string.IsNullOrEmpty(currentPlayerId))
            {
                StartCoroutine(CheckGamePhase());
            }
            else
            {
                Debug.LogWarning("OnlineGameManager - gameId or playerId is empty, skipping phase check");
            }
        }
    }

    // ゲームフェーズを確認
    private IEnumerator CheckGamePhase()
    {
        try
        {
            Debug.Log($"OnlineGameManager - Checking game phase for gameId: {currentGameId}, playerId: {currentPlayerId}");
            
            // HttpManagerを使用してget-game-state APIを呼び出す
            HttpManager.Instance.GetGameState(currentGameId, currentPlayerId, OnGameStateReceived, OnGameStateError);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OnlineGameManager - Error checking game phase: {e.Message}");
        }
        
        yield return null; // 非同期処理のため、yield return null
    }

    // ゲーム状態を受信した時の処理
    private void OnGameStateReceived(string response)
    {
        try
        {
            Debug.Log($"OnlineGameManager - Received game state: {response}");
            
            // JSONをパース
            var gameState = JsonUtility.FromJson<GameStateResponse>(response);
            
            if (gameState != null)
            {
                HandleGamePhaseChange(gameState.gamePhase);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OnlineGameManager - Error parsing game state: {e.Message}");
        }
    }

    // ゲーム状態エラー時の処理
    private void OnGameStateError(string error)
    {
        Debug.LogError($"OnlineGameManager - Game state error: {error}");
    }

    // ゲームフェーズ変更を処理
    private void HandleGamePhaseChange(string newPhase)
    {
        Debug.Log($"OnlineGameManager - Handling phase change to: {newPhase}");
        Debug.Log($"OnlineGameManager - Current state: isSetPhaseActive={isSetPhaseActive}, canSetCard={canSetCard}");
        
        switch (newPhase)
        {
            case "set_phase":
                Debug.Log("OnlineGameManager - Processing set_phase case");
                if (!isSetPhaseActive)
                {
                    isSetPhaseActive = true;
                    canSetCard = false;
                    Debug.Log("OnlineGameManager - Activating set phase");
                    if (panelManager != null)
                    {
                        panelManager.ShowStartPhasePanel("Set Phase", "カードをSetZoneにセットしてください");
                        Debug.Log("OnlineGameManager - Set Phase started");
                        Debug.Log("OnlineGameManager - Lambda function: set_phase phase activated");
                    }
                    else
                    {
                        Debug.LogError("OnlineGameManager - panelManager is null!");
                    }
                }
                else
                {
                    Debug.Log("OnlineGameManager - Set phase already active, skipping");
                }
                break;
                
            case "card_placement":
                if (isSetPhaseActive)
                {
                    isSetPhaseActive = false;
                    canSetCard = true;
                    if (panelManager != null)
                    {
                        panelManager.HideStartPhasePanel();
                        Debug.Log("OnlineGameManager - Set Phase ended, card placement enabled");
                        Debug.Log("OnlineGameManager - Lambda function: card_placement phase activated");
                    }
                }
                break;

            case "betting":
                if (panelManager != null)
                {
                    panelManager.ShowBettingPhasePanel();
                    Debug.Log("OnlineGameManager - Betting Phase started");
                    Debug.Log("OnlineGameManager - Lambda function: betting phase activated");
                }
                break;

            case "reveal":
                if (panelManager != null)
                {
                    panelManager.ShowRevealPhasePanel();
                    Debug.Log("OnlineGameManager - Reveal Phase started");
                    Debug.Log("OnlineGameManager - Lambda function: reveal phase activated");
                }
                break;
                
            default:
                Debug.Log($"OnlineGameManager - Unknown game phase: {newPhase}");
                break;
        }
    }

    // カードセット可能かどうかを取得
    public bool CanSetCard()
    {
        return canSetCard;
    }

    // カード配置・スキル・ベットなどのイベントは
    // サーバー同期用のメソッドをここに追加していく
    // 例: public void PlaceCard(int cardId) { ... }
    // 例: public void UseSkill(SkillType skill) { ... }

    [System.Serializable]
    private class OnlineGameDataWithCards
    {
        public string roomCode;
        public string playerId;
        public string opponentId;
        public bool isPlayer1;
        public string gameId;
        public int[] player1Cards;
        public int[] player2Cards;
    }

    [System.Serializable]
    private class GameStateResponse
    {
        public string gameId;
        public string gamePhase;
        public long? phaseTransitionTime; // nullableに変更
        public string currentTurn;
        public int player1Life;
        public int player2Life;
        public int currentBet;
        public bool player1CardPlaced;
        public bool player2CardPlaced;
        public int[] myCards;
        public int myLife;
        public int myBetAmount;
        public bool myCardPlaced;
        public bool opponentCardPlaced;
        public int? opponentPlacedCardId;
        public string updatedAt;
    }

    
    public void ConfirmPlacement() { /* TODO: 実装 */ }
    public void CancelPlacement() { /* TODO: 実装 */ }
    public void PlaceBet(int amount) { /* TODO: 実装 */ }
    public int CurrentBetAmount { get; private set; }
    public void SetOpponentCalled(bool called) { /* TODO: 実装 */ }
    public void RevealCards() { /* TODO: 実装 */ }
    public CardDisplay SetPlayerCard { get; }
    public CardDisplay SetOpponentCard { get; }
    public bool PlayerCanUseScanSkill { get; }
    public bool PlayerCanUseChangeSkill { get; }
    public bool PlayerCanUseObstructSkill { get; }
    public bool PlayerCanUseFakeOutSkill { get; }
    public bool PlayerCanUseCopySkill { get; }
}
