using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System;

public class OnlineGameManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerLifeText;
    public TextMeshProUGUI opponentLifeText;
    public TextMeshProUGUI turnIndicatorText;
    public TextMeshProUGUI gamePhaseText;
    public GameObject waitingPanel;
    public GameObject gamePanel;
    public GameObject vsPanel;
    public TextMeshProUGUI vsText;

    [Header("Card Display")]
    public CardDisplay playerCard1;
    public CardDisplay playerCard2;
    public CardDisplay opponentCard1;
    public CardDisplay opponentCard2;
    public DropZone playerDropZone;
    public DropZone opponentDropZone;

    [Header("API Configuration")]
    public string apiBaseUrl = "https://your-api-gateway-url.execute-api.ap-northeast-1.amazonaws.com/dev";

    // ゲーム状態
    private string gameId;
    private string playerId;
    private string opponentId;
    private bool isPlayer1;
    private List<int> myCards = new List<int>();
    private int myLife = 10;
    private int opponentLife = 10;
    private string currentTurn;
    private string gamePhase = "card_placement";
    private bool myCardPlaced = false;
    private bool opponentCardPlaced = false;
    private int currentBet = 0;

    // 通信管理
    private OnlineGameClient gameClient;
    private Coroutine gameStatePollingCoroutine;

    // イベント
    public event Action<string> OnGamePhaseChanged;
    public event Action<string> OnTurnChanged;
    public event Action<int, int> OnLifeChanged;
    public event Action<List<int>> OnCardsReceived;

    void Start()
    {
        gameClient = new OnlineGameClient(apiBaseUrl);
        InitializeUI();
    }

    void OnDestroy()
    {
        if (gameStatePollingCoroutine != null)
        {
            StopCoroutine(gameStatePollingCoroutine);
        }
    }

    /// <summary>
    /// オンラインゲームを開始
    /// </summary>
    public void StartOnlineGame(string roomCode, string playerId, string opponentId, bool isPlayer1, string gameId = null)
    {
        this.gameId = gameId; // gameIdが渡された場合は設定、そうでなければ後で設定
        this.playerId = playerId;
        this.opponentId = opponentId;
        this.isPlayer1 = isPlayer1;

        // VS画面を表示
        ShowVSPanel();
        
        // 数秒後にゲーム開始
        StartCoroutine(StartGameAfterDelay(roomCode));
    }

    private IEnumerator StartGameAfterDelay(string roomCode)
    {
        // VS画面を3秒間表示
        yield return new WaitForSeconds(3f);
        
        // VS画面を非表示
        vsPanel.SetActive(false);
        gamePanel.SetActive(true);

        // ゲーム状態のポーリングを開始
        StartGameStatePolling();
    }

    private void ShowVSPanel()
    {
        vsPanel.SetActive(true);
        gamePanel.SetActive(false);
        waitingPanel.SetActive(false);
        
        string player1Name = isPlayer1 ? playerId : opponentId;
        string player2Name = isPlayer1 ? opponentId : playerId;
        vsText.text = $"{player1Name} vs {player2Name}";
    }

    /// <summary>
    /// ゲーム状態のポーリングを開始
    /// </summary>
    private void StartGameStatePolling()
    {
        if (gameStatePollingCoroutine != null)
        {
            StopCoroutine(gameStatePollingCoroutine);
        }
        gameStatePollingCoroutine = StartCoroutine(PollGameState());
    }

    /// <summary>
    /// ゲーム状態を定期的に取得
    /// </summary>
    private IEnumerator PollGameState()
    {
        while (true)
        {
            if (!string.IsNullOrEmpty(gameId))
            {
                gameClient.GetGameState(gameId, playerId, OnGameStateReceived, OnError);
            }
            
            // 1秒間隔でポーリング
            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// ゲーム状態を受信した時の処理
    /// </summary>
    private void OnGameStateReceived(GameStateResponse gameState)
    {
        // ゲームIDが設定されていない場合は設定
        if (string.IsNullOrEmpty(gameId))
        {
            gameId = gameState.gameId;
        }

        // 状態が変更された場合のみ更新
        if (gameState.gamePhase != gamePhase)
        {
            gamePhase = gameState.gamePhase;
            OnGamePhaseChanged?.Invoke(gamePhase);
            UpdateGamePhaseUI();
        }

        if (gameState.currentTurn != currentTurn)
        {
            currentTurn = gameState.currentTurn;
            OnTurnChanged?.Invoke(currentTurn);
            UpdateTurnUI();
        }

        if (gameState.myLife != myLife || gameState.player1Life != player1Life || gameState.player2Life != player2Life)
        {
            myLife = gameState.myLife;
            opponentLife = isPlayer1 ? gameState.player2Life : gameState.player1Life;
            OnLifeChanged?.Invoke(myLife, opponentLife);
            UpdateLifeUI();
        }

        // カードが初回受信または変更された場合
        if (myCards.Count == 0 || !AreCardListsEqual(myCards, gameState.myCards))
        {
            myCards = new List<int>(gameState.myCards);
            OnCardsReceived?.Invoke(myCards);
            UpdateCardDisplay();
        }

        // カード配置状態の更新
        myCardPlaced = gameState.myCardPlaced;
        opponentCardPlaced = gameState.opponentCardPlaced;
    }

    /// <summary>
    /// エラー処理
    /// </summary>
    private void OnError(string error)
    {
        Debug.LogError($"Online Game Error: {error}");
        // エラー処理（リトライ、エラー画面表示など）
    }

    /// <summary>
    /// カードを配置
    /// </summary>
    public void PlaceCard(int cardId)
    {
        if (currentTurn != playerId)
        {
            Debug.LogWarning("Not your turn!");
            return;
        }

        if (myCardPlaced)
        {
            Debug.LogWarning("Card already placed!");
            return;
        }

        if (!myCards.Contains(cardId))
        {
            Debug.LogWarning("Card not in hand!");
            return;
        }

        gameClient.UpdateGameState(gameId, playerId, "place_card", new Dictionary<string, object>
        {
            { "cardId", cardId }
        }, OnActionSuccess, OnError);
    }

    /// <summary>
    /// ベット
    /// </summary>
    public void PlaceBet(int amount)
    {
        if (currentTurn != playerId)
        {
            Debug.LogWarning("Not your turn!");
            return;
        }

        if (myLife < amount)
        {
            Debug.LogWarning("Insufficient life!");
            return;
        }

        gameClient.UpdateGameState(gameId, playerId, "bet", new Dictionary<string, object>
        {
            { "amount", amount }
        }, OnActionSuccess, OnError);
    }

    /// <summary>
    /// コール
    /// </summary>
    public void Call()
    {
        if (currentTurn != playerId)
        {
            Debug.LogWarning("Not your turn!");
            return;
        }

        gameClient.UpdateGameState(gameId, playerId, "call", new Dictionary<string, object>(), OnActionSuccess, OnError);
    }

    /// <summary>
    /// スキルを使用
    /// </summary>
    public void UseSkill(string skillType)
    {
        if (currentTurn != playerId)
        {
            Debug.LogWarning("Not your turn!");
            return;
        }

        gameClient.UpdateGameState(gameId, playerId, "use_skill", new Dictionary<string, object>
        {
            { "skillType", skillType }
        }, OnActionSuccess, OnError);
    }

    /// <summary>
    /// アクション成功時の処理
    /// </summary>
    private void OnActionSuccess(ActionResponse response)
    {
        Debug.Log($"Action successful: {response.message}");
        // 成功時の処理（UI更新など）
    }

    // UI更新メソッド
    private void InitializeUI()
    {
        waitingPanel.SetActive(true);
        gamePanel.SetActive(false);
        vsPanel.SetActive(false);
    }

    private void UpdateGamePhaseUI()
    {
        if (gamePhaseText != null)
        {
            gamePhaseText.text = $"Phase: {gamePhase}";
        }
    }

    private void UpdateTurnUI()
    {
        if (turnIndicatorText != null)
        {
            bool isMyTurn = currentTurn == playerId;
            turnIndicatorText.text = isMyTurn ? "Your Turn" : "Opponent's Turn";
            turnIndicatorText.color = isMyTurn ? Color.green : Color.red;
        }
    }

    private void UpdateLifeUI()
    {
        if (playerLifeText != null)
        {
            playerLifeText.text = $"Life: {myLife}";
        }
        if (opponentLifeText != null)
        {
            opponentLifeText.text = $"Life: {opponentLife}";
        }
    }

    private void UpdateCardDisplay()
    {
        // プレイヤーのカードを表示
        if (myCards.Count >= 1)
        {
            DisplayCard(playerCard1, myCards[0], true);
        }
        if (myCards.Count >= 2)
        {
            DisplayCard(playerCard2, myCards[1], true);
        }
    }

    private void DisplayCard(CardDisplay cardDisplay, int cardId, bool isPlayerCard)
    {
        if (cardDisplay != null)
        {
            cardDisplay.SetCard(isPlayerCard);
            cardDisplay.SetCardValue(cardId);
        }
    }

    // ユーティリティメソッド
    private bool AreCardListsEqual(List<int> list1, List<int> list2)
    {
        if (list1.Count != list2.Count) return false;
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i]) return false;
        }
        return true;
    }

    // プロパティ
    public string GameId => gameId;
    public string PlayerId => playerId;
    public string OpponentId => opponentId;
    public bool IsPlayer1 => isPlayer1;
    public List<int> MyCards => myCards;
    public int MyLife => myLife;
    public int OpponentLife => opponentLife;
    public string CurrentTurn => currentTurn;
    public string GamePhase => gamePhase;
    public bool MyCardPlaced => myCardPlaced;
    public bool OpponentCardPlaced => opponentCardPlaced;
    public int CurrentBet => currentBet;
} 