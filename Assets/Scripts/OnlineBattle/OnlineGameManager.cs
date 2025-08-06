using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System;
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
        
        while (true)
        {
            yield return new WaitForSeconds(0.1f); // 0.1秒ごとに状態確認（短縮）
            
            // セット完了フェーズになったら監視を継続（Betting Phaseへの遷移のため）
            if (isSetCompletePhaseActive)
            {
                Debug.Log($"OnlineGameManager - Set Complete phase active, continuing monitoring: isSetCompletePhaseActive={isSetCompletePhaseActive}");
                yield return new WaitForSeconds(0.1f);
                continue;
            }
            
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
        Debug.Log($"OnlineGameManager - Current state: isSetPhaseActive={isSetPhaseActive}, isBettingPhaseActive={isBettingPhaseActive}, isRevealPhaseActive={isRevealPhaseActive}");
        
        // フェーズテキストを更新
        if (panelManager != null)
        {
            panelManager.UpdatePhaseText(newPhase);
        }
        
        // 現在のフェーズを記録
        string currentPhase = isSetPhaseActive ? "set_phase" : (isBettingPhaseActive ? "betting" : (isRevealPhaseActive ? "reveal" : "unknown"));
        
        // 同じフェーズの場合は処理をスキップ
        if (currentPhase == newPhase)
        {
            Debug.Log($"OnlineGameManager - Phase {newPhase} already active, skipping");
            return;
        }
        
        // フェーズ遷移の順序をチェック
        if (newPhase == "reveal" && !isBettingPhaseActive)
        {
            Debug.Log("OnlineGameManager - Skipping reveal phase, betting phase not completed yet");
            return;
        }
        
        switch (newPhase)
        {
            case "set_phase":
                Debug.Log("OnlineGameManager - Processing set_phase case");
                if (!isSetPhaseActive)
                {
                    isSetPhaseActive = true;
                    isRevealPhaseActive = false;
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
                else
                {
                    // set_phaseをスキップして直接card_placementになった場合
                    canSetCard = true;
                    Debug.Log("OnlineGameManager - Direct card_placement phase activated");
                }
                break;

            case "betting":
                Debug.Log($"OnlineGameManager - Processing betting case, isBettingPhaseActive: {isBettingPhaseActive}");
                
                if (!isBettingPhaseActive)
                {
                    Debug.Log("OnlineGameManager - Activating Betting Phase");
                    isBettingPhaseActive = true;
                    isSetPhaseActive = false;
                    canSetCard = false;
                    
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
                if (!isRevealPhaseActive)
                {
                    isRevealPhaseActive = true;
                    isSetPhaseActive = false;
                    canSetCard = false;
                    
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
                
            default:
                Debug.Log($"OnlineGameManager - Unknown game phase: {newPhase}");
                break;
        }
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
            // Debug.Log("OnlineGameManager - Attempting to parse response JSON");
            // レスポンスをパース
            var setCardResponse = JsonUtility.FromJson<SetCardResponse>(response);
            // Debug.Log($"OnlineGameManager - Successfully parsed response. GameId: {setCardResponse.gameId}, GamePhase: {setCardResponse.gamePhase}");
            // Debug.Log($"OnlineGameManager - Player1Set: {setCardResponse.player1Set}, Player2Set: {setCardResponse.player2Set}");
            // Debug.Log($"OnlineGameManager - Player1CardValue: {setCardResponse.player1CardValue}, Player2CardValue: {setCardResponse.player2CardValue}");
            // Debug.Log($"OnlineGameManager - Player1CardPlaced: {setCardResponse.player1CardPlaced}, Player2CardPlaced: {setCardResponse.player2CardPlaced}");
            
            // カード配置後はセット不可
            canSetCard = false;
            Debug.Log("OnlineGameManager - Set canSetCard to false");
            
            // 両者セット済みかチェック
            // Debug.Log($"OnlineGameManager - Checking both players set condition: player1Set={setCardResponse.player1Set}, player2Set={setCardResponse.player2Set}");
            // Debug.Log($"OnlineGameManager - Current flags: isSetCompletePhaseActive={isSetCompletePhaseActive}, isBettingPhaseActive={isBettingPhaseActive}");
            
            if (setCardResponse.player1Set && setCardResponse.player2Set)
            {
                // Debug.Log("OnlineGameManager - Both players have set their cards!");
                
                // セット完了フェーズを開始（重複実行を防ぐ）
                if (!isSetCompletePhaseActive)
                {
                    Debug.Log("OnlineGameManager - Starting Set Complete Phase");
                    StartCoroutine(HandleSetCompletePhase(setCardResponse.player1CardValue, setCardResponse.player2CardValue));
                }
                else
                {
                    Debug.Log("OnlineGameManager - Set Complete Phase already active, skipping");
                }
            }
            else
            {
                Debug.Log($"OnlineGameManager - Waiting for opponent. Player1Set: {setCardResponse.player1Set}, Player2Set: {setCardResponse.player2Set}");
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
        Debug.Log($"OnlineGameManager - HandleSetCompletePhase: isSetCompletePhaseActive will be set to true");
        
        isSetCompletePhaseActive = true;
        
        // 相手のカードを裏向きで表示
        DisplayOpponentCardFaceDown(player2CardValue);
        
        // セット完了パネルを表示
        if (panelManager != null)
        {
            panelManager.ShowSetCompletePanel();
        }
        
        // セット完了パネルを3秒間表示
        yield return new WaitForSeconds(3f);
        
        // セット完了パネルを非表示
        if (panelManager != null)
        {
            panelManager.HideSetCompletePanel();
        }
        
        // // 相手のカードを表向きにする
        // DisplayOpponentCardFaceUp(player2CardValue);
        
        // Debug.Log("OnlineGameManager - About to transition to Betting Phase");
        
        // Betting Phaseに遷移
        Debug.Log("OnlineGameManager - About to call HandleGamePhaseChange('betting')");
        HandleGamePhaseChange("betting");
        
        // Debug.Log("OnlineGameManager - HandleGamePhaseChange('betting') completed");
        
        // Debug.Log("OnlineGameManager - Setting isSetCompletePhaseActive to false");
        isSetCompletePhaseActive = false;
        
        Debug.Log("OnlineGameManager - HandleSetCompletePhase completed");
    }

    // 相手のカードを裏向きで表示
    private void DisplayOpponentCardFaceDown(int cardValue)
    {
        Debug.Log($"OnlineGameManager - DisplayOpponentCardFaceDown called with cardValue: {cardValue}");
        // TODO: 相手のSetZoneに裏向きカードを配置
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

    public void PlaceBet(int amount) 
    { 
        Debug.Log($"OnlineGameManager - PlaceBet called with amount: {amount}");
        CurrentBetAmount = amount;
        // TODO: ベット額をサーバーに通知
    }
    
    public int CurrentBetAmount { get; private set; }
    
    public void Call() 
    { 
        Debug.Log("OnlineGameManager - Call called");
        // TODO: Call処理を実装
    }
    
    public void Drop() 
    { 
        Debug.Log("OnlineGameManager - Drop called");
        // TODO: Drop処理を実装
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
