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
            handManager.SetPlayerHand(gameDataProvider.MyCards);
            handManager.SetOpponentHand(gameDataProvider.OpponentCards);
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
    }
}

