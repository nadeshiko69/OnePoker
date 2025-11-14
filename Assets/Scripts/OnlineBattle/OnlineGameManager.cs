using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;
using OnePoker.Network;

/// <summary>
/// オンライン対戦のメインマネージャークラス（リファクタリング版）
/// 各専用マネージャーを統合して管理
/// </summary>
public class OnlineGameManager : MonoBehaviour
{
    public enum SkillType { Scan, Change, Obstruct, FakeOut, Copy, None }
    
    [Header("UI References")]
    public TextMeshProUGUI playerLifeText;
    public TextMeshProUGUI opponentLifeText;

    [Header("Card Prefab")]
    public GameObject cardPrefab;
    
    // 各マネージャー
    private OnlinePhaseManager phaseManager;
    private OnlineCardPlacementManager cardPlacementManager;
    private OnlineBettingManager bettingManager;
    private OnlineGameDataProvider gameDataProvider;
    private OnlineCardDisplayManager cardDisplayManager;
    private OnlineBettingTurnManager bettingTurnManager;
    
    // その他のマネージャー
    private OnlineMatchManager matchManager;
    private OnlineResultViewManager resultViewManager;
    private OnlinePanelManager panelManager;
    private OnlineSkillManager skillManager;
    private OnlineHandManager handManager;

    void Start()
    {
        Debug.Log("[OnlineGameManager] Start called");
        
        // マネージャーの初期化
        InitializeManagers();
        
        // ゲームデータの読み込み
        if (gameDataProvider.LoadGameData())
        {
            // 手札の設定
            SetupHands();
            
            // ライフ表示の初期化
            UpdateLifeDisplay();
            
            // HIGH/LOW表示の更新
            UpdateHighLowDisplay();
            
            // 初期化完了処理
            CompleteInitialization();
            
            // ゲーム開始
            StartGame();
        }
        else
        {
            Debug.LogError("[OnlineGameManager] Failed to load game data");
        }
    }
    
    /// <summary>
    /// 各マネージャーを初期化
    /// </summary>
    private void InitializeManagers()
    {
        Debug.Log("[OnlineGameManager] Initializing managers");
        
        // 既存のマネージャーを取得
        matchManager = FindObjectOfType<OnlineMatchManager>();
        resultViewManager = FindObjectOfType<OnlineResultViewManager>();
        panelManager = FindObjectOfType<OnlinePanelManager>();
        skillManager = FindObjectOfType<OnlineSkillManager>();
        handManager = FindObjectOfType<OnlineHandManager>();
        
        // 新しいマネージャーを取得または作成
        gameDataProvider = GetOrAddComponent<OnlineGameDataProvider>();
        phaseManager = GetOrAddComponent<OnlinePhaseManager>();
        cardPlacementManager = GetOrAddComponent<OnlineCardPlacementManager>();
        bettingManager = GetOrAddComponent<OnlineBettingManager>();
        cardDisplayManager = GetOrAddComponent<OnlineCardDisplayManager>();
        bettingTurnManager = GetOrAddComponent<OnlineBettingTurnManager>();
        
        // 各マネージャーを初期化
        phaseManager.Initialize(panelManager, gameDataProvider);
        cardPlacementManager.Initialize(panelManager, handManager, gameDataProvider);
        bettingManager.Initialize(panelManager, gameDataProvider);
        cardDisplayManager.Initialize(gameDataProvider);
        bettingTurnManager.Initialize(gameDataProvider, bettingManager, panelManager);
        
        // cardPrefabを設定
        if (cardDisplayManager != null && cardPrefab != null)
        {
            cardDisplayManager.cardPrefab = cardPrefab;
        }
        
        // イベント登録
        RegisterEvents();
    }
    
    /// <summary>
    /// コンポーネントを取得または追加
    /// </summary>
    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
    
    /// <summary>
    /// イベントを登録
    /// </summary>
    private void RegisterEvents()
    {
        // フェーズ変更イベント
        phaseManager.OnSetPhaseStarted += OnSetPhaseStarted;
        phaseManager.OnBettingPhaseStarted += OnBettingPhaseStarted;
        phaseManager.OnRevealPhaseStarted += OnRevealPhaseStarted;
        
        // カード配置イベント
        cardPlacementManager.OnBothPlayersPlaced += OnBothPlayersPlaced;
        
        // ベッティングイベント
        bettingManager.OnBetActionExecuted += OnBetActionExecuted;
    }
    
    /// <summary>
    /// 手札を設定
    /// </summary>
    private void SetupHands()
    {
        Debug.Log("[OnlineGameManager] Setting up hands");
        
        if (handManager != null)
        {
            var myCards = gameDataProvider.MyCards;
            var opponentCards = gameDataProvider.OpponentCards;
            
            Debug.Log($"[OnlineGameManager] MyCards: {(myCards != null ? string.Join(",", myCards) : "null")}");
            Debug.Log($"[OnlineGameManager] OpponentCards: {(opponentCards != null ? string.Join(",", opponentCards) : "null")}");
            
            handManager.SetPlayerHand(myCards);
            handManager.SetOpponentHand(opponentCards);
        }
        else
        {
            Debug.LogError("[OnlineGameManager] handManager is null!");
        }
    }
    
    /// <summary>
    /// HIGH/LOW表示を更新
    /// </summary>
    private void UpdateHighLowDisplay()
    {
        Debug.Log("[OnlineGameManager] Updating HIGH/LOW display");
        
        if (panelManager != null)
        {
            var myCards = gameDataProvider.MyCards;
            var opponentCards = gameDataProvider.OpponentCards;
            
            if (myCards != null && myCards.Length >= 2 && opponentCards != null && opponentCards.Length >= 2)
            {
                panelManager.UpdatePlayerHighLowDisplay(myCards[0], myCards[1]);
                panelManager.UpdateOpponentHighLowDisplay(opponentCards[0], opponentCards[1]);
            }
        }
    }
    
    /// <summary>
    /// 初期化完了処理
    /// </summary>
    private void CompleteInitialization()
    {
        Debug.Log("[OnlineGameManager] Completing initialization");

        // ライフUI初期化
        UpdateLifeUI();
        
        // 使用済スキル表示を初期化（ゲーム開始時のみ）
        if (panelManager != null)
        {
            panelManager.InitializeUsedSkillsDisplay();
        }
        
        if (resultViewManager != null)
        {
            resultViewManager.ResetResults();
            Debug.Log("[OnlineGameManager] ResultView reset for new game");
        }
        
        // カードセットを有効化
        cardPlacementManager.EnableCardPlacement();
    }
    
    /// <summary>
    /// ゲーム開始
    /// </summary>
    private void StartGame()
    {
        Debug.Log("[OnlineGameManager] Starting game");
        
        if (panelManager != null && gameDataProvider.IsValid())
            {
                panelManager.UpdatePhaseText("set_phase");
            
            StartCoroutine(ShowMatchStartPanelAndStartMonitoring(
                gameDataProvider.PlayerId,
                gameDataProvider.OpponentId,
                3f
            ));
        }
    }
    
    /// <summary>
    /// マッチング完了パネル表示後にフェーズ監視を開始
    /// </summary>
    private IEnumerator ShowMatchStartPanelAndStartMonitoring(string playerName, string opponentName, float duration)
    {
        panelManager.ShowMatchStartPanel(playerName, opponentName, duration);
        yield return new WaitForSeconds(duration);
            
            // フェーズ移行時間を設定
            yield return StartCoroutine(SetPhaseTransitionTime());
            
        // フェーズ監視開始
        phaseManager.StartPhaseMonitoring();
    }
    
    /// <summary>
    /// フェーズ移行時間を設定
    /// </summary>
    private IEnumerator SetPhaseTransitionTime()
    {
        HttpManager.Instance.SetPhaseTransitionTime(
            gameDataProvider.GameId,
            gameDataProvider.PlayerId,
            3,
            (response) => Debug.Log($"[OnlineGameManager] Phase transition time set: {response}"),
            (error) => Debug.LogError($"[OnlineGameManager] Phase transition time error: {error}")
        );
        
        yield return new WaitForSeconds(0.1f);
    }

    /// <summary>
    /// ライフUIを更新
    /// </summary>
    public void UpdateLifeUI()
    {
        if (playerLifeText != null && matchManager != null)
            playerLifeText.text = $"Life: {matchManager.PlayerLife}";
        if (opponentLifeText != null && matchManager != null)
            opponentLifeText.text = $"Life: {matchManager.OpponentLife}";
    }

    // ========== イベントハンドラー ==========
    
    private void OnSetPhaseStarted()
    {
        Debug.Log("[OnlineGameManager] Set Phase started");
        cardPlacementManager.EnableCardPlacement();
        
        // スキルUIを表示
        if (skillManager != null)
        {
            skillManager.OnSetPhaseStarted();
        }

        // Bet値のUIを非表示にして、LifeのUIを表示（BetPhaseの逆処理）
        if (panelManager != null)
        {
            // betAmountTextを非表示
            if (panelManager.betAmountText != null) 
                panelManager.betAmountText.gameObject.SetActive(false);
            
            // playerLifeを表示
            if (panelManager.playerLife != null) 
                panelManager.playerLife.gameObject.SetActive(true);
            
            // 役割表示を更新
            bool isPlayerDealer = gameDataProvider.IsDealer;
            panelManager.UpdatePlayerRoleDisplay(isPlayerDealer);
            Debug.Log($"[OnlineGameManager] Role display updated on SetPhase - IsDealer: {isPlayerDealer}");
        }
    }
    
    private void OnBettingPhaseStarted()
    {
        Debug.Log("[OnlineGameManager] Betting Phase started");
        cardPlacementManager.DisableCardPlacement();
        cardDisplayManager.EnsureOpponentCardDisplayed();
        
        // スキルUIを非表示
        if (skillManager != null)
        {
            skillManager.OnNonSetPhaseStarted();
        }
        
        // Scanスキルで表向きになった相手の手札を裏向きに戻す
        if (handManager != null)
        {
            handManager.ResetOpponentCardsToFaceDown();
        }
        
        bettingTurnManager.StartTurnMonitoring();
    }
    
    private void OnRevealPhaseStarted()
    {
        Debug.Log("[OnlineGameManager] Reveal Phase started");
        
        // スキルUIを非表示
        if (skillManager != null)
        {
            skillManager.OnNonSetPhaseStarted();
        }
        
        // AWSから最新のゲーム状態を取得してカード値を更新
        FetchLatestGameStateForReveal();
        
        // 3秒後に次のラウンドに進む
        StartCoroutine(WaitAndProceedToNextRound());
    }
    
    /// <summary>
    /// Reveal後、3秒待ってから次のラウンドに進む
    /// </summary>
    private IEnumerator WaitAndProceedToNextRound()
    {
        Debug.Log("[OnlineGameManager] Waiting 3 seconds before next round...");
        yield return new WaitForSeconds(3f);
        
        Debug.Log("[OnlineGameManager] Proceeding to next round");
        StartNextRound();
    }
    
    /// <summary>
    /// 次のラウンドを開始
    /// </summary>
    private void StartNextRound()
    {
        Debug.Log("[OnlineGameManager] StartNextRound called");
        
        string gameId = gameDataProvider.GameId;
        string playerId = gameDataProvider.MyPlayerId;
        
        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogError("[OnlineGameManager] GameId is empty, cannot start next round");
            return;
        }
        
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[OnlineGameManager] PlayerId is empty, cannot start next round");
            return;
        }
        
        Debug.Log($"[OnlineGameManager] Starting next round - GameId: {gameId}, PlayerId: {playerId}");
        
        HttpManager.Instance.NextRound(
            gameId,
            playerId,
            OnNextRoundSuccess,
            OnNextRoundError
        );
    }
    
    /// <summary>
    /// 次のラウンド成功時のコールバック
    /// </summary>
    private void OnNextRoundSuccess(HttpManager.NextRoundResponse response)
    {
        Debug.Log($"[OnlineGameManager] Next round success: Round {response.currentRound}, Dealer: {response.currentDealer}");
        
        // ========== UIリセット処理 ==========
        
        // 1. 勝敗パネルを非表示にする
        if (panelManager != null)
        {
            if (panelManager.gameResultPanel != null)
            {
                panelManager.gameResultPanel.SetActive(false);
                Debug.Log("[OnlineGameManager] Game result panel hidden");
            }
        }
        
        // 2. ResultViewはリセットしない（結果を蓄積するため）
        Debug.Log("[OnlineGameManager] Result view preserved for multi-round display");
        
        // ========== ゲームデータ更新 ==========
        
        // カード情報を更新
        Debug.Log($"[OnlineGameManager] Updating game data with new cards - Player1: {string.Join(",", response.player1Cards)}, Player2: {string.Join(",", response.player2Cards)}");
        
        gameDataProvider.UpdateGameData(new OnlineGameDataProvider.OnlineGameDataWithCards
        {
            gameId = response.gameId,
            playerId = gameDataProvider.MyPlayerId,
            opponentId = gameDataProvider.OpponentId,
            isPlayer1 = gameDataProvider.IsPlayer1,
            roomCode = gameDataProvider.RoomCode,
            player1Cards = response.player1Cards,
            player2Cards = response.player2Cards,
            player1CardValue = null,
            player2CardValue = null,
            player1Life = response.player1Life,
            player2Life = response.player2Life,
            player1UsedSkills = new System.Collections.Generic.List<string>(),
            player2UsedSkills = new System.Collections.Generic.List<string>(),
            currentDealer = response.currentDealer
        });
        
        // 相手の手札が正しく更新されたか確認
        var myCardsAfterUpdate = gameDataProvider.MyCards;
        var opponentCardsAfterUpdate = gameDataProvider.OpponentCards;
        Debug.Log($"[OnlineGameManager] After update - MyCards: {(myCardsAfterUpdate != null ? string.Join(",", myCardsAfterUpdate) : "null")}, OpponentCards: {(opponentCardsAfterUpdate != null ? string.Join(",", opponentCardsAfterUpdate) : "null")}");
        
        // Dealer情報を更新
        gameDataProvider.UpdateCurrentDealer(response.currentDealer);
        
        // 役割表示を更新
        if (panelManager != null)
        {
            bool isPlayerDealer = gameDataProvider.IsDealer;
            panelManager.UpdatePlayerRoleDisplay(isPlayerDealer);
            Debug.Log($"[OnlineGameManager] Role display updated - IsDealer: {isPlayerDealer}");
        }
        
        // ライフ表示を更新
        UpdateLifeDisplay();
        
        // ========== 盤面リセット処理 ==========
        
        // 3. SetZone（DropZone）のカードを削除
        ClearSetZone();
        
        // 4. 手札を再設定（1枚補充）
        Debug.Log("[OnlineGameManager] About to call SetupHands");
        SetupHands();
        Debug.Log("[OnlineGameManager] SetupHands completed");
        
        // 5. スキルマネージャーをリセット
        if (skillManager != null)
        {
            skillManager.ResetUsedSkills();
        }
        
        // ========== フェーズ管理 ==========
        
        // 6. フェーズマネージャーを再起動
        phaseManager.StartPhaseMonitoring();
        
        Debug.Log($"[OnlineGameManager] Next round setup complete - Round {response.currentRound}");
        Debug.Log($"[OnlineGameManager] UI reset: Game result panel hidden, Result view reset, SetZone cleared, Hands refreshed");
    }
    
    /// <summary>
    /// 次のラウンド失敗時のコールバック
    /// </summary>
    private void OnNextRoundError(string error)
    {
        Debug.LogError($"[OnlineGameManager] Failed to start next round: {error}");
        // エラー通知をUIに表示
                    if (panelManager != null)
                    {
            panelManager.ShowOpponentUsedSkillNotification($"次のラウンド開始エラー: {error}");
        }
    }
    
    /// <summary>
    /// ライフ表示を更新
    /// </summary>
    private void UpdateLifeDisplay()
    {
        if (playerLifeText != null)
        {
            playerLifeText.text = $"Life: {gameDataProvider.MyLife}";
            Debug.Log($"[OnlineGameManager] Player life updated: {gameDataProvider.MyLife}");
        }
        
        if (opponentLifeText != null)
        {
            opponentLifeText.text = $"Life: {gameDataProvider.OpponentLife}";
            Debug.Log($"[OnlineGameManager] Opponent life updated: {gameDataProvider.OpponentLife}");
        }
    }
    
    /// <summary>
    /// SetZone（DropZone）のカードを削除
    /// </summary>
    private void ClearSetZone()
    {
        Debug.Log("[OnlineGameManager] Clearing SetZone cards");
        
        // デバッグ: OnlineMatchManagerのDropZone参照を確認
        if (matchManager != null)
        {
            Debug.Log($"[OnlineGameManager] matchManager found - playerDropZone: {matchManager.playerDropZone?.name}, opponentDropZone: {matchManager.opponentDropZone?.name}");
            
            // OnlineMatchManagerの参照を使用してカードを削除
            if (matchManager.playerDropZone != null)
            {
                int childCount = matchManager.playerDropZone.transform.childCount;
                Debug.Log($"[OnlineGameManager] Player DropZone has {childCount} children");
                foreach (Transform child in matchManager.playerDropZone.transform)
                {
                    Debug.Log($"[OnlineGameManager] Destroying player card: {child.name}");
                    // まず即座に非表示にする（描画の被りを回避）
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                Debug.Log("[OnlineGameManager] Player DropZone cleared via matchManager");
            }
            
            if (matchManager.opponentDropZone != null)
            {
                int childCount = matchManager.opponentDropZone.transform.childCount;
                Debug.Log($"[OnlineGameManager] Opponent DropZone has {childCount} children");
                foreach (Transform child in matchManager.opponentDropZone.transform)
                {
                    Debug.Log($"[OnlineGameManager] Destroying opponent card: {child.name}");
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                Debug.Log("[OnlineGameManager] Opponent DropZone cleared via matchManager");
            }
                    }
                    else
                    {
            Debug.LogWarning("[OnlineGameManager] matchManager is null, trying GameObject.Find method");
            
            // フォールバック: GameObject.Findを使用
            var mySetZone = GameObject.Find("MySetZone");
            if (mySetZone != null)
            {
                Debug.Log($"[OnlineGameManager] Found MySetZone: {mySetZone.name}");
                foreach (Transform child in mySetZone.transform)
                {
                    Destroy(child.gameObject);
                }
                Debug.Log("[OnlineGameManager] My SetZone cleared via GameObject.Find");
                }
                else
                {
                Debug.LogWarning("[OnlineGameManager] MySetZone not found via GameObject.Find");
            }
            
            var opponentSetZone = GameObject.Find("OpponentSetZone");
            if (opponentSetZone != null)
            {
                Debug.Log($"[OnlineGameManager] Found OpponentSetZone: {opponentSetZone.name}");
                foreach (Transform child in opponentSetZone.transform)
                {
                    Destroy(child.gameObject);
                }
                Debug.Log("[OnlineGameManager] Opponent SetZone cleared via GameObject.Find");
                }
                else
                {
                Debug.LogWarning("[OnlineGameManager] OpponentSetZone not found via GameObject.Find");
            }
        }
    }
    
    private void OnBothPlayersPlaced(int player1CardValue, int player2CardValue)
    {
        Debug.Log($"[OnlineGameManager] Both players placed cards: {player1CardValue}, {player2CardValue}");
        StartCoroutine(HandleSetCompletePhase(player1CardValue, player2CardValue));
    }
    
    private void OnBetActionExecuted(string actionType, int betValue)
    {
        Debug.Log($"[OnlineGameManager] Bet action executed: {actionType}, {betValue}");
    }
    
    /// <summary>
    /// セット完了フェーズの処理
    /// </summary>
    private IEnumerator HandleSetCompletePhase(int player1CardValue, int player2CardValue)
    {
        Debug.Log("[OnlineGameManager] HandleSetCompletePhase started");
        
        phaseManager.StartSetCompletePhase();
        
        // 相手のカードを裏向きで表示
        int opponentCardValue = gameDataProvider.IsPlayer1 ? player2CardValue : player1CardValue;
        cardDisplayManager.DisplayOpponentCardFaceDown(opponentCardValue);
        
        // セット完了パネルを表示
        if (panelManager != null)
        {
            panelManager.ShowSetCompletePanel();
        }
        
        yield return new WaitForSeconds(3f);
        
        // セット完了パネルを非表示
        if (panelManager != null)
        {
            panelManager.HideSetCompletePanel();
        }
        
        // Betting Phaseに遷移
        phaseManager.EndSetCompletePhase();
        phaseManager.HandlePhaseChange("betting");
    }
    
    /// <summary>
    /// 勝敗判定
    /// </summary>
    private void JudgeWinner(int playerCardValue, int opponentCardValue)
    {
        int playerRankIndex = playerCardValue % 13;
        int opponentRankIndex = opponentCardValue % 13;
        bool playerWins = resultViewManager.IsWinner(playerRankIndex, opponentRankIndex);
        
        Debug.Log($"[OnlineGameManager] JudgeWinner: player={playerCardValue}, opponent={opponentCardValue}, playerWins={playerWins}");
        
            if (panelManager != null)
            {
            panelManager.gameResultPanel.SetActive(true);
            
            if (playerWins)
            {
                panelManager.gameResultText.text = "YOU WIN!";
                panelManager.gameResultText.color = Color.red;
        }
        else
        {
                panelManager.gameResultText.text = "YOU LOSE...";
                panelManager.gameResultText.color = Color.blue;
            }
        }
    }
    
    // ========== 公開メソッド（UI連携用） ==========
    
    public void ShowConfirmation(CardDisplay card, OnlineDropZone zone)
    {
        cardPlacementManager.ShowConfirmation(card, zone);
    }
    
    public void ConfirmPlacement()
    {
        cardPlacementManager.ConfirmPlacement();
    }
    
    public void CancelPlacement()
    {
        cardPlacementManager.CancelPlacement();
    }
    
    public void IncreaseBetValue()
    {
        bettingManager.IncreaseBetValue();
    }
    
    public void DecreaseBetValue()
    {
        bettingManager.DecreaseBetValue();
    }
    
    public void CallOrRaise()
    {
        bettingManager.CallOrRaise();
    }
    
    public void Drop()
    {
        bettingManager.Drop();
    }
    
    public bool CanSetCard()
    {
        return cardPlacementManager.CanSetCard;
    }
    
    // スキル関連プロパティ（互換性のため）
    public CardDisplay SetOpponentCard { get; }
    public bool PlayerCanUseScanSkill { get; }
    public bool PlayerCanUseChangeSkill { get; }
    public bool PlayerCanUseObstructSkill { get; }
    public bool PlayerCanUseFakeOutSkill { get; }
    public bool PlayerCanUseCopySkill { get; }
    
    public void SetOpponentCalled(bool called) { }
    public void RevealCards() { }
    
    /// <summary>
    /// RevealPhase用にAWSから最新のゲーム状態を取得
    /// </summary>
    private void FetchLatestGameStateForReveal()
    {
        Debug.Log("[REVEAL_DEBUG] Fetching latest game state for reveal phase");
        
        if (!gameDataProvider.IsValid())
        {
            Debug.LogError("[REVEAL_DEBUG] GameDataProvider is not valid, cannot fetch game state");
            return;
        }
        
        HttpManager.Instance.GetGameState(
            gameDataProvider.GameId,
            gameDataProvider.PlayerId,
            OnRevealGameStateReceived,
            OnRevealGameStateError
        );
    }
    
    /// <summary>
    /// RevealPhase用ゲーム状態取得成功時のコールバック
    /// </summary>
    private void OnRevealGameStateReceived(string response)
    {
        Debug.Log($"[REVEAL_DEBUG] Game state received: {response}");
        
        try
        {
            var gameStateData = JsonUtility.FromJson<GameStateResponse>(response);
            
            if (gameStateData != null)
            {
                Debug.Log($"[REVEAL_DEBUG] Raw card values from AWS - Player1: {gameStateData.player1CardValue}, Player2: {gameStateData.player2CardValue}");
                
                // Dealer情報を更新
                if (!string.IsNullOrEmpty(gameStateData.currentDealer))
                {
                    gameDataProvider.UpdateCurrentDealer(gameStateData.currentDealer);
                    Debug.Log($"[REVEAL_DEBUG] Current dealer: {gameStateData.currentDealer}");
                }
                
                // セットしたカード値を更新
                gameDataProvider.UpdateSetCardValues(gameStateData.player1CardValue, gameStateData.player2CardValue);
                
                // プレイヤーの立場に応じてカード値を決定
                bool isPlayer1 = gameDataProvider.IsPlayer1;
                int myCardValue = isPlayer1 ? gameStateData.player1CardValue : gameStateData.player2CardValue;
                int opponentCardValue = isPlayer1 ? gameStateData.player2CardValue : gameStateData.player1CardValue;
                
                Debug.Log($"[REVEAL_DEBUG] Player perspective - IsPlayer1: {isPlayer1}, MyCard: {myCardValue}, OpponentCard: {opponentCardValue}");
                
                // 相手のカードを表向きで表示
                cardDisplayManager.DisplayOpponentCardFaceUp(opponentCardValue);
                
                // ResultViewの表を更新（自分のカード、相手のカードの順）
                if (resultViewManager != null)
                {
                    Debug.Log($"[REVEAL_DEBUG] Calling ShowResultTable with MyCard: {myCardValue}, OpponentCard: {opponentCardValue}");
                    resultViewManager.ShowResultTable(myCardValue, opponentCardValue);
                }
                else
                {
                    Debug.LogError("[REVEAL_DEBUG] resultViewManager is null, cannot update result table");
                }
                
                // 勝敗判定（自分のカード、相手のカードの順）
                JudgeWinner(myCardValue, opponentCardValue);
                
                Debug.Log($"[REVEAL_DEBUG] Reveal phase processing completed - MyCard: {myCardValue}, OpponentCard: {opponentCardValue}");
            }
            else
            {
                Debug.LogError("[REVEAL_DEBUG] Failed to parse game state response");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[REVEAL_DEBUG] Exception while processing game state: {e.Message}");
        }
    }
    
    /// <summary>
    /// RevealPhase用ゲーム状態取得失敗時のコールバック
    /// </summary>
    private void OnRevealGameStateError(string error)
    {
        Debug.LogError($"[REVEAL_DEBUG] Failed to fetch game state: {error}");
    }

    [System.Serializable]
    private class GameStateResponse
    {
        public string gameId;
        public string gamePhase;
        public string currentTurn;
        public string currentDealer;  // 現在の親プレイヤー
        public int player1Life;
        public int player2Life;
        public int currentBet;
        public bool player1CardPlaced;
        public bool player2CardPlaced;
        public int player1BetAmount;
        public int player2BetAmount;
        public int player1CardValue;
        public int player2CardValue;
        public int[] myCards;
        public int myLife;
        public int myBetAmount;
        public bool myCardPlaced;
        public bool opponentCardPlaced;
        public int? opponentPlacedCardId;
        public string updatedAt;
        public int[] player1Cards;
        public int[] player2Cards;
    }
}

