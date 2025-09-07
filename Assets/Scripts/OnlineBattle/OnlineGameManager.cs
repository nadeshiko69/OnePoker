using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System;
using OnePoker.Network;
using UnityEngine.Networking; // Added for UnityWebRequest

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
    private bool isRevealPhaseActive = false;
    private bool isSetCompletePhaseActive = false;
    private bool isBettingPhaseActive = false;
    private bool canSetCard = false;
    private string currentGameId = "";
    private string currentPlayerId = "";
    private Coroutine gamePhaseMonitorCoroutine;
    private OnlineGameDataWithCards gameData; // ゲームデータを保存

    // カード配置管理
    private CardDisplay currentCard;
    private OnlineDropZone currentZone;
    private CardDisplay setPlayerCard;
    public CardDisplay SetPlayerCard => setPlayerCard;

    // ベット関連のフィールド
    private int currentBetValue = 1; // 現在のベット値（最小1）
    private int minimumBetValue = 1; // 最小ベット値（相手のレイズに応じて変更）
    private bool isMyTurn = false; // 自分のターンかどうか
    private bool hasOpponentActed = false; // 相手がアクション済みかどうか

    // 親子システム関連のフィールド
    private bool isParent = false; // 自分が親かどうか
    private int currentRound = 0; // 現在のラウンド（1-15）
    private int parentChangeRound = 3; // 親が交代するラウンド数
    private bool isParentTurn = false; // 親のターンかどうか
    private bool waitingForParentAction = false; // 親のアクション待ちかどうか
    private bool waitingForChildAction = false; // 子のアクション待ちかどうか
    
    // 親のBet完了監視関連のフィールド
    private Coroutine parentBetMonitorCoroutine;
    private string notifyBetCompleteUrl = "https://your-api-gateway-url/dev/notify-bet-complete";
    private string checkParentBetStatusUrl = "https://your-api-gateway-url/dev/check-parent-bet-status";

    // プレハブ参照
    [Header("Prefabs")]
    public GameObject cardPrefab; // カードプレハブ（Inspectorで設定）

    // ベット値の管理
    public int CurrentBetValue => currentBetValue;
    public int MinimumBetValue => minimumBetValue;
    public bool IsMyTurn => isMyTurn;
    public bool IsParent => isParent;
    public bool IsParentTurn => isParentTurn;

    void Start()
    {
        Debug.Log("[START_DEBUG] OnlineGameManager.Start() called");
        
        // 各マネージャーの初期化
        InitializeManagers();        
        // gameDataの初期化と読み込み
        InitializeGameData();
        
        // gameDataが正常に読み込まれた場合のみ処理を続行
        if (gameData != null)
        {
            // 手札の設定とHIGH/LOW表示の更新
            SetupHandsAndHighLowDisplay();           
            // 初期設定の完了処理
            CompleteInitialization();           
            // 初期ゲームフェーズ監視の開始
            InitializeGamePhaseMonitoring();
        }
        else
        {
            Debug.LogError("gameData is null");
        }
    }


    // 親子システムの初期化
    private void InitializeParentChildSystem()
    {
        // CreateRoomから対戦に遷移する方を先行の親とする
        // gameData.isPlayer1がtrueの場合、そのプレイヤーが先行の親
        isParent = gameData.isPlayer1;
        currentRound = 1;
        
        Debug.Log($"[CHILD_DEBUG] Parent-Child system initialized. isParent: {isParent}, currentRound: {currentRound}");
        Debug.Log($"[CHILD_DEBUG] gameData.isPlayer1: {gameData.isPlayer1}");
    }

    // 親子の交代判定
    private void CheckParentChange()
    {
        if (currentRound % parentChangeRound == 0)
        {
            isParent = !isParent;
            Debug.Log($"Parent changed at round {currentRound}. New parent: {isParent}");
            // 親子の表示を更新
            if (panelManager != null)
            {
                panelManager.UpdatePlayerRoleDisplay(isParent);
            }
        }
    }

    // ラウンド終了時の処理
    private void OnRoundEnd()
    {
        Debug.Log($"Round {currentRound} ended");
        
        // 親子交代をチェック
        CheckParentChange();
        
        // 次のラウンドに進む
        currentRound++;
        Debug.Log($"Moving to round {currentRound}");
        
        // 新しいラウンドの親子システムを初期化
        InitializeParentChildSystem();
    }

    // 親のターンを開始
    private void StartParentTurn()
    {
        if (isParent)
        {
            isParentTurn = true;
            isMyTurn = true;
            waitingForParentAction = false;
            waitingForChildAction = false;
            
            Debug.Log("Parent turn started. Parent can now bet.");
            
            if (panelManager != null)
            {
                panelManager.ShowParentTurnPanel();
                panelManager.SetBettingButtonInteractable(true);
            }
        }
        else
        {
            isParentTurn = false;
            isMyTurn = false;
            waitingForParentAction = true;
            waitingForChildAction = false;
            
            Debug.Log("Child waiting for parent action.");
            
            if (panelManager != null)
            {
                panelManager.ShowWaitingForParentPanel();
                panelManager.ShowParentBettingPanel(); // 親プレイヤーBet中パネルを表示
                panelManager.SetBettingButtonInteractable(false);
            }
        }
    }

    // 子のターンを開始
    private void StartChildTurn()
    {
        Debug.Log($"[CHILD_DEBUG] StartChildTurn called - isParent: {isParent}");
        
        if (!isParent)
        {
            Debug.Log("[CHILD_DEBUG] Child player detected, setting up child turn");
            
            isParentTurn = false;
            isMyTurn = true;
            waitingForParentAction = false;
            waitingForChildAction = false;
            
            Debug.Log("[CHILD_DEBUG] Child turn started. Child can now bet.");
            
            if (panelManager != null)
            {
                Debug.Log("[CHILD_DEBUG] panelManager found, updating UI");
                
                panelManager.HideWaitingForParentPanel();
                panelManager.HideParentBettingPanel(); // 親プレイヤーBet中パネルを非表示
                panelManager.SetBettingButtonInteractable(true);
                
                // 子プレイヤーの場合、betAmountTextを非表示にする
                if (panelManager.betAmountText != null)
                {
                    panelManager.betAmountText.gameObject.SetActive(false);
                    Debug.Log("[CHILD_DEBUG] Child turn: betAmountText hidden");
                }
                
                Debug.Log("[CHILD_DEBUG] Child UI update completed");
            }
            else
            {
                Debug.LogError("[CHILD_DEBUG] panelManager is null!");
            }
        }
        else
        {
            Debug.Log("[CHILD_DEBUG] Parent player detected, setting up parent waiting state");
            
            isParentTurn = true;
            isMyTurn = false;
            waitingForParentAction = false;
            waitingForChildAction = true;
            
            Debug.Log("[CHILD_DEBUG] Parent waiting for child action.");
            
            if (panelManager != null)
            {
                panelManager.ShowWaitingForChildPanel();
                panelManager.SetBettingButtonInteractable(false);
            }
        }
        
        // 子プレイヤーの場合、親のBet完了を監視開始
        if (!isParent)
        {
            Debug.Log("[CHILD_DEBUG] Starting parent bet monitoring for child player");
            Debug.Log($"[CHILD_DEBUG] Child monitoring state - waitingForParentAction: {waitingForParentAction}");
            StartParentBetMonitoring();
        }
        
        Debug.Log("[CHILD_DEBUG] StartChildTurn completed");
    }
    
    // 親のBet完了監視を開始
    private void StartParentBetMonitoring()
    {
        Debug.Log($"[CHILD_DEBUG] StartParentBetMonitoring called - isParent: {isParent}");
        
        if (isParent)
        {
            Debug.Log("[CHILD_DEBUG] Parent player, skipping bet monitoring");
            return;
        }
        
        Debug.Log("[CHILD_DEBUG] Starting parent bet monitoring for child player");
        Debug.Log($"[CHILD_DEBUG] Current state - waitingForParentAction: {waitingForParentAction}");
        
        // 既存の監視を停止
        if (parentBetMonitorCoroutine != null)
        {
            Debug.Log("[CHILD_DEBUG] Stopping existing parent bet monitoring");
            StopCoroutine(parentBetMonitorCoroutine);
        }
        
        // 新しい監視を開始
        Debug.Log("[CHILD_DEBUG] Starting new parent bet monitoring coroutine");
        parentBetMonitorCoroutine = StartCoroutine(MonitorParentBetStatus());
    }
    
    // 親のBet完了状態を監視
    private IEnumerator MonitorParentBetStatus()
    {
        Debug.Log("[CHILD_DEBUG] Parent bet monitoring started");
        Debug.Log($"[CHILD_DEBUG] Initial state - waitingForParentAction: {waitingForParentAction}, isParent: {isParent}");
        
        while (waitingForParentAction)
        {
            Debug.Log("[CHILD_DEBUG] Checking parent bet status...");
            yield return new WaitForSeconds(1f); // 1秒ごとに確認
            
            // 親のBet完了状態を確認
            yield return StartCoroutine(CheckParentBetStatus());
        }
        
        Debug.Log("[CHILD_DEBUG] Parent bet monitoring stopped");
    }
    
    // 親のBet完了状態を確認
    private IEnumerator CheckParentBetStatus()
    {
        Debug.Log("[CHILD_DEBUG] CheckParentBetStatus called");
        
        if (gameData == null)
        {
            Debug.LogWarning("[CHILD_DEBUG] gameData is null, cannot check parent bet status");
            yield break;
        }
        
        Debug.Log($"[CHILD_DEBUG] Calling GetGameState - gameId: {gameData.gameId}, playerId: {gameData.playerId}");
        
        // DynamoDBのGamestatesテーブルからPlayer1BetAmountを確認
        HttpManager.Instance.GetGameState(
            gameData.gameId,
            gameData.playerId,
            OnParentBetStatusReceived,
            OnParentBetStatusError
        );
        
        yield return null;
    }
    
    // 親のBet状態を受信した時の処理
    private void OnParentBetStatusReceived(string response)
    {
        try
        {
            Debug.Log($"[CHILD_DEBUG] Parent bet status received: {response}");
            
            // JSONをパース
            var gameState = JsonUtility.FromJson<GameStateResponse>(response);
            
            Debug.Log($"[CHILD_DEBUG] Parsed gameState: {gameState != null}");
            if (gameState != null)
            {
                Debug.Log($"[CHILD_DEBUG] player1BetAmount: {gameState.player1BetAmount}");
                Debug.Log($"[CHILD_DEBUG] waitingForParentAction: {waitingForParentAction}");
                Debug.Log($"[CHILD_DEBUG] isParent: {isParent}");
            }
            
            if (gameState != null && gameState.player1BetAmount > 0)
            {
                Debug.Log($"[CHILD_DEBUG] Parent bet amount found: {gameState.player1BetAmount}");
                Debug.Log($"[CHILD_DEBUG] Calling HandleParentBetComplete");
                
                // 親のBet完了を処理
                HandleParentBetComplete("call", gameState.player1BetAmount);
                
                Debug.Log($"[CHILD_DEBUG] HandleParentBetComplete completed");
                
                // 監視を停止
                waitingForParentAction = false;
                if (parentBetMonitorCoroutine != null)
                {
                    StopCoroutine(parentBetMonitorCoroutine);
                    parentBetMonitorCoroutine = null;
                    Debug.Log($"[CHILD_DEBUG] Parent bet monitoring stopped");
                }
            }
            else
            {
                Debug.Log($"[CHILD_DEBUG] Parent bet amount not found or zero, continuing to wait");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CHILD_DEBUG] Error parsing parent bet status: {e.Message}");
        }
    }
    
    // 親のBet状態エラー時の処理
    private void OnParentBetStatusError(string error)
    {
        Debug.LogError($"Parent bet status error: {error}");
    }
    
    // 親のBet完了を処理
    private void HandleParentBetComplete(string betAction, int betAmount)
    {
        Debug.Log($"[CHILD_DEBUG] Handling parent bet completion: {betAction}, amount: {betAmount}");
        Debug.Log($"[CHILD_DEBUG] Current state - isParent: {isParent}, waitingForParentAction: {waitingForParentAction}");
        
        switch (betAction)
        {
            case "call":
                // 親がコールした場合、子のターンを開始
                Debug.Log("[CHILD_DEBUG] Parent called, starting child turn");
                StartChildTurn();
                break;
                
            case "raise":
                // 親がレイズした場合、最低ベット額を更新して子のターンを開始
                Debug.Log($"[CHILD_DEBUG] Parent raised to {betAmount}, updating minimum bet and starting child turn");
                minimumBetValue = betAmount;
                currentBetValue = betAmount;
                StartChildTurn();
                UpdateBetUI();
                break;
                
            case "drop":
                // 親がドロップした場合、OpenPhaseに移行
                Debug.Log("[CHILD_DEBUG] Parent dropped, transitioning to OpenPhase");
                HandleGamePhaseChange("reveal");
                break;
        }
        
        Debug.Log($"[CHILD_DEBUG] HandleParentBetComplete completed for action: {betAction}");
    }

    // ベット値の増減
    public void IncreaseBetValue()
    {
        if (currentBetValue < 10) // 最大10まで
        {
            currentBetValue++;
            Debug.Log($"Bet value increased to: {currentBetValue}");
            UpdateBetUI();
        }
    }

    public void DecreaseBetValue()
    {
        if (currentBetValue > minimumBetValue)
        {
            currentBetValue--;
            Debug.Log($"Bet value decreased to: {currentBetValue}");
            UpdateBetUI();
        }
    }

    // ベットUIの更新
    private void UpdateBetUI()
    {
        if (panelManager != null)
        {
            // Call/Raiseボタンのテキスト更新
            panelManager.UpdateCallButtonText(currentBetValue);
            
            // ベット額表示の更新
            panelManager.UpdateBetAmountDisplay(currentBetValue);
            
            // ボタンの有効/無効状態更新
            panelManager.SetBettingButtonInteractable(true);
        }
    }

    // ベットアクションの実行
    public void ExecuteBetAction(string actionType)
    {
        Debug.Log($"ExecuteBetAction called with actionType: {actionType}, betValue: {currentBetValue}");
        Debug.Log($"Current state: isParent={isParent}, isParentTurn={isParentTurn}, isMyTurn={isMyTurn}");
        
        if (!isBettingPhaseActive)
        {
            Debug.LogWarning("Betting phase not active, ignoring bet action");
            return;
        }

        // 自分のターンでない場合は処理しない
        if (!isMyTurn)
        {
            Debug.LogWarning("Not my turn, ignoring bet action");
            return;
        }

        // 親子システムに基づいてアクションを処理
        if (isParent)
        {
            HandleParentAction(actionType);
        }
        else
        {
            HandleChildAction(actionType);
        }
    }

    // 親のアクション処理
    private void HandleParentAction(string actionType)
    {
        Debug.Log($"Handling parent action: {actionType}");
        
        switch (actionType)
        {
            case "call":
                // 親がコールした場合、DynamoDBのGamestatesテーブルにBet値を登録
                Debug.Log($"Parent called with bet value: {currentBetValue}");
                UpdatePlayerBetAmountInGameState(currentBetValue);
                StartChildTurn();
                break;
                
            case "raise":
                // 親がレイズした場合、最低ベット額を更新してDynamoDBにBet値を登録
                Debug.Log($"Parent raised to {currentBetValue}, updating minimum bet and starting child turn");
                minimumBetValue = currentBetValue;
                UpdatePlayerBetAmountInGameState(currentBetValue);
                StartChildTurn();
                break;
                
            case "drop":
                // 親がドロップした場合、OpenPhaseに移行
                Debug.Log("Parent dropped, transitioning to OpenPhase");
                HandleGamePhaseChange("reveal");
                break;
        }
        
        // 親のBet完了をDynamo側に通知
        NotifyParentBetComplete(actionType, currentBetValue);
        
        // AWSにベットアクションを送信
        SendBetActionToAWS(actionType, currentBetValue);
    }

    // 子のアクション処理
    private void HandleChildAction(string actionType)
    {
        Debug.Log($"Handling child action: {actionType}");
        
        switch (actionType)
        {
            case "call":
            case "drop":
                // 子がコールまたはドロップした場合、OpenPhaseに移行
                Debug.Log($"Child {actionType}, transitioning to OpenPhase");
                HandleGamePhaseChange("reveal");
                break;
                
            case "raise":
                // 子がレイズした場合、親のターンに戻る
                Debug.Log($"Child raised to {currentBetValue}, returning to parent turn");
                minimumBetValue = currentBetValue;
                StartParentTurn();
                break;
        }

        // AWSにベットアクションを送信
        SendBetActionToAWS(actionType, currentBetValue);
    }

    // Call/Raiseアクション
    public void CallOrRaise()
    {
        string actionType = currentBetValue == 1 ? "call" : "raise";
        ExecuteBetAction(actionType);
    }

    // Dropアクション
    public void Drop()
    {
        ExecuteBetAction("drop");
    }

    // AWSにベットアクションを送信
    private void SendBetActionToAWS(string actionType, int betValue)
    {
        Debug.Log($"Sending bet action to AWS: {actionType}, betValue: {betValue}");
        
        // HttpManagerを使用してベットアクションを送信
        HttpManager.Instance.SendBetAction(
            currentGameId,
            currentPlayerId,
            actionType,
            betValue,
            OnBetActionSuccess,
            OnBetActionError
        );
    }
    
    // DynamoDBのGamestatesテーブルにPlayer1BetAmountを更新
    private void UpdatePlayerBetAmountInGameState(int betAmount)
    {
        if (gameData == null)
        {
            Debug.LogError("gameData is null, cannot update bet amount in game state");
            return;
        }
        
        Debug.Log($"Updating Player1BetAmount in GameStates table: {betAmount}");
        
        // update-game-state APIを呼び出してPlayer1BetAmountを更新
        HttpManager.Instance.UpdatePlayerBetAmount(
            gameData.gameId,
            gameData.playerId,
            betAmount,
            OnBetAmountUpdateSuccess,
            OnBetAmountUpdateError
        );
    }
    
    // 親のBet完了をDynamo側に通知
    private void NotifyParentBetComplete(string actionType, int betValue)
    {
        if (!isParent)
        {
            Debug.Log("Not parent, skipping bet completion notification");
            return;
        }
        
        Debug.Log($"Notifying parent bet completion: {actionType}, betValue: {betValue}");
        
        // 通知用のJSONデータを作成
        var notificationData = new ParentBetCompleteRequest
        {
            roomCode = gameData.roomCode,
            playerId = playerId,
            betAction = actionType,
            betAmount = betValue
        };
        
        string jsonBody = JsonUtility.ToJson(notificationData);
        Debug.Log($"Parent bet completion notification JSON: {jsonBody}");
        
        // HTTP POSTリクエストを送信
        StartCoroutine(SendParentBetCompleteNotification(jsonBody));
    }
    
    // 親のBet完了通知を送信
    private IEnumerator SendParentBetCompleteNotification(string jsonBody)
    {
        var request = new UnityWebRequest(notifyBetCompleteUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Parent bet completion notification successful: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"Parent bet completion notification failed: {request.error}");
        }
    }

    // ベットアクション成功時の処理
    private void OnBetActionSuccess(string response)
    {
        Debug.Log($"Bet action successful: {response}");
        
        // レスポンスをパース
        var betResponse = JsonUtility.FromJson<BetActionResponse>(response);
        
        if (betResponse != null)
        {
            // ゲーム状態を更新
            UpdateGameStateFromBetResponse(betResponse);
            
            // UIを更新
            UpdateBettingUI(betResponse);
        }
    }

    // ベットアクションエラー時の処理
    private void OnBetActionError(string error)
    {
        Debug.LogError($"Bet action failed: {error}");
        // エラー時の処理（例：ボタンを再度有効化）
        if (panelManager != null)
        {
            panelManager.SetBettingButtonInteractable(true);
        }
    }
    
    // Bet金額更新成功時の処理
    private void OnBetAmountUpdateSuccess(string response)
    {
        Debug.Log($"Bet amount updated successfully: {response}");
    }
    
    // Bet金額更新エラー時の処理
    private void OnBetAmountUpdateError(string error)
    {
        Debug.LogError($"Failed to update bet amount: {error}");
    }

    // ベットレスポンスからゲーム状態を更新
    private void UpdateGameStateFromBetResponse(BetActionResponse response)
    {
        // 相手のアクションに応じて処理
        if (response.opponentAction != null)
        {
            HandleOpponentAction(response.opponentAction);
        }
        
        // フェーズ遷移の確認
        if (response.gamePhase == "reveal")
        {
            Debug.Log("Game phase changed to reveal from bet action");
            HandleGamePhaseChange("reveal");
        }
    }

    // 相手のアクションを処理
    private void HandleOpponentAction(OpponentAction action)
    {
        Debug.Log($"Handling opponent action: {action.actionType}, betValue: {action.betValue}");
        
        switch (action.actionType)
        {
            case "call":
                ShowOpponentCalledPanel();
                break;
            case "raise":
                ShowOpponentRaisedPanel(action.betValue);
                minimumBetValue = action.betValue;
                currentBetValue = action.betValue;
                UpdateBetUI();
                break;
            case "drop":
                ShowOpponentDroppedPanel();
                break;
        }
    }

    // 相手がコールした時のパネル表示
    private void ShowOpponentCalledPanel()
    {
        if (panelManager != null)
        {
            panelManager.ShowOpponentActionPanel("対戦相手がコールしました", 3f);
        }
    }

    // 相手がレイズした時のパネル表示
    private void ShowOpponentRaisedPanel(int betValue)
    {
        if (panelManager != null)
        {
            panelManager.ShowOpponentActionPanel($"対戦相手が{betValue}にレイズしました", 3f);
        }
    }

    // 相手がドロップした時のパネル表示
    private void ShowOpponentDroppedPanel()
    {
        if (panelManager != null)
        {
            panelManager.ShowOpponentActionPanel("対戦相手がドロップしました", 3f);
        }
    }

    // ベットUIの更新
    private void UpdateBettingUI(BetActionResponse response)
    {
        if (panelManager != null)
        {
            // 自分のターンかどうかを更新
            isMyTurn = response.isMyTurn;
            
            // ボタンの有効/無効状態を更新
            panelManager.SetBettingButtonInteractable(isMyTurn);
        }
    }

    // 各マネージャーの初期化
    private void InitializeManagers()
    {
        Debug.Log("[START_DEBUG] OnlineGameManager.InitializeManagers() called");
        matchManager = FindObjectOfType<OnlineMatchManager>();
        resultViewManager = FindObjectOfType<OnlineResultViewManager>();
        panelManager = FindObjectOfType<OnlinePanelManager>();
        skillManager = FindObjectOfType<OnlineSkillManager>();
        handManager = FindObjectOfType<OnlineHandManager>();
    }

    // gameDataの初期化と読み込み
    private void InitializeGameData()
    {
        Debug.Log("[START_DEBUG] OnlineGameManager.InitializeGameData() called");
        // gameDataをクラスフィールドとして初期化
        gameData = null;
        // Debug.Log($"[START_DEBUG] gameData初期化完了: {gameData == null}");

        // OnlineGameDataから手札・プレイヤー情報を取得
        // Debug.Log($"[START_DEBUG] PlayerPrefsからOnlineGameData読み込み開始");
        string gameDataJson = PlayerPrefs.GetString("OnlineGameData", "");
        Debug.Log($"[START_DEBUG] JSON in PlayerPrefs: {gameDataJson}");
        // Debug.Log($"OnlineGameData from PlayerPrefs: {gameDataJson}");
        
        if (!string.IsNullOrEmpty(gameDataJson))
        {
            this.gameData = JsonUtility.FromJson<OnlineGameDataWithCards>(gameDataJson);
            // Debug.Log($"Parsed gameData: {gameData != null}");
            
            if (gameData != null)
            {
                LogGameDataDetails();
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
    }

    // gameDataの詳細情報をログ出力
    private void LogGameDataDetails()
    {
        Debug.Log($"=== gameData詳細情報 ===");
        Debug.Log($"[GAMEDATA_DEBUG] GameData - isPlayer1: {gameData.isPlayer1}, playerId: {gameData.playerId}, opponentId: {gameData.opponentId}");
        Debug.Log($"[GAMEDATA_DEBUG] GameData - gameId: {gameData.gameId}, roomCode: {gameData.roomCode}");
        Debug.Log($"[GAMEDATA_DEBUG] GameData - player1Cards: {(gameData.player1Cards != null ? string.Join(",", gameData.player1Cards) : "null")}");
        Debug.Log($"[GAMEDATA_DEBUG] GameData - player2Cards: {(gameData.player2Cards != null ? string.Join(",", gameData.player2Cards) : "null")}");
        
        if (gameData.player1Cards != null)
        {
            // Debug.Log($"[GAMEDATA_DEBUG] GameData - player1Cards.Length: {gameData.player1Cards.Length}");
            for (int i = 0; i < gameData.player1Cards.Length; i++)
            {
                Debug.Log($"[GAMEDATA_DEBUG] GameData - player1Cards[{i}]: {gameData.player1Cards[i]}");
            }
        }
        
        if (gameData.player2Cards != null)
        {
            // Debug.Log($"[GAMEDATA_DEBUG] GameData - player2Cards.Length: {gameData.player2Cards.Length}");
            for (int i = 0; i < gameData.player2Cards.Length; i++)
            {
                Debug.Log($"[GAMEDATA_DEBUG] GameData - player2Cards[{i}]: {gameData.player2Cards[i]}");
            }
        }
    }

    // 手札の設定とHIGH/LOW表示の更新
    private void SetupHandsAndHighLowDisplay()
    {
        Debug.Log("[START_DEBUG] OnlineGameManager.SetupHandsAndHighLowDisplay() called");
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
            
            // 手札設定完了後、HIGH/LOW表示を更新
            UpdateHighLowDisplay();
                }
                else
                {
                    Debug.LogError("handManager is null!");
                }
    }

    // HIGH/LOW表示の更新
    private void UpdateHighLowDisplay()
    {
        Debug.Log("[START_DEBUG] OnlineGameManager.UpdateHighLowDisplay() called");
        if (panelManager != null)
        {
            Debug.Log($"自分の手札: {(myHand != null ? string.Join(",", myHand) : "null")}");
            Debug.Log($"相手の手札: {(opponentHand != null ? string.Join(",", opponentHand) : "null")}");
            
            // HIGH/LOW表示を更新
            if (myHand != null && myHand.Length >= 2 && opponentHand != null && opponentHand.Length >= 2)
            {
                int playerCard1 = myHand[0];
                int playerCard2 = myHand[1];
                int opponentCard1 = opponentHand[0];
                int opponentCard2 = opponentHand[1];
                
                Debug.Log($"自分のカード: [{playerCard1}, {playerCard2}]");
                Debug.Log($"相手のカード: [{opponentCard1}, {opponentCard2}]");
                
                panelManager.UpdatePlayerHighLowDisplay(playerCard1, playerCard2);
                panelManager.UpdateOpponentHighLowDisplay(opponentCard1, opponentCard2);
            }
            else
            {
                Debug.LogWarning($"手札が不足しています: myHand.Length={myHand?.Length}, opponentHand.Length={opponentHand?.Length}");
            }
        }
        else
        {
            Debug.LogError($"[START_DEBUG] panelManagerがnullです！");
        }
        }

    // 初期設定の完了処理
    private void CompleteInitialization()
    {
        Debug.Log("[START_DEBUG] OnlineGameManager.CompleteInitialization() called");
        // ライフUI初期化
        UpdateLifeUI();
        // カードセットを有効化
        canSetCard = true;
        // 親子システムの初期化
        InitializeParentChildSystem();
    }

    // 初期ゲームフェーズ監視の開始
    private void InitializeGamePhaseMonitoring()
    {
        // マッチング完了パネル表示後にゲームフェーズ監視を開始
        if (panelManager != null && gameData != null)
        {
            string playerName = gameData.playerId;
            string opponentName = gameData.opponentId;
            Debug.Log($"[START_DEBUG] playerName: {playerName}, opponentName: {opponentName}");
            
            // 初期フェーズテキストを設定
            if (panelManager != null)
            {
                panelManager.UpdatePhaseText("set_phase");
            }
            
            // マッチング完了パネルを表示し、3秒後にゲームフェーズ監視を開始
            StartCoroutine(ShowMatchStartPanelAndStartMonitoring(playerName, opponentName, 3f));
        }
        else if (gameData != null)
        {
            // panelManagerがnullの場合は即座にゲームフェーズ監視を開始
            currentGameId = gameData.gameId;
            currentPlayerId = gameData.playerId;
            Debug.Log($"[START_DEBUG] Starting game phase monitoring for gameId: {currentGameId}, playerId: {currentPlayerId}");
            StartGamePhaseMonitoring();
        }
        else
        {
            Debug.LogError("gameData is null");
        }
    }

    // ライフUIの更新
    public void UpdateLifeUI()
    {
        Debug.Log("[START_DEBUG] OnlineGameManager.UpdateLifeUI() called");
        if (playerLifeText != null)
            playerLifeText.text = $"Life: {matchManager.PlayerLife}";
        if (opponentLifeText != null)
            opponentLifeText.text = $"Life: {matchManager.OpponentLife}";
    }

    // マッチング完了パネル表示後にゲームフェーズ監視を開始するコルーチン
    private IEnumerator ShowMatchStartPanelAndStartMonitoring(string playerName, string opponentName, float duration)
    {
        Debug.Log($"[START_DEBUG] Showing match start panel for {duration} seconds");       
        // マッチング完了パネルを表示
        panelManager.ShowMatchStartPanel(playerName, opponentName, duration);
        // パネル表示時間分待機
        yield return new WaitForSeconds(duration);
        
        // フェーズ移行時間を設定してからゲームフェーズ監視を開始
        if (gameData != null)
        {
            currentGameId = gameData.gameId;
            currentPlayerId = gameData.playerId;
            
            // フェーズ移行時間を設定
            yield return StartCoroutine(SetPhaseTransitionTime());
            
            Debug.Log($"Starting game phase monitoring for gameId: {currentGameId}, playerId: {currentPlayerId}");
            StartGamePhaseMonitoring();
            }
            else
            {
            Debug.LogError("gameData is null, cannot start phase monitoring");
        }
    }

    // フェーズ移行時間を設定するコルーチン
    private IEnumerator SetPhaseTransitionTime()
    {
        Debug.Log($"Setting phase transition time for gameId: {currentGameId}, playerId: {currentPlayerId}");
        
        // set-phase-transition APIを呼び出す
        HttpManager.Instance.SetPhaseTransitionTime(currentGameId, currentPlayerId, 3, OnPhaseTransitionSet, OnPhaseTransitionError);
        
        // 非同期処理の完了を待つ
        yield return new WaitForSeconds(0.1f);
    }

    // フェーズ移行時間設定成功時のコールバック
    private void OnPhaseTransitionSet(string response)
    {
        Debug.Log($"Phase transition time set successfully: {response}");
    }

    // フェーズ移行時間設定エラー時のコールバック
    private void OnPhaseTransitionError(string error)
    {
        Debug.LogError($"Failed to set phase transition time: {error}");
    }

    // ゲームフェーズ監視を開始
    private void StartGamePhaseMonitoring()
    {
        Debug.Log($"StartGamePhaseMonitoring called, current coroutine: {gamePhaseMonitorCoroutine != null}");
        
        if (gamePhaseMonitorCoroutine != null)
        {
            StopCoroutine(gamePhaseMonitorCoroutine);
            Debug.Log("Stopped existing phase monitoring coroutine");
        }
        
        gamePhaseMonitorCoroutine = StartCoroutine(MonitorGamePhase());
        Debug.Log("Started new phase monitoring coroutine");
    }

    // ゲームフェーズを定期的に監視
    private IEnumerator MonitorGamePhase()
    {
        Debug.Log("Starting game phase monitoring coroutine");
        Debug.Log($"MonitorGamePhase: Initial flags - isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}");
        
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // 0.1秒ごとに状態確認（短縮）
            
            // セット完了フェーズになったら監視を継続（Betting Phaseへの遷移のため）
            if (isSetCompletePhaseActive)
            {
                Debug.Log($"Set Complete phase active, continuing monitoring: isSetCompletePhaseActive={isSetCompletePhaseActive}");
                Debug.Log($"MonitorGamePhase: Current flags during set complete - isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}");
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
            if (!string.IsNullOrEmpty(currentGameId) && !string.IsNullOrEmpty(currentPlayerId))
            {
                Debug.Log($"MonitorGamePhase: Checking game phase for gameId: {currentGameId}, playerId: {currentPlayerId}");
                Debug.Log($"MonitorGamePhase: Current flags before phase check - isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}");
                StartCoroutine(CheckGamePhase());
            }
            else
            {
                Debug.LogWarning("gameId or playerId is empty, skipping phase check");
            }
        }
    }

    // ゲームフェーズを確認
    private IEnumerator CheckGamePhase()
    {
        try
        {
            Debug.Log($"Checking game phase for gameId: {currentGameId}, playerId: {currentPlayerId}");
            
            // HttpManagerを使用してget-game-state APIを呼び出す
            HttpManager.Instance.GetGameState(currentGameId, currentPlayerId, OnGameStateReceived, OnGameStateError);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking game phase: {e.Message}");
        }
        
        yield return null; // 非同期処理のため、yield return null
    }

    // ゲーム状態を受信した時の処理
    private void OnGameStateReceived(string response)
    {
        try
        {
            Debug.Log($"Received game state: {response}");
            
            // JSONをパース
            var gameState = JsonUtility.FromJson<GameStateResponse>(response);
            
            if (gameState != null)
            {
                HandleGamePhaseChange(gameState.gamePhase);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing game state: {e.Message}");
        }
    }

    // ゲーム状態エラー時の処理
    private void OnGameStateError(string error)
    {
        Debug.LogError($"Game state error: {error}");
    }

    // ゲームフェーズ変更を処理
    private void HandleGamePhaseChange(string newPhase)
    {
        // フェーズテキストを更新
        if (panelManager != null)
        {
            panelManager.UpdatePhaseText(newPhase);
            Debug.Log($"Phase text updated to: {newPhase}");
        }
        else
        {
            Debug.LogWarning("panelManager is null in HandleGamePhaseChange");
        }
        
        // 現在のフェーズを記録
        string currentPhase = isSetPhaseActive ? "set_phase" : (isBettingPhaseActive ? "betting" : (isRevealPhaseActive ? "reveal" : (isSetCompletePhaseActive ? "set_complete" : "unknown")));
        Debug.Log($"Current phase string: {currentPhase}");
        
        // 同じフェーズの場合は処理をスキップ
        if (currentPhase == newPhase)
        {
            Debug.Log($"Phase {newPhase} already active, skipping phase change");
            return;
        }
        
        // フェーズ遷移の順序をチェック
        if (newPhase == "reveal" && !isBettingPhaseActive)
        {
            Debug.LogWarning("Skipping reveal phase, betting phase not completed yet");
            return;
        }
        
        Debug.Log($"Processing phase change from {currentPhase} to {newPhase}");
        
        switch (newPhase)
        {
            case "set_phase":
                Debug.Log("Processing set_phase case");
                if (!isSetPhaseActive)
                {
                    Debug.Log("Activating set phase");
                    isSetPhaseActive = true;
                    isRevealPhaseActive = false;
                    isBettingPhaseActive = false;
                    isSetCompletePhaseActive = false;
                    canSetCard = true;
                    Debug.Log("Set phase flags updated.");
                    if (panelManager != null)
                    {
                        panelManager.ShowStartPhasePanel("Set Phase", "カードをSetZoneにセットしてください");
                        Debug.Log("Set Phase started");
                        Debug.Log("Lambda function: set_phase phase activated");
                    }
                    else
                    {
                        Debug.LogError("panelManager is null!");
                    }
                }
                else
                {
                    Debug.Log("Set phase already active, skipping");
                }
                break;
                
            case "card_placement":
                // card_placementはset_phaseと統合されたため、set_phaseとして処理
                // 遷移処理は削除したが、一応残す。あとでcaseを削除して問題なく動くか確認する。
                Debug.Log("Processing card_placement case (redirected to set_phase)");
                HandleGamePhaseChange("set_phase");
                break;

            case "betting":
                Debug.Log($"Processing betting case, isBettingPhaseActive: {isBettingPhaseActive}");
                Debug.Log($"Current flags before betting transition: isSetPhaseActive={isSetPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}, canSetCard={canSetCard}");
                
                if (!isBettingPhaseActive)
                {
                    Debug.Log("Activating Betting Phase");
                    isBettingPhaseActive = true;
                    isSetPhaseActive = false;
                    isSetCompletePhaseActive = false;
                    canSetCard = false;
                    Debug.Log("Betting phase flags updated: isBettingPhaseActive=true, isSetPhaseActive=false, isSetCompletePhaseActive=false, canSetCard=false");
                    
                    // 親子システムの初期化
                    InitializeParentChildSystem();
                    
                    // 相手のカードが表示されているか確認し、必要に応じて表示
                    EnsureOpponentCardDisplayed();
                    
                    // 親のターンを開始
                    StartParentTurn();
                    
                    if (panelManager != null)
                    {
                        panelManager.ShowBettingPhasePanel();
                        Debug.Log("Betting Phase started");
                        Debug.Log("Lambda function: betting phase activated");
                        
                        // Betting Phase開始後にフェーズ監視を再開（重複を防ぐ）
                        if (gamePhaseMonitorCoroutine == null)
                        {
                            Debug.Log("Restarting phase monitoring for betting phase");
                            StartGamePhaseMonitoring();
                        }
                        else
                        {
                            Debug.Log("Phase monitoring already active, skipping restart");
                        }
                    }
                    else
                    {
                        Debug.LogError("panelManager is null in betting case");
                    }
                }
                else
                {
                    Debug.Log("Betting phase already active, skipping");
                }
                break;

            case "reveal":
                Debug.Log("Processing reveal case");
                if (!isRevealPhaseActive)
                {
                    Debug.Log("Activating reveal phase");
                    isRevealPhaseActive = true;
                    isSetPhaseActive = false;
                    isBettingPhaseActive = false;
                    isSetCompletePhaseActive = false;
                    canSetCard = false;
                    Debug.Log("Reveal phase flags updated: isRevealPhaseActive=true, others=false");
                    
                    if (panelManager != null)
                    {
                        panelManager.ShowRevealPhasePanel();
                        Debug.Log("Reveal Phase started");
                        Debug.Log("Lambda function: reveal phase activated");
                    }
                }
                else
                {
                    Debug.Log("Reveal phase already active, skipping");
                }
                break;

            case "open_phase":
                Debug.Log("Processing open_phase case");
                if (panelManager != null)
                {
                    // HIGH/LOW表示をクリア
                    panelManager.ClearHighLowDisplay();
                    Debug.Log("HIGH/LOW display cleared for open phase");
                    
                    panelManager.ShowOpenPhasePanel();
                    Debug.Log("Open Phase started");
                    Debug.Log("Lambda function: open_phase activated");
                }
                else
                {
                    Debug.LogError("panelManager is null in open_phase case");
                }
                break;
                
            default:
                Debug.LogWarning($"Unknown game phase: {newPhase}");
                break;
        }
        
        Debug.Log($"HandleGamePhaseChange completed. Final state: isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}, canSetCard={canSetCard}");
    }

    // カードセット可能かどうかを取得
    public bool CanSetCard()
    {
        Debug.Log($"CanSetCard called, returning: {canSetCard}");
        return canSetCard;
    }

    // カード配置確認パネルを表示
    public void ShowConfirmation(CardDisplay card, OnlineDropZone zone)
    {
        Debug.Log($"ShowConfirmation called for card: {card.CardValue}");
        
        // canSetCardがfalseの場合はカード配置を許可しない
        if (!canSetCard)
        {
            Debug.LogWarning($"ShowConfirmation: canSetCard is false, rejecting card placement for card: {card.CardValue}");
            
            // カードを元の位置に戻す
            ReturnCardToOriginalPosition(card);
            return;
        }
        
        currentCard = card;
        currentZone = zone;
        
        if (panelManager != null)
        {
            Debug.Log($"panelManager found, yesButton: {panelManager.yesButton != null}");
        panelManager.confirmationPanel.SetActive(true);

        // リスナーの多重登録防止
        panelManager.yesButton.onClick.RemoveAllListeners();
        panelManager.noButton.onClick.RemoveAllListeners();

        panelManager.yesButton.onClick.AddListener(ConfirmPlacement);
        panelManager.noButton.onClick.AddListener(CancelPlacement);
            
            Debug.Log("Button listeners added successfully");
        }
        else
        {
            Debug.LogError("panelManager is null!");
        }
    }

    // カード配置を確定
    public void ConfirmPlacement()
    {
        Debug.Log($"[CARD_CONFIRM_DEBUG] === ConfirmPlacement開始 ===");
        Debug.Log($"[CARD_CONFIRM_DEBUG] OnlineGameManager - ConfirmPlacement method entered!");
        Debug.Log($"[CARD_CONFIRM_DEBUG] OnlineGameManager - ConfirmPlacement called for card: {currentCard?.CardValue}");
        
        if (currentCard != null && currentZone != null)
        {
            Debug.Log($"[CARD_CONFIRM_DEBUG] カードとゾーンの状態確認:");
            Debug.Log($"[CARD_CONFIRM_DEBUG]   currentCard: {currentCard != null}");
            Debug.Log($"[CARD_CONFIRM_DEBUG]   currentZone: {currentZone != null}");
            Debug.Log($"[CARD_CONFIRM_DEBUG]   currentCard.CardValue: {currentCard.CardValue}");
            Debug.Log($"[CARD_CONFIRM_DEBUG]   currentZone.isPlayerZone: {currentZone.isPlayerZone}");
            
            // カードをDropZoneの子にする（親子関係をセット）
            currentCard.transform.SetParent(currentZone.transform);
            currentCard.transform.localPosition = Vector3.zero;

            // 配置されたカードを記録
            setPlayerCard = currentCard;
            Debug.Log($"[CARD_CONFIRM_DEBUG] setPlayerCard設定完了: {setPlayerCard.CardValue}");

            // 確認パネルを非表示
            if (panelManager != null)
            {
                panelManager.confirmationPanel.SetActive(false);
                Debug.Log($"[CARD_CONFIRM_DEBUG] 確認パネル非表示完了");
            }
            
            // カード配置完了をサーバーに通知
            Debug.Log($"[CARD_CONFIRM_DEBUG] NotifyCardPlacement呼び出し開始");
            StartCoroutine(NotifyCardPlacement(currentCard.CardValue));
            canSetCard = false;
        }
        else
        {
            Debug.LogError($"[CARD_CONFIRM_DEBUG] currentCard or currentZone is null!");
            Debug.LogError($"[CARD_CONFIRM_DEBUG] currentCard: {currentCard != null}, currentZone: {currentZone != null}");
        }
        Debug.Log($"[CARD_CONFIRM_DEBUG] === ConfirmPlacement完了 ===");
    }

    // カード配置をキャンセル
    public void CancelPlacement()
    {
        Debug.Log("CancelPlacement called");
        
        if (currentCard != null)
    {
        // カードを元の位置に戻す
        var drag = currentCard.GetComponent<CardDraggable>();
        if (drag != null)
        {
            currentCard.transform.SetParent(drag.OriginalParent);
            currentCard.transform.position = drag.OriginalPosition;
            }
        }
        
        // 確認パネルを非表示
        if (panelManager != null)
        {
        panelManager.confirmationPanel.SetActive(false);
    }

        ResetPlacementState();
    }

    // カード配置状態をリセット
    private void ResetPlacementState()
    {
        currentCard = null;
        currentZone = null;
        
        if (panelManager != null)
        {
        panelManager.confirmationPanel.SetActive(false);

        // リスナーをリセット
        panelManager.yesButton.onClick.RemoveAllListeners();
        panelManager.noButton.onClick.RemoveAllListeners();
    }
    }

    // カードを元の位置（手札）に戻す
    private void ReturnCardToOriginalPosition(CardDisplay card)
    {
        Debug.Log($"ReturnCardToOriginalPosition called for card: {card.CardValue}");
        
        if (card != null)
        {
            // CardDraggableコンポーネントを取得
            var drag = card.GetComponent<CardDraggable>();
            if (drag != null && drag.OriginalParent != null)
            {
                // カードを元の親（手札）に戻す
                card.transform.SetParent(drag.OriginalParent);
                
                // 元の位置に戻す
                if (drag.OriginalPosition != Vector3.zero)
                {
                    card.transform.position = drag.OriginalPosition;
                }
                else
                {
                    // OriginalPositionが設定されていない場合は手札の適切な位置に配置
                    card.transform.localPosition = Vector3.zero;
                }
                
                Debug.Log($"Card {card.CardValue} returned to original position");
            }
            else
            {
                Debug.LogWarning($"CardDraggable component or OriginalParent not found for card: {card.CardValue}");
                
                // フォールバック: handManagerのplayerCard1またはplayerCard2に戻す
                if (handManager != null)
                {
                    // カードの値に基づいて適切な手札位置に戻す
                    if (card == handManager.playerCard1 || card.CardValue == handManager.playerCard1?.CardValue)
                    {
                        card.transform.SetParent(handManager.playerCard1.transform.parent);
                        card.transform.position = handManager.playerCard1.transform.position;
                        Debug.Log($"Card {card.CardValue} returned to playerCard1 position");
                    }
                    else if (card == handManager.playerCard2 || card.CardValue == handManager.playerCard2?.CardValue)
                    {
                        card.transform.SetParent(handManager.playerCard2.transform.parent);
                        card.transform.position = handManager.playerCard2.transform.position;
                        Debug.Log($"Card {card.CardValue} returned to playerCard2 position");
                    }
                    else
                    {
                        Debug.LogWarning($"Card {card.CardValue} could not be matched to any hand position");
                    }
                }
                else
                {
                    Debug.LogError($"handManager is null, cannot return card: {card.CardValue}");
                }
            }
        }
        else
        {
            Debug.LogError("ReturnCardToOriginalPosition: card is null");
    }
    }

    // サーバーにカード配置を通知
    private IEnumerator NotifyCardPlacement(int cardValue)
    {
        Debug.Log($"[CARD_NOTIFY_DEBUG] === NotifyCardPlacement開始 ===");
        Debug.Log($"[CARD_NOTIFY_DEBUG] OnlineGameManager - NotifyCardPlacement called with cardValue: {cardValue}");
        Debug.Log($"[CARD_NOTIFY_DEBUG] 現在の状態:");
        Debug.Log($"[CARD_NOTIFY_DEBUG]   isSetPhaseActive: {isSetPhaseActive}");
        Debug.Log($"[CARD_NOTIFY_DEBUG]   isSetCompletePhaseActive: {isSetCompletePhaseActive}");
        Debug.Log($"[CARD_NOTIFY_DEBUG]   gameData: {gameData != null}");
        if (gameData != null)
        {
            Debug.Log($"[CARD_NOTIFY_DEBUG]   gameData.gameId: {gameData.gameId}");
            Debug.Log($"[CARD_NOTIFY_DEBUG]   gameData.playerId: {gameData.playerId}");
            Debug.Log($"[CARD_NOTIFY_DEBUG]   gameData.isPlayer1: {gameData.isPlayer1}");
        }
        
        // 既存のSetCardメソッドを使用
        Debug.Log($"[CARD_NOTIFY_DEBUG] 既存のSetCardメソッドを呼び出し");
        SetCard(gameData.gameId, gameData.playerId, cardValue, OnCardPlacementSuccess, OnCardPlacementError);
        
        Debug.Log($"[CARD_NOTIFY_DEBUG] SetCardメソッド呼び出し完了");
        Debug.Log($"[CARD_NOTIFY_DEBUG] === NotifyCardPlacement完了 ===");
        
        yield return null;
    }

    // カードセット用のAPI呼び出しメソッド
    private void SetCard(
        string gameId,
        string playerId,
        int cardValue,
        Action<string> onSuccess,
        Action<string> onError)
    {
        Debug.Log($"[SET_CARD_DEBUG] === SetCard開始 ===");
        Debug.Log($"[SET_CARD_DEBUG] OnlineGameManager - SetCard API call started");
        Debug.Log($"[SET_CARD_DEBUG] OnlineGameManager - SetCard method entered with gameId: {gameId}, playerId: {playerId}, cardValue: {cardValue}");
        Debug.Log($"[SET_CARD_DEBUG] OnlineGameManager - This will update Player{(playerId == gameData.playerId ? "1" : "2")}Set");
        
        string url = $"{HttpManager.ApiBaseUrl}/update-state";
        string jsonBody = JsonUtility.ToJson(new SetCardRequest
        {
            gameId = gameId,
            playerId = playerId,
            cardValue = cardValue
        });
        
        Debug.Log($"[SET_CARD_DEBUG] API呼び出し詳細:");
        Debug.Log($"[SET_CARD_DEBUG]   URL: {url}");
        Debug.Log($"[SET_CARD_DEBUG]   JSON: {jsonBody}");
        Debug.Log($"[SET_CARD_DEBUG]   onSuccess: {onSuccess != null}");
        Debug.Log($"[SET_CARD_DEBUG]   onError: {onError != null}");
        
        Debug.Log($"[SET_CARD_DEBUG] HttpManager.Post呼び出し開始");
        HttpManager.Instance.Post<SetCardResponse>(
            url,
            jsonBody,
            (response) => {
                Debug.Log($"[SET_CARD_DEBUG] ✅ SetCard API成功");
                string responseJson = JsonUtility.ToJson(response);
                Debug.Log($"[SET_CARD_DEBUG] レスポンスJSON: {responseJson}");
                Debug.Log($"[SET_CARD_DEBUG] onSuccessコールバック呼び出し");
                onSuccess?.Invoke(responseJson);
            },
            (error) => {
                Debug.LogError($"[SET_CARD_DEBUG] ❌ SetCard API失敗: {error}");
                Debug.LogError($"[SET_CARD_DEBUG] onErrorコールバック呼び出し");
                onError?.Invoke(error);
            }
        );
        
        Debug.Log($"[SET_CARD_DEBUG] SetCardメソッド完了、API呼び出し開始済み");
        Debug.Log($"[SET_CARD_DEBUG] === SetCard完了 ===");
    }

    // カード配置成功時のコールバック
    private void OnCardPlacementSuccess(string response)
    {
        Debug.Log($"=== OnCardPlacementSuccess開始 ===");
        Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - OnCardPlacementSuccess called with response: {response}");
        Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - 現在の状態:");
        Debug.Log($"[CARD_PLACEMENT_DEBUG]   isSetPhaseActive: {isSetPhaseActive}");
        Debug.Log($"[CARD_PLACEMENT_DEBUG]   isSetCompletePhaseActive: {isSetCompletePhaseActive}");
        Debug.Log($"[CARD_PLACEMENT_DEBUG]   isBettingPhaseActive: {isBettingPhaseActive}");
        Debug.Log($"[CARD_PLACEMENT_DEBUG]   gameData: {gameData != null}");
        if (gameData != null)
        {
            Debug.Log($"[CARD_PLACEMENT_DEBUG]   gameData.player1Cards: {(gameData.player1Cards != null ? string.Join(",", gameData.player1Cards) : "null")}");
            Debug.Log($"[CARD_PLACEMENT_DEBUG]   gameData.player2Cards: {(gameData.player2Cards != null ? string.Join(",", gameData.player2Cards) : "null")}");
        }
        
        try
        {
            var setCardResponse = JsonUtility.FromJson<SetCardResponse>(response);
            Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - Parsed response - player1Set: {setCardResponse.player1Set}, player2Set: {setCardResponse.player2Set}");
            Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - Parsed response - player1CardValue: {setCardResponse.player1CardValue}, player2CardValue: {setCardResponse.player2CardValue}");
            
            // 両プレイヤーがカードをセットしたかチェック
            if (setCardResponse.player1Set && setCardResponse.player2Set)
            {
                Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - Both players have set cards!");
                Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - About to start Set Complete Phase. Current flags: isSetCompletePhaseActive={isSetCompletePhaseActive}");
                
                // セット完了フェーズを開始（重複実行を防ぐ）
                if (!isSetCompletePhaseActive)
                {
                    Debug.Log("[CARD_PLACEMENT_DEBUG] OnlineGameManager - Starting Set Complete Phase");
                    Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - 呼び出し前の状態確認:");
                    Debug.Log($"[CARD_PLACEMENT_DEBUG]   setCardResponse.player1CardValue: {setCardResponse.player1CardValue}");
                    Debug.Log($"[CARD_PLACEMENT_DEBUG]   setCardResponse.player2CardValue: {setCardResponse.player2CardValue}");
                    Debug.Log($"[CARD_PLACEMENT_DEBUG]   isSetCompletePhaseActive: {isSetCompletePhaseActive}");
                    
                    StartCoroutine(HandleSetCompletePhase(setCardResponse.player1CardValue, setCardResponse.player2CardValue));
                    
                    Debug.Log("[CARD_PLACEMENT_DEBUG] OnlineGameManager - HandleSetCompletePhase呼び出し完了");
                }
                else
                {
                    Debug.LogWarning("[CARD_PLACEMENT_DEBUG] OnlineGameManager - Set Complete Phase already active, skipping duplicate start");
                }
            }
            else
            {
                Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - Waiting for opponent. Player1Set: {setCardResponse.player1Set}, Player2Set: {setCardResponse.player2Set}");
                Debug.Log($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - Current flags while waiting: isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - Error parsing card placement response: {e.Message}");
            Debug.LogError($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - Full exception: {e}");
        }
        Debug.Log($"=== OnCardPlacementSuccess完了 ===");
    }

    // セット完了フェーズの処理
    private IEnumerator HandleSetCompletePhase(int player1CardValue, int player2CardValue)
    {
        Debug.Log($"=== HandleSetCompletePhase開始 ===");
        Debug.Log($"[SET_COMPLETE_DEBUG] OnlineGameManager - HandleSetCompletePhase started with player1CardValue: {player1CardValue}, player2CardValue: {player2CardValue}");
        Debug.Log($"[SET_COMPLETE_DEBUG] OnlineGameManager - HandleSetCompletePhase: Current flags before starting - isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isSetPhaseActive={isSetPhaseActive}");
        Debug.Log($"[SET_COMPLETE_DEBUG] OnlineGameManager - HandleSetCompletePhase: isSetCompletePhaseActive will be set to true");
        
        isSetCompletePhaseActive = true;
        Debug.Log($"[SET_COMPLETE_DEBUG] OnlineGameManager - HandleSetCompletePhase: isSetCompletePhaseActive set to {isSetCompletePhaseActive}");
        
        // フラグをリセット
        isSetPhaseActive = false;
        isBettingPhaseActive = false;
        
        Debug.Log($"[SET_COMPLETE_DEBUG] HandleSetCompletePhase: フラグリセット完了 - isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}");
        
        // gameDataの状態を確認
        Debug.Log($"[SET_COMPLETE_DEBUG] HandleSetCompletePhase: gameDataの状態確認");
        Debug.Log($"[SET_COMPLETE_DEBUG]   gameData: {gameData != null}");
        if (gameData != null)
        {
            Debug.Log($"[SET_COMPLETE_DEBUG]   gameData.player1Cards: {gameData.player1Cards != null}");
            if (gameData.player1Cards != null)
            {
                Debug.Log($"[SET_COMPLETE_DEBUG]   gameData.player1Cards.Length: {gameData.player1Cards.Length}");
                if (gameData.player1Cards.Length > 0)
                {
                    Debug.Log($"[SET_COMPLETE_DEBUG]   gameData.player1Cards[0]: {gameData.player1Cards[0]}");
                    Debug.Log($"[SET_COMPLETE_DEBUG]   gameData.player1Cards[1]: {gameData.player1Cards[1]}");
                }
            }
            Debug.Log($"[SET_COMPLETE_DEBUG]   gameData.player2Cards: {gameData.player2Cards != null}");
            if (gameData.player2Cards != null)
            {
                Debug.Log($"[SET_COMPLETE_DEBUG]   gameData.player2Cards.Length: {gameData.player2Cards.Length}");
                if (gameData.player2Cards.Length > 0)
                {
                    Debug.Log($"[SET_COMPLETE_DEBUG]   gameData.player2Cards[0]: {gameData.player2Cards[0]}");
                    Debug.Log($"[SET_COMPLETE_DEBUG]   gameData.player2Cards[1]: {gameData.player2Cards[1]}");
                }
            }
        }
        
        // panelManagerの状態を確認
        Debug.Log($"[SET_COMPLETE_DEBUG] HandleSetCompletePhase: panelManagerの状態確認");
        Debug.Log($"[SET_COMPLETE_DEBUG]   panelManager: {panelManager != null}");
        
        // 相手のカードを裏向きで表示
        Debug.Log($"[SET_COMPLETE_DEBUG] HandleSetCompletePhase: DisplayOpponentCardFaceDown呼び出し");
        DisplayOpponentCardFaceDown(player2CardValue);
        
        // HIGH/LOW表示はStart()で手札設定完了時に更新済みのため、ここでは更新しない
        Debug.Log($"[SET_COMPLETE_DEBUG] HIGH/LOW表示はStart()で更新済みのため、ここでは更新しません");
        
        // セット完了パネルを表示
        if (panelManager != null)
        {
            panelManager.ShowSetCompletePanel();
            Debug.Log("HandleSetCompletePhase: Set Complete Panel shown");
        }
        else
        {
            Debug.LogWarning("HandleSetCompletePhase: panelManager is null, cannot show Set Complete Panel");
        }
        
        // セット完了パネルを3秒間表示
        Debug.Log("HandleSetCompletePhase: Waiting 3 seconds before hiding panel");
        yield return new WaitForSeconds(3f);
        
        // セット完了パネルを非表示
        if (panelManager != null)
        {
            panelManager.HideSetCompletePanel();
            Debug.Log("HandleSetCompletePhase: Set Complete Panel hidden");
        }
        
        // Betting Phaseに遷移
        Debug.Log("HandleSetCompletePhase: About to transition to Betting Phase");
        Debug.Log($"HandleSetCompletePhase: Current flags before transition - isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isSetPhaseActive={isSetPhaseActive}");
        
        Debug.Log("HandleSetCompletePhase: Calling HandleGamePhaseChange('betting')");
        HandleGamePhaseChange("betting");
        Debug.Log("HandleSetCompletePhase: HandleGamePhaseChange('betting') completed");
        
        // フェーズ遷移後の状態を確認
        Debug.Log($"HandleSetCompletePhase: Flags after transition - isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isSetPhaseActive={isSetPhaseActive}");
        
        // isSetCompletePhaseActiveをfalseに設定
        Debug.Log("HandleSetCompletePhase: Setting isSetCompletePhaseActive to false");
        isSetCompletePhaseActive = false;
        Debug.Log($"HandleSetCompletePhase: Final flags - isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isSetPhaseActive={isSetPhaseActive}");
        
        Debug.Log("HandleSetCompletePhase completed");
    }

    // 相手のカードを裏向きで表示
    private void DisplayOpponentCardFaceDown(int cardValue)
    {
        Debug.Log($"DisplayOpponentCardFaceDown called with cardValue: {cardValue}");
        
        // 相手側のDropZoneを探す
        OnlineDropZone opponentZone = FindOpponentDropZone();
        
        if (opponentZone != null)
        {
            // カードプレハブを生成して相手側のDropZoneに配置
            CreateOpponentCard(cardValue, opponentZone);
            Debug.Log($"Opponent card {cardValue} displayed face down in opponent zone");
        }
        else
        {
            Debug.LogError("Could not find opponent DropZone");
        }
    }

    // 相手側のDropZoneを探す
    private OnlineDropZone FindOpponentDropZone()
    {
        OnlineDropZone[] dropZones = FindObjectsOfType<OnlineDropZone>();
        
        foreach (OnlineDropZone zone in dropZones)
        {
            if (!zone.isPlayerZone) // 相手側のDropZone
            {
                Debug.Log($"Found opponent DropZone: {zone.name}");
                return zone;
            }
        }
        
        Debug.LogWarning("No opponent DropZone found");
        return null;
    }

    // 相手のカードを生成して配置
    private void CreateOpponentCard(int cardValue, OnlineDropZone opponentZone)
    {
        // カードプレハブを確認
        if (cardPrefab == null)
        {
            Debug.LogError("cardPrefab is null! Please assign it in the Inspector.");
            return;
        }
        
        // カードを生成
        GameObject cardObject = Instantiate(cardPrefab, opponentZone.transform);
        CardDisplay cardDisplay = cardObject.GetComponent<CardDisplay>();
        
        if (cardDisplay != null)
        {
            // カードの値を設定
            cardDisplay.SetCardValue(cardValue);
            
            // 裏向きにする
            cardDisplay.SetCardFaceDown(true);
            
            // 位置を調整
            cardObject.transform.localPosition = Vector3.zero;
            
            Debug.Log($"Opponent card {cardValue} created and placed in opponent zone");
        }
        else
        {
            Debug.LogError("CardDisplay component not found on card prefab");
        }
    }

    // 相手のカードが確実に表示されているか確認
    private void EnsureOpponentCardDisplayed()
    {
        Debug.Log("Ensuring opponent card is displayed");
        
        // 相手側のDropZoneを探す
        OnlineDropZone opponentZone = FindOpponentDropZone();
        
        if (opponentZone != null)
        {
            // 既にカードが配置されているかチェック
            CardDisplay existingCard = opponentZone.GetComponentInChildren<CardDisplay>();
            
            if (existingCard == null)
            {
                // カードが配置されていない場合、相手のカード値を取得して表示
                if (gameData != null)
                {
                    int opponentCardValue = isPlayer1 ? gameData.player2Cards[0] : gameData.player1Cards[0];
                    Debug.Log($"No opponent card found, creating one with value: {opponentCardValue}");
                    CreateOpponentCard(opponentCardValue, opponentZone);
                }
                else
                {
                    Debug.LogWarning("gameData is null, cannot determine opponent card value");
                }
            }
            else
            {
                Debug.Log("Opponent card already displayed");
            }
        }
        else
        {
            Debug.LogError("Could not find opponent DropZone for card display check");
        }
    }

    // 相手のカードを表向きで表示
    private void DisplayOpponentCardFaceUp(int cardValue)
    {
        Debug.Log($"DisplayOpponentCardFaceUp called with cardValue: {cardValue}");
        // TODO: 相手のSetZoneのカードを表向きにする
    }

    // カード配置エラー時のコールバック
    private void OnCardPlacementError(string error)
    {
        Debug.LogError($"[CARD_PLACEMENT_DEBUG] OnlineGameManager - OnCardPlacementError called with error: {error}");
        Debug.LogError($"[CARD_PLACEMENT_DEBUG] カード配置API呼び出しでエラーが発生しました");
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
        public int player1BetAmount; // 親のBet金額
    }

    [System.Serializable]
    private class SetCardRequest
    {
        public string gameId;
        public string playerId;
        public int cardValue;
    }

    [System.Serializable]
    private class SetCardResponse
    {
        public string gameId;
        public string gamePhase;
        public bool player1Set;
        public bool player2Set;
        public int player1CardValue;
        public int player2CardValue;
        public string player1Id;
        public string player2Id;
        public bool player1CardPlaced;
        public bool player2CardPlaced;
    }

    [System.Serializable]
    private class BetActionRequest
    {
        public string gameId;
        public string playerId;
        public string actionType; // "call", "raise", "drop"
        public int betValue;
    }

    [System.Serializable]
    private class BetActionResponse
    {
        public string gameId;
        public string gamePhase;
        public bool isMyTurn;
        public int currentBet;
        public OpponentAction opponentAction;
        public string message;
    }

    [System.Serializable]
    private class OpponentAction
    {
        public string actionType; // "call", "raise", "drop"
        public int betValue;
        public string playerId;
    }

    [System.Serializable]
    private class ParentBetCompleteRequest
    {
        public string roomCode;
        public string playerId;
        public string betAction; // "call", "raise", "drop"
        public int betAmount;
    }
    
    [System.Serializable]
    private class CheckParentBetStatusRequest
    {
        public string roomCode;
    }
    
    [System.Serializable]
    private class ParentBetStatusResponse
    {
        public bool parentBetComplete;
        public string parentBetAction;
        public int parentBetAmount;
        public int minimumBetAmount;
        public string message;
    }

    public void SetOpponentCalled(bool called) { /* TODO: 実装 */ }
    public void RevealCards() { /* TODO: 実装 */ }
    public CardDisplay SetOpponentCard { get; }
    public bool PlayerCanUseScanSkill { get; }
    public bool PlayerCanUseChangeSkill { get; }
    public bool PlayerCanUseObstructSkill { get; }
    public bool PlayerCanUseFakeOutSkill { get; }
    public bool PlayerCanUseCopySkill { get; }
}
