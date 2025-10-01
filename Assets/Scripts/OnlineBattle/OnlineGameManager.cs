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
public class OnlineGameManagerNew : MonoBehaviour
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
    private OnlineParentChildManager parentChildManager;
    private OnlineGameDataProvider gameDataProvider;
    private OnlineCardDisplayManager cardDisplayManager;
    
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
        parentChildManager = GetOrAddComponent<OnlineParentChildManager>();
        cardDisplayManager = GetOrAddComponent<OnlineCardDisplayManager>();
        
        // 各マネージャーを初期化
        phaseManager.Initialize(panelManager, gameDataProvider);
        cardPlacementManager.Initialize(panelManager, handManager, gameDataProvider);
        bettingManager.Initialize(panelManager, gameDataProvider);
        parentChildManager.Initialize(panelManager, bettingManager, gameDataProvider);
        cardDisplayManager.Initialize(gameDataProvider);
        
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
        
        // 親子システムイベント
        parentChildManager.OnParentTurnStarted += OnParentTurnStarted;
        parentChildManager.OnChildTurnStarted += OnChildTurnStarted;
        parentChildManager.OnParentBetComplete += OnParentBetComplete;
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
        
        // 親子システムの初期化
        parentChildManager.InitializeParentChildSystem(gameDataProvider.IsPlayer1);
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
    }
    
    private void OnBettingPhaseStarted()
    {
        Debug.Log("[OnlineGameManager] Betting Phase started");
        cardPlacementManager.DisableCardPlacement();
        cardDisplayManager.EnsureOpponentCardDisplayed();
        parentChildManager.StartParentTurn();
    }
    
    private void OnRevealPhaseStarted()
    {
        Debug.Log("[OnlineGameManager] Reveal Phase started");
        
        int playerCardValue = gameDataProvider.GetPlayerCardValue(0);
        int opponentCardValue = gameDataProvider.GetOpponentCardValue(0);
        
        // 相手のカードを表向きに
        cardDisplayManager.DisplayOpponentCardFaceUp(opponentCardValue);
        
        // 結果表を更新
        if (resultViewManager != null)
        {
            resultViewManager.ShowResultTable(playerCardValue, opponentCardValue);
        }
        
        // 勝敗判定
        JudgeWinner(playerCardValue, opponentCardValue);
    }
    
    private void OnBothPlayersPlaced(int player1CardValue, int player2CardValue)
    {
        Debug.Log($"[OnlineGameManager] Both players placed cards: {player1CardValue}, {player2CardValue}");
        StartCoroutine(HandleSetCompletePhase(player1CardValue, player2CardValue));
    }
    
    private void OnBetActionExecuted(string actionType, int betValue)
    {
        Debug.Log($"[OnlineGameManager] Bet action executed: {actionType}, {betValue}");
        
        if (parentChildManager.IsParent)
        {
            // 親の場合
            bettingManager.UpdatePlayerBetAmountInServer(betValue);
            
            if (actionType == "call" || actionType == "raise")
            {
                parentChildManager.StartChildTurn();
            }
            else if (actionType == "drop")
            {
                phaseManager.HandlePhaseChange("reveal");
            }
        }
        else
        {
            // 子の場合
            if (actionType == "call" || actionType == "drop")
            {
                phaseManager.HandlePhaseChange("reveal");
            }
            else if (actionType == "raise")
            {
                parentChildManager.StartParentTurn();
            }
        }
    }
    
    private void OnParentTurnStarted()
    {
        Debug.Log("[OnlineGameManager] Parent turn started");
    }
    
    private void OnChildTurnStarted()
    {
        Debug.Log("[OnlineGameManager] Child turn started");
    }
    
    private void OnParentBetComplete(string betAction, int betAmount)
    {
        Debug.Log($"[OnlineGameManager] Parent bet complete: {betAction}, {betAmount}");
        
        bettingManager.SetMinimumBetValue(betAmount);
        bettingManager.SetBetValue(betAmount);
        
        if (panelManager != null)
        {
            panelManager.UpdateOpponentBetAmountDisplay(betAmount);
        }
        
        parentChildManager.StartChildTurnWithDelay();
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
}

