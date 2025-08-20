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

    // 親子システムの初期化
    private void InitializeParentChildSystem()
    {
        // CreateRoomから対戦に遷移する方を先行の親とする
        // gameData.isPlayer1がtrueの場合、そのプレイヤーが先行の親
        isParent = gameData.isPlayer1;
        currentRound = 1;
        
        Debug.Log($"OnlineGameManager - Parent-Child system initialized. isParent: {isParent}, currentRound: {currentRound}");
        
        // 親子の表示を更新
        if (panelManager != null)
        {
            panelManager.UpdateParentChildDisplay(isParent);
            panelManager.UpdatePlayerRoleDisplay(isParent);
        }
    }

    // 親子の交代判定
    private void CheckParentChange()
    {
        if (currentRound % parentChangeRound == 0)
        {
            isParent = !isParent;
            Debug.Log($"OnlineGameManager - Parent changed at round {currentRound}. New parent: {isParent}");
            
            if (panelManager != null)
            {
                panelManager.UpdateParentChildDisplay(isParent);
                panelManager.UpdatePlayerRoleDisplay(isParent);
            }
        }
    }

    // ラウンド終了時の処理
    private void OnRoundEnd()
    {
        Debug.Log($"OnlineGameManager - Round {currentRound} ended");
        
        // 親子交代をチェック
        CheckParentChange();
        
        // 次のラウンドに進む
        currentRound++;
        Debug.Log($"OnlineGameManager - Moving to round {currentRound}");
        
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
            
            Debug.Log("OnlineGameManager - Parent turn started. Parent can now bet.");
            
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
            
            Debug.Log("OnlineGameManager - Child waiting for parent action.");
            
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
        if (!isParent)
        {
            isParentTurn = false;
            isMyTurn = true;
            waitingForParentAction = false;
            waitingForChildAction = false;
            
            Debug.Log("OnlineGameManager - Child turn started. Child can now bet.");
            
            if (panelManager != null)
            {
                panelManager.HideWaitingForParentPanel();
                panelManager.HideParentBettingPanel(); // 親プレイヤーBet中パネルを非表示
                panelManager.SetBettingButtonInteractable(true);
            }
        }
        else
        {
            isParentTurn = true;
            isMyTurn = false;
            waitingForParentAction = false;
            waitingForChildAction = true;
            
            Debug.Log("OnlineGameManager - Parent waiting for child action.");
            
            if (panelManager != null)
            {
                panelManager.ShowWaitingForChildPanel();
                panelManager.SetBettingButtonInteractable(false);
            }
        }
        
        // 子プレイヤーの場合、親のBet完了を監視開始
        if (!isParent)
        {
            StartParentBetMonitoring();
        }
    }
    
    // 親のBet完了監視を開始
    private void StartParentBetMonitoring()
    {
        if (isParent)
        {
            Debug.Log("OnlineGameManager - Parent player, skipping bet monitoring");
            return;
        }
        
        Debug.Log("OnlineGameManager - Starting parent bet monitoring for child player");
        
        // 既存の監視を停止
        if (parentBetMonitorCoroutine != null)
        {
            StopCoroutine(parentBetMonitorCoroutine);
        }
        
        // 新しい監視を開始
        parentBetMonitorCoroutine = StartCoroutine(MonitorParentBetStatus());
    }
    
    // 親のBet完了状態を監視
    private IEnumerator MonitorParentBetStatus()
    {
        Debug.Log("OnlineGameManager - Parent bet monitoring started");
        
        while (waitingForParentAction)
        {
            yield return new WaitForSeconds(1f); // 1秒ごとに確認
            
            // 親のBet完了状態を確認
            yield return StartCoroutine(CheckParentBetStatus());
        }
        
        Debug.Log("OnlineGameManager - Parent bet monitoring stopped");
    }
    
    // 親のBet完了状態を確認
    private IEnumerator CheckParentBetStatus()
    {
        if (gameData == null)
        {
            Debug.LogWarning("OnlineGameManager - gameData is null, cannot check parent bet status");
            yield break;
        }
        
        var requestData = new CheckParentBetStatusRequest
        {
            roomCode = gameData.roomCode
        };
        
        string jsonBody = JsonUtility.ToJson(requestData);
        Debug.Log($"OnlineGameManager - Checking parent bet status: {jsonBody}");
        
        var request = new UnityWebRequest(checkParentBetStatusUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"OnlineGameManager - Parent bet status check successful: {request.downloadHandler.text}");
            
            try
            {
                var response = JsonUtility.FromJson<ParentBetStatusResponse>(request.downloadHandler.text);
                
                if (response.parentBetComplete)
                {
                    Debug.Log($"OnlineGameManager - Parent bet completed: {response.parentBetAction}, amount: {response.parentBetAmount}");
                    
                    // 親のBet完了を処理
                    HandleParentBetComplete(response.parentBetAction, response.parentBetAmount);
                    
                    // 監視を停止
                    waitingForParentAction = false;
                    if (parentBetMonitorCoroutine != null)
                    {
                        StopCoroutine(parentBetMonitorCoroutine);
                        parentBetMonitorCoroutine = null;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"OnlineGameManager - Error parsing parent bet status response: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"OnlineGameManager - Parent bet status check failed: {request.error}");
        }
    }
    
    // 親のBet完了を処理
    private void HandleParentBetComplete(string betAction, int betAmount)
    {
        Debug.Log($"OnlineGameManager - Handling parent bet completion: {betAction}, amount: {betAmount}");
        
        switch (betAction)
        {
            case "call":
                // 親がコールした場合、子のターンを開始
                Debug.Log("OnlineGameManager - Parent called, starting child turn");
                StartChildTurn();
                break;
                
            case "raise":
                // 親がレイズした場合、最低ベット額を更新して子のターンを開始
                Debug.Log($"OnlineGameManager - Parent raised to {betAmount}, updating minimum bet and starting child turn");
                minimumBetValue = betAmount;
                currentBetValue = betAmount;
                StartChildTurn();
                UpdateBetUI();
                break;
                
            case "drop":
                // 親がドロップした場合、OpenPhaseに移行
                Debug.Log("OnlineGameManager - Parent dropped, transitioning to OpenPhase");
                HandleGamePhaseChange("reveal");
                break;
        }
    }

    // ベット値の増減
    public void IncreaseBetValue()
    {
        if (currentBetValue < 10) // 最大10まで
        {
            currentBetValue++;
            Debug.Log($"OnlineGameManager - Bet value increased to: {currentBetValue}");
            UpdateBetUI();
        }
    }

    public void DecreaseBetValue()
    {
        if (currentBetValue > minimumBetValue)
        {
            currentBetValue--;
            Debug.Log($"OnlineGameManager - Bet value decreased to: {currentBetValue}");
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
        Debug.Log($"OnlineGameManager - ExecuteBetAction called with actionType: {actionType}, betValue: {currentBetValue}");
        Debug.Log($"OnlineGameManager - Current state: isParent={isParent}, isParentTurn={isParentTurn}, isMyTurn={isMyTurn}");
        
        if (!isBettingPhaseActive)
        {
            Debug.LogWarning("OnlineGameManager - Betting phase not active, ignoring bet action");
            return;
        }

        // 自分のターンでない場合は処理しない
        if (!isMyTurn)
        {
            Debug.LogWarning("OnlineGameManager - Not my turn, ignoring bet action");
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
        Debug.Log($"OnlineGameManager - Handling parent action: {actionType}");
        
        switch (actionType)
        {
            case "call":
                // 親がコールした場合、子のターンに移行
                Debug.Log("OnlineGameManager - Parent called, starting child turn");
                StartChildTurn();
                break;
                
            case "raise":
                // 親がレイズした場合、最低ベット額を更新して子のターンに移行
                Debug.Log($"OnlineGameManager - Parent raised to {currentBetValue}, updating minimum bet and starting child turn");
                minimumBetValue = currentBetValue;
                StartChildTurn();
                break;
                
            case "drop":
                // 親がドロップした場合、OpenPhaseに移行
                Debug.Log("OnlineGameManager - Parent dropped, transitioning to OpenPhase");
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
        Debug.Log($"OnlineGameManager - Handling child action: {actionType}");
        
        switch (actionType)
        {
            case "call":
            case "drop":
                // 子がコールまたはドロップした場合、OpenPhaseに移行
                Debug.Log($"OnlineGameManager - Child {actionType}, transitioning to OpenPhase");
                HandleGamePhaseChange("reveal");
                break;
                
            case "raise":
                // 子がレイズした場合、親のターンに戻る
                Debug.Log($"OnlineGameManager - Child raised to {currentBetValue}, returning to parent turn");
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
        Debug.Log($"OnlineGameManager - Sending bet action to AWS: {actionType}, betValue: {betValue}");
        
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
    
    // 親のBet完了をDynamo側に通知
    private void NotifyParentBetComplete(string actionType, int betValue)
    {
        if (!isParent)
        {
            Debug.Log("OnlineGameManager - Not parent, skipping bet completion notification");
            return;
        }
        
        Debug.Log($"OnlineGameManager - Notifying parent bet completion: {actionType}, betValue: {betValue}");
        
        // 通知用のJSONデータを作成
        var notificationData = new ParentBetCompleteRequest
        {
            roomCode = gameData.roomCode,
            playerId = playerId,
            betAction = actionType,
            betAmount = betValue
        };
        
        string jsonBody = JsonUtility.ToJson(notificationData);
        Debug.Log($"OnlineGameManager - Parent bet completion notification JSON: {jsonBody}");
        
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
            Debug.Log($"OnlineGameManager - Parent bet completion notification successful: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"OnlineGameManager - Parent bet completion notification failed: {request.error}");
        }
    }

    // ベットアクション成功時の処理
    private void OnBetActionSuccess(string response)
    {
        Debug.Log($"OnlineGameManager - Bet action successful: {response}");
        
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
        Debug.LogError($"OnlineGameManager - Bet action failed: {error}");
        // エラー時の処理（例：ボタンを再度有効化）
        if (panelManager != null)
        {
            panelManager.SetBettingButtonInteractable(true);
        }
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
            Debug.Log("OnlineGameManager - Game phase changed to reveal from bet action");
            HandleGamePhaseChange("reveal");
        }
    }

    // 相手のアクションを処理
    private void HandleOpponentAction(OpponentAction action)
    {
        Debug.Log($"OnlineGameManager - Handling opponent action: {action.actionType}, betValue: {action.betValue}");
        
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

        // デバッグ用：カードセットを有効化
        canSetCard = true;
        Debug.Log("OnlineGameManager - Card set enabled for testing in Start()");

        // 親子システムの初期化
        InitializeParentChildSystem();

        // HIGH/LOW表示をクリア
        if (panelManager != null)
        {
            panelManager.ClearHighLowDisplay();
        }

        // マッチング完了パネル表示後にゲームフェーズ監視を開始
        if (panelManager != null && gameData != null)
        {
            string playerName = gameData.playerId;
            string opponentName = gameData.opponentId;
            Debug.Log($"[MatchStartPanel] playerName: {playerName}, opponentName: {opponentName}");
            
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
        Debug.Log($"OnlineGameManager - StartGamePhaseMonitoring called, current coroutine: {gamePhaseMonitorCoroutine != null}");
        
        if (gamePhaseMonitorCoroutine != null)
        {
            StopCoroutine(gamePhaseMonitorCoroutine);
            Debug.Log("OnlineGameManager - Stopped existing phase monitoring coroutine");
        }
        
        gamePhaseMonitorCoroutine = StartCoroutine(MonitorGamePhase());
        Debug.Log("OnlineGameManager - Started new phase monitoring coroutine");
    }

    // ゲームフェーズを定期的に監視
    private IEnumerator MonitorGamePhase()
    {
        Debug.Log("OnlineGameManager - Starting game phase monitoring coroutine");
        Debug.Log($"OnlineGameManager - MonitorGamePhase: Initial flags - isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}");
        
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // 0.1秒ごとに状態確認（短縮）
            
            // セット完了フェーズになったら監視を継続（Betting Phaseへの遷移のため）
            if (isSetCompletePhaseActive)
            {
                Debug.Log($"OnlineGameManager - Set Complete phase active, continuing monitoring: isSetCompletePhaseActive={isSetCompletePhaseActive}");
                Debug.Log($"OnlineGameManager - MonitorGamePhase: Current flags during set complete - isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}");
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
            if (!string.IsNullOrEmpty(currentGameId) && !string.IsNullOrEmpty(currentPlayerId))
            {
                Debug.Log($"OnlineGameManager - MonitorGamePhase: Checking game phase for gameId: {currentGameId}, playerId: {currentPlayerId}");
                Debug.Log($"OnlineGameManager - MonitorGamePhase: Current flags before phase check - isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}");
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
        Debug.Log($"OnlineGameManager - HandleGamePhaseChange called with newPhase: {newPhase}");
        Debug.Log($"OnlineGameManager - Current state before change: isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}");
        Debug.Log($"OnlineGameManager - canSetCard: {canSetCard}");
        
        // フェーズテキストを更新
        if (panelManager != null)
        {
            panelManager.UpdatePhaseText(newPhase);
            Debug.Log($"OnlineGameManager - Phase text updated to: {newPhase}");
        }
        else
        {
            Debug.LogWarning("OnlineGameManager - panelManager is null in HandleGamePhaseChange");
        }
        
        // 現在のフェーズを記録
        string currentPhase = isSetPhaseActive ? "set_phase" : (isBettingPhaseActive ? "betting" : (isRevealPhaseActive ? "reveal" : (isSetCompletePhaseActive ? "set_complete" : "unknown")));
        Debug.Log($"OnlineGameManager - Current phase string: {currentPhase}");
        
        // 同じフェーズの場合は処理をスキップ
        if (currentPhase == newPhase)
        {
            Debug.Log($"OnlineGameManager - Phase {newPhase} already active, skipping phase change");
            return;
        }
        
        // フェーズ遷移の順序をチェック
        if (newPhase == "reveal" && !isBettingPhaseActive)
        {
            Debug.LogWarning("OnlineGameManager - Skipping reveal phase, betting phase not completed yet");
            return;
        }
        
        Debug.Log($"OnlineGameManager - Processing phase change from {currentPhase} to {newPhase}");
        
        switch (newPhase)
        {
            case "set_phase":
                Debug.Log("OnlineGameManager - Processing set_phase case");
                if (!isSetPhaseActive)
                {
                    Debug.Log("OnlineGameManager - Activating set phase");
                    isSetPhaseActive = true;
                    isRevealPhaseActive = false;
                    isBettingPhaseActive = false;
                    isSetCompletePhaseActive = false;
                    canSetCard = false;
                    Debug.Log("OnlineGameManager - Set phase flags updated: isSetPhaseActive=true, canSetCard=false");
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
                Debug.Log("OnlineGameManager - Processing card_placement case");
                if (isSetPhaseActive)
                {
                    Debug.Log("OnlineGameManager - Transitioning from set_phase to card_placement");
                    isSetPhaseActive = false;
                    canSetCard = true;
                    Debug.Log("OnlineGameManager - Card placement flags updated: isSetPhaseActive=false, canSetCard=true");
                    if (panelManager != null)
                    {
                        panelManager.HideStartPhasePanel();
                        Debug.Log("OnlineGameManager - Set Phase ended, card placement enabled");
                        Debug.Log("OnlineGameManager - Lambda function: card_placement phase activated");
                    }
                }
                else
                {
                    // set_phaseをスキップして直接card_placementになった場合
                    Debug.Log("OnlineGameManager - Direct card_placement phase activated (set_phase was skipped)");
                    canSetCard = true;
                    Debug.Log("OnlineGameManager - Direct card_placement: canSetCard=true");
                }
                break;

            case "betting":
                Debug.Log($"OnlineGameManager - Processing betting case, isBettingPhaseActive: {isBettingPhaseActive}");
                Debug.Log($"OnlineGameManager - Current flags before betting transition: isSetPhaseActive={isSetPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}, canSetCard={canSetCard}");
                
                if (!isBettingPhaseActive)
                {
                    Debug.Log("OnlineGameManager - Activating Betting Phase");
                    isBettingPhaseActive = true;
                    isSetPhaseActive = false;
                    isSetCompletePhaseActive = false;
                    canSetCard = false;
                    Debug.Log("OnlineGameManager - Betting phase flags updated: isBettingPhaseActive=true, isSetPhaseActive=false, isSetCompletePhaseActive=false, canSetCard=false");
                    
                    // 親子システムの初期化
                    InitializeParentChildSystem();
                    
                    // 相手のカードが表示されているか確認し、必要に応じて表示
                    EnsureOpponentCardDisplayed();
                    
                    // 親のターンを開始
                    StartParentTurn();
                    
                    if (panelManager != null)
                    {
                        panelManager.ShowBettingPhasePanel();
                        Debug.Log("OnlineGameManager - Betting Phase started");
                        Debug.Log("OnlineGameManager - Lambda function: betting phase activated");
                        
                        // Betting Phase開始後にフェーズ監視を再開（重複を防ぐ）
                        if (gamePhaseMonitorCoroutine == null)
                        {
                            Debug.Log("OnlineGameManager - Restarting phase monitoring for betting phase");
                            StartGamePhaseMonitoring();
                        }
                        else
                        {
                            Debug.Log("OnlineGameManager - Phase monitoring already active, skipping restart");
                        }
                    }
                    else
                    {
                        Debug.LogError("OnlineGameManager - panelManager is null in betting case");
                    }
                }
                else
                {
                    Debug.Log("OnlineGameManager - Betting phase already active, skipping");
                }
                break;

            case "reveal":
                Debug.Log("OnlineGameManager - Processing reveal case");
                if (!isRevealPhaseActive)
                {
                    Debug.Log("OnlineGameManager - Activating reveal phase");
                    isRevealPhaseActive = true;
                    isSetPhaseActive = false;
                    isBettingPhaseActive = false;
                    isSetCompletePhaseActive = false;
                    canSetCard = false;
                    Debug.Log("OnlineGameManager - Reveal phase flags updated: isRevealPhaseActive=true, others=false");
                    
                    if (panelManager != null)
                    {
                        panelManager.ShowRevealPhasePanel();
                        Debug.Log("OnlineGameManager - Reveal Phase started");
                        Debug.Log("OnlineGameManager - Lambda function: reveal phase activated");
                    }
                }
                else
                {
                    Debug.Log("OnlineGameManager - Reveal phase already active, skipping");
                }
                break;

            case "open_phase":
                Debug.Log("OnlineGameManager - Processing open_phase case");
                if (panelManager != null)
                {
                    // HIGH/LOW表示をクリア
                    panelManager.ClearHighLowDisplay();
                    Debug.Log("OnlineGameManager - HIGH/LOW display cleared for open phase");
                    
                    panelManager.ShowOpenPhasePanel();
                    Debug.Log("OnlineGameManager - Open Phase started");
                    Debug.Log("OnlineGameManager - Lambda function: open_phase activated");
                }
                else
                {
                    Debug.LogError("OnlineGameManager - panelManager is null in open_phase case");
                }
                break;
                
            default:
                Debug.LogWarning($"OnlineGameManager - Unknown game phase: {newPhase}");
                break;
        }
        
        Debug.Log($"OnlineGameManager - HandleGamePhaseChange completed. Final state: isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}, isSetCompletePhaseActive={isSetCompletePhaseActive}, canSetCard={canSetCard}");
    }

    // カードセット可能かどうかを取得
    public bool CanSetCard()
    {
        Debug.Log($"OnlineGameManager - CanSetCard called, returning: {canSetCard}");
        return canSetCard;
    }

    // デバッグ用：カードセット可能フラグを強制的に有効化
    public void EnableCardSetForTesting()
    {
        canSetCard = true;
        Debug.Log("OnlineGameManager - Card set enabled for testing");
    }

    // カード配置確認パネルを表示
    public void ShowConfirmation(CardDisplay card, OnlineDropZone zone)
    {
        Debug.Log($"OnlineGameManager - ShowConfirmation called for card: {card.CardValue}");
        currentCard = card;
        currentZone = zone;
        
        if (panelManager != null)
        {
            Debug.Log($"OnlineGameManager - panelManager found, yesButton: {panelManager.yesButton != null}");
        panelManager.confirmationPanel.SetActive(true);

        // リスナーの多重登録防止
        panelManager.yesButton.onClick.RemoveAllListeners();
        panelManager.noButton.onClick.RemoveAllListeners();

        panelManager.yesButton.onClick.AddListener(ConfirmPlacement);
        panelManager.noButton.onClick.AddListener(CancelPlacement);
            
            Debug.Log("OnlineGameManager - Button listeners added successfully");
        }
        else
        {
            Debug.LogError("OnlineGameManager - panelManager is null!");
        }
    }

    // カード配置を確定
    public void ConfirmPlacement()
    {
        Debug.Log("OnlineGameManager - ConfirmPlacement method entered!");
        Debug.Log($"OnlineGameManager - ConfirmPlacement called for card: {currentCard?.CardValue}");
        
        if (currentCard != null && currentZone != null)
        {
            // カードをDropZoneの子にする（親子関係をセット）
            currentCard.transform.SetParent(currentZone.transform);
            currentCard.transform.localPosition = Vector3.zero;

            // 配置されたカードを記録
            setPlayerCard = currentCard;

            // 確認パネルを非表示
            if (panelManager != null)
            {
                panelManager.confirmationPanel.SetActive(false);
            }
            
            // カード配置完了をサーバーに通知
            StartCoroutine(NotifyCardPlacement(currentCard.CardValue));
        }
        else
        {
            Debug.LogError("OnlineGameManager - currentCard or currentZone is null!");
        }

        ResetPlacementState();
    }

    // カード配置をキャンセル
    public void CancelPlacement()
    {
        Debug.Log("OnlineGameManager - CancelPlacement called");
        
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

    // サーバーにカード配置を通知
    private IEnumerator NotifyCardPlacement(int cardValue)
    {
        Debug.Log($"OnlineGameManager - NotifyCardPlacement called with cardValue: {cardValue}");
        Debug.Log($"OnlineGameManager - currentGameId: {currentGameId}, currentPlayerId: {currentPlayerId}");
        
        // SetCard APIを呼び出してカード配置を通知
        SetCard(currentGameId, currentPlayerId, cardValue, OnCardPlacementSuccess, OnCardPlacementError);
        
        Debug.Log($"OnlineGameManager - SetCard API call completed");
        
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
        Debug.Log($"OnlineGameManager - SetCard API call started");
        Debug.Log($"OnlineGameManager - SetCard method entered with gameId: {gameId}, playerId: {playerId}, cardValue: {cardValue}");
        Debug.Log($"OnlineGameManager - This will update Player{(playerId == gameData.playerId ? "1" : "2")}Set");
        
        string url = $"{HttpManager.ApiBaseUrl}/update-state";
        string jsonBody = JsonUtility.ToJson(new SetCardRequest
        {
            gameId = gameId,
            playerId = playerId,
            cardValue = cardValue
        });
        
        Debug.Log($"OnlineGameManager - Calling set-card API: {url}, body: {jsonBody}");
        Debug.Log($"OnlineGameManager - About to call HttpManager.Post with onSuccess and onError callbacks");
        
        HttpManager.Instance.Post<SetCardResponse>(
            url,
            jsonBody,
            (response) => {
                Debug.Log($"OnlineGameManager - SetCard API success callback triggered");
                string responseJson = JsonUtility.ToJson(response);
                Debug.Log($"OnlineGameManager - Response JSON: {responseJson}");
                onSuccess?.Invoke(responseJson);
            },
            (error) => {
                Debug.LogError($"OnlineGameManager - SetCard API error callback triggered: {error}");
                onError?.Invoke(error);
            }
        );
        
        Debug.Log($"OnlineGameManager - SetCard method completed, API call initiated");
    }

    // カード配置成功時のコールバック
    private void OnCardPlacementSuccess(string response)
    {
        Debug.Log($"OnlineGameManager - OnCardPlacementSuccess method entered with response: {response}");
        
        try
        {
            // レスポンスをパース
            var setCardResponse = JsonUtility.FromJson<SetCardResponse>(response);
            Debug.Log($"OnlineGameManager - Successfully parsed response. GameId: {setCardResponse.gameId}, GamePhase: {setCardResponse.gamePhase}");
            Debug.Log($"OnlineGameManager - Player1Set: {setCardResponse.player1Set}, Player2Set: {setCardResponse.player2Set}");
            Debug.Log($"OnlineGameManager - Player1CardValue: {setCardResponse.player1CardValue}, Player2CardValue: {setCardResponse.player2CardValue}");
            Debug.Log($"OnlineGameManager - Player1CardPlaced: {setCardResponse.player1CardPlaced}, Player2CardValue: {setCardResponse.player2CardValue}");
            
            // カード配置後はセット不可
            canSetCard = false;
            Debug.Log("OnlineGameManager - Set canSetCard to false");
            
            // 両者セット済みかチェック
            Debug.Log($"OnlineGameManager - Checking both players set condition: player1Set={setCardResponse.player1Set}, player2Set={setCardResponse.player2Set}");
            Debug.Log($"OnlineGameManager - Current flags: isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}");
            
            if (setCardResponse.player1Set && setCardResponse.player2Set)
            {
                Debug.Log("OnlineGameManager - Both players have set their cards!");
                Debug.Log($"OnlineGameManager - About to start Set Complete Phase. Current flags: isSetCompletePhaseActive={isSetCompletePhaseActive}");
                
                // セット完了フェーズを開始（重複実行を防ぐ）
                if (!isSetCompletePhaseActive)
                {
                    Debug.Log("OnlineGameManager - Starting Set Complete Phase");
                    StartCoroutine(HandleSetCompletePhase(setCardResponse.player1CardValue, setCardResponse.player2CardValue));
                }
                else
                {
                    Debug.LogWarning("OnlineGameManager - Set Complete Phase already active, skipping duplicate start");
                }
            }
            else
            {
                Debug.Log($"OnlineGameManager - Waiting for opponent. Player1Set: {setCardResponse.player1Set}, Player2Set: {setCardResponse.player2Set}");
                Debug.Log($"OnlineGameManager - Current flags while waiting: isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"OnlineGameManager - Error parsing card placement response: {e.Message}");
            Debug.LogError($"OnlineGameManager - Full exception: {e}");
        }
    }

    // セット完了フェーズの処理
    private IEnumerator HandleSetCompletePhase(int player1CardValue, int player2CardValue)
    {
        Debug.Log($"OnlineGameManager - HandleSetCompletePhase started with player1CardValue: {player1CardValue}, player2CardValue: {player2CardValue}");
        Debug.Log($"OnlineGameManager - HandleSetCompletePhase: Current flags before starting - isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isSetPhaseActive={isSetPhaseActive}");
        Debug.Log($"OnlineGameManager - HandleSetCompletePhase: isSetCompletePhaseActive will be set to true");
        
        isSetCompletePhaseActive = true;
        Debug.Log($"OnlineGameManager - HandleSetCompletePhase: isSetCompletePhaseActive set to {isSetCompletePhaseActive}");
        
        // 相手のカードを裏向きで表示
        DisplayOpponentCardFaceDown(player2CardValue);
        
        // HIGH/LOW表示を更新
        if (panelManager != null)
        {
            // 自分のカードのHIGH/LOW表示を更新
            if (isPlayer1)
            {
                // 2枚のカードの値を取得
                int playerCard1 = gameData.player1Cards[0];
                int playerCard2 = gameData.player1Cards[1];
                int opponentCard1 = gameData.player2Cards[0];
                int opponentCard2 = gameData.player2Cards[1];
                
                panelManager.UpdatePlayerHighLowDisplay(playerCard1, playerCard2);
                panelManager.UpdateOpponentHighLowDisplay(opponentCard1, opponentCard2);
            }
            else
            {
                // 2枚のカードの値を取得
                int playerCard1 = gameData.player2Cards[0];
                int playerCard2 = gameData.player2Cards[1];
                int opponentCard1 = gameData.player1Cards[0];
                int opponentCard2 = gameData.player1Cards[1];
                
                panelManager.UpdatePlayerHighLowDisplay(playerCard1, playerCard2);
                panelManager.UpdateOpponentHighLowDisplay(opponentCard1, opponentCard2);
            }
        }
        
        // セット完了パネルを表示
        if (panelManager != null)
        {
            panelManager.ShowSetCompletePanel();
            Debug.Log("OnlineGameManager - HandleSetCompletePhase: Set Complete Panel shown");
        }
        else
        {
            Debug.LogWarning("OnlineGameManager - HandleSetCompletePhase: panelManager is null, cannot show Set Complete Panel");
        }
        
        // セット完了パネルを3秒間表示
        Debug.Log("OnlineGameManager - HandleSetCompletePhase: Waiting 3 seconds before hiding panel");
        yield return new WaitForSeconds(3f);
        
        // セット完了パネルを非表示
        if (panelManager != null)
        {
            panelManager.HideSetCompletePanel();
            Debug.Log("OnlineGameManager - HandleSetCompletePhase: Set Complete Panel hidden");
        }
        
        // Betting Phaseに遷移
        Debug.Log("OnlineGameManager - HandleSetCompletePhase: About to transition to Betting Phase");
        Debug.Log($"OnlineGameManager - HandleSetCompletePhase: Current flags before transition - isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isSetPhaseActive={isSetPhaseActive}");
        
        Debug.Log("OnlineGameManager - HandleSetCompletePhase: Calling HandleGamePhaseChange('betting')");
        HandleGamePhaseChange("betting");
        Debug.Log("OnlineGameManager - HandleSetCompletePhase: HandleGamePhaseChange('betting') completed");
        
        // フェーズ遷移後の状態を確認
        Debug.Log($"OnlineGameManager - HandleSetCompletePhase: Flags after transition - isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isSetPhaseActive={isSetPhaseActive}");
        
        // isSetCompletePhaseActiveをfalseに設定
        Debug.Log("OnlineGameManager - HandleSetCompletePhase: Setting isSetCompletePhaseActive to false");
        isSetCompletePhaseActive = false;
        Debug.Log($"OnlineGameManager - HandleSetCompletePhase: Final flags - isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isSetPhaseActive={isSetPhaseActive}");
        
        Debug.Log("OnlineGameManager - HandleSetCompletePhase completed");
    }

    // 相手のカードを裏向きで表示
    private void DisplayOpponentCardFaceDown(int cardValue)
    {
        Debug.Log($"OnlineGameManager - DisplayOpponentCardFaceDown called with cardValue: {cardValue}");
        
        // 相手側のDropZoneを探す
        OnlineDropZone opponentZone = FindOpponentDropZone();
        
        if (opponentZone != null)
        {
            // カードプレハブを生成して相手側のDropZoneに配置
            CreateOpponentCard(cardValue, opponentZone);
            Debug.Log($"OnlineGameManager - Opponent card {cardValue} displayed face down in opponent zone");
        }
        else
        {
            Debug.LogError("OnlineGameManager - Could not find opponent DropZone");
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
                Debug.Log($"OnlineGameManager - Found opponent DropZone: {zone.name}");
                return zone;
            }
        }
        
        Debug.LogWarning("OnlineGameManager - No opponent DropZone found");
        return null;
    }

    // 相手のカードを生成して配置
    private void CreateOpponentCard(int cardValue, OnlineDropZone opponentZone)
    {
        // カードプレハブを確認
        if (cardPrefab == null)
        {
            Debug.LogError("OnlineGameManager - cardPrefab is null! Please assign it in the Inspector.");
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
            
            Debug.Log($"OnlineGameManager - Opponent card {cardValue} created and placed in opponent zone");
        }
        else
        {
            Debug.LogError("OnlineGameManager - CardDisplay component not found on card prefab");
        }
    }

    // 相手のカードが確実に表示されているか確認
    private void EnsureOpponentCardDisplayed()
    {
        Debug.Log("OnlineGameManager - Ensuring opponent card is displayed");
        
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
                    Debug.Log($"OnlineGameManager - No opponent card found, creating one with value: {opponentCardValue}");
                    CreateOpponentCard(opponentCardValue, opponentZone);
                }
                else
                {
                    Debug.LogWarning("OnlineGameManager - gameData is null, cannot determine opponent card value");
                }
            }
            else
            {
                Debug.Log("OnlineGameManager - Opponent card already displayed");
            }
        }
        else
        {
            Debug.LogError("OnlineGameManager - Could not find opponent DropZone for card display check");
        }
    }

    // 相手のカードを表向きで表示
    private void DisplayOpponentCardFaceUp(int cardValue)
    {
        Debug.Log($"OnlineGameManager - DisplayOpponentCardFaceUp called with cardValue: {cardValue}");
        // TODO: 相手のSetZoneのカードを表向きにする
    }

    // カード配置エラー時のコールバック
    private void OnCardPlacementError(string error)
    {
        Debug.LogError($"OnlineGameManager - OnCardPlacementError method entered with error: {error}");
        Debug.LogError($"OnlineGameManager - Card placement failed: {error}");
        // エラーの場合はカードを元の位置に戻す
        Debug.Log("OnlineGameManager - Calling CancelPlacement due to error");
        CancelPlacement();
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
